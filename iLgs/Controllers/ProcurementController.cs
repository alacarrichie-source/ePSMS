using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Ai.Services;
using iLgs.Services.PPMP_;
using iLgs.Services.PurchaseOrder;
using iLgs.Services.PurchaseRequest;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class ProcurementController : BaseController
    {
        private const string AnnualFilterSessionKey = "AnnualProcurementFilters";
        private const string CartSessionKey = "ProcurementCart";
        private const string CheckoutTokenSessionKey = "ProcurementCheckoutToken";
        private readonly IOrderService _orderService;
        private readonly IRequestService _requestService;
        private readonly ICodextnService _codextnService;
        private readonly IPPMPService _ppmpService;

        private string _menuId = string.Empty;

        public ProcurementController()
        {
            _orderService = new OrderService(_db);
            _requestService = new RequestService(_db);
            _codextnService = new CodextnService(_db);
            _ppmpService = new PPMPService(_db);
        }

        [Serializable]
        private class AnnualFilterState
        {
            public int? FiscalYear { get; set; }
            public Guid? Department { get; set; }
            public string Category { get; set; }
            public string SearchText { get; set; }
        }        

        public async Task<ActionResult> Annual(int? fiscalYear, Guid? department, string category,string searchText)
        {
            var activeCart = GetCart();
            var hasIncomingFilters =
                Request.QueryString["fiscalYear"] != null ||
                Request.QueryString["department"] != null ||
                Request.QueryString["category"] != null ||
                Request.QueryString["searchText"] != null;
            var savedFilter = Session[AnnualFilterSessionKey] as AnnualFilterState;

            if (!hasIncomingFilters && savedFilter != null)
            {
                fiscalYear = savedFilter.FiscalYear;
                department = savedFilter.Department;
                category = savedFilter.Category;
                searchText = savedFilter.SearchText;
            }

            var userId = User.Identity.GetUserId();
            var departments = (await _codextnService.GetUserDepartmentsAsync(userId))
                .OrderBy(x => x.Code)
                .ToList();

            if (activeCart.IsRevision)
            {
                if (!CanWriteRevisionCart(activeCart))
                {
                    SaveCart(new CartViewModel());
                    return RedirectToAction("Index", "Requests");
                }
                fiscalYear = activeCart.FiscalYear;
                department = activeCart.DepartmentId;
            }

            if (!departments.Any())
            {
                return new HttpStatusCodeResult(403, "No department is assigned to this user.");
            }

            var departmentIds = departments.Select(x => x.Id).ToList();
            var availableItems = _ppmpService.PPMPItem.GetAll()
                .Where(x => x.PPMP.DeptId.HasValue &&
                            departmentIds.Contains(x.PPMP.DeptId.Value));

            var fiscalYears = await availableItems
                .Where(x => x.PPMP.ForYear.HasValue)
                .Select(x => x.PPMP.ForYear.Value)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            fiscalYear = fiscalYear ?? fiscalYears.FirstOrDefault();
            if (!fiscalYear.HasValue || fiscalYear.Value == 0)
            {
                fiscalYear = DateTime.Today.Year;
            }
            if (!fiscalYears.Contains(fiscalYear.Value))
            {
                fiscalYears.Insert(0, fiscalYear.Value);
            }

            department = department ?? departments.First().Id;
            if (!departments.Any(x => x.Id == department.Value))
            {
                return new HttpStatusCodeResult(403, "You are not authorized to use the selected department.");
            }

            Session[AnnualFilterSessionKey] = new AnnualFilterState
            {
                FiscalYear = fiscalYear,
                Department = department,
                Category = category ?? "",
                SearchText = searchText ?? ""
            };

            var itemQuery = _ppmpService.PPMPItem.GetAll()
                .Where(x => x.PPMP.DeptId == department && x.PPMP.ForYear == fiscalYear)
                .Where(x => String.IsNullOrEmpty(category) || x.ProcMode == category);

            if (!String.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();
                itemQuery = itemQuery.Where(x =>
                    (x.Description ?? "").Contains(searchText) ||
                    (x.Code ?? "").Contains(searchText));
            }

            var items = await itemQuery
                .OrderBy(o => o.Code)
                .ThenBy(o => o.Description)
                .ToListAsync();

            var procurementModes = await availableItems
                .Where(x => x.PPMP.DeptId == department &&
                            x.PPMP.ForYear == fiscalYear &&
                            x.ProcMode != null &&
                            x.ProcMode != "")
                .Select(x => x.ProcMode)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var cart = GetCart();
            var cartItemIds = new HashSet<Guid>(cart.Items.Select(x => x.Id));

            foreach (var item in items)
            {
                item.IsInCart = cartItemIds.Contains(item.Id);
            }

            return View(new AnnualProcurementViewModel
            {
                FiscalYear = fiscalYear.Value,
                Department = department,
                Category = category ?? "",
                SearchText = searchText,
                Items = items,
                CartCount = cart.Items.Count,
                IsRevision = cart.IsRevision,
                RequestId = cart.RequestId,
                FiscalYears = fiscalYears.Select(x => new SelectListItem
                {
                    Text = x.ToString(),
                    Value = x.ToString()
                }),
                Departments = departments,
                Categories = new[]
                {
                    new SelectListItem { Text = "All Procurement Modes", Value = "" }
                }.Concat(procurementModes.Select(x => new SelectListItem { Text = x, Value = x }))
            });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StartRevision(Guid id)
        {
            var access = await Access(User.Identity.GetUserId(), "requests");
            if (!access.IsAllowed || !access.AllowEdit)
                return Json(new { success = false, message = "Revision access denied." });

            using (var transaction = _db.Database.BeginTransaction())
            {
                var entity = await _db.Requests
                    .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                    .Include(x => x.RequestItems.Select(i => i.PPMPItem.PPMP))
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                    return Json(new { success = false, message = "The Purchase Request no longer exists." });

                var historyService = new DocumentHistoryService(_db);
                var latest = await historyService.GetLatestHistoryAsync(DocumentTypes.PurchaseRequest, id);
                var isReturned = latest != null && String.Equals(latest.ToStatus, PrStatuses.Returned, StringComparison.OrdinalIgnoreCase);
                var isRevising = latest != null && String.Equals(latest.ToStatus, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase);
                if (!isReturned && !isRevising)
                    return Json(new { success = false, message = "Only a Returned Purchase Request or an active revision can be opened." });
                if (!access.IsAdmin && !String.Equals(entity.InsertedBy, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only the original requester can revise this Purchase Request." });

                var departments = await GetAuthorizedDepartmentsAsync();
                if (!entity.DeptId.HasValue || !departments.Any(x => x.Id == entity.DeptId.Value))
                    return Json(new { success = false, message = "You are not authorized for this Purchase Request department." });
                var existingCart = GetCart();
                if (isRevising && existingCart.IsRevision && existingCart.RequestId == entity.Id && CanWriteRevisionCart(existingCart))
                    return Json(new { success = true, redirectUrl = Url.Action("Cart", "Procurement") });
                if (entity.RequestItems.Any(x => !x.PpmpItemId.HasValue || x.PPMPItem == null || x.PPMPItem.PPMP == null))
                    return Json(new { success = false, message = "A Purchase Request item is no longer linked to the Annual Procurement Plan." });
                if (!entity.RequestItems.Any())
                    return Json(new { success = false, message = "The Purchase Request has no items to revise." });
                if (entity.RequestItems.Any(x => x.Qty.GetValueOrDefault() <= 0 || x.Qty.GetValueOrDefault() != Math.Truncate(x.Qty.GetValueOrDefault())))
                    return Json(new { success = false, message = "The shared cart requires whole-number item quantities." });

                var revisionNo = await _db.DocumentStatusHistories.CountAsync(x =>
                    x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == id && x.Action.StartsWith("Resubmitted")) + 1;
                var returnComment = await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == id && x.ToStatus == PrStatuses.Returned)
                    .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                    .Select(x => x.Remarks).FirstOrDefaultAsync();
                var cart = new CartViewModel
                {
                    RequestId = entity.Id, IsRevision = true, Status = PrStatuses.Revising,
                    ReviewComment = returnComment, RevisionNo = revisionNo, RevisionUser = User.Identity.Name,
                    DepartmentId = entity.DeptId,
                    FiscalYear = entity.RequestItems.Select(x => x.PPMPItem.PPMP.ForYear).FirstOrDefault()
                };
                cart.Items = entity.RequestItems.OrderBy(x => x.ItemNoIndex).Select(item => new CartItemViewModel
                {
                    Id = item.PpmpItemId.Value, RequestItemId = item.Id, ItemNo = item.ItemNo,
                    Code = item.PpmpCode, Description = item.Description,
                    TechnicalSpecifications = item.OtherDesc, Unit = item.Unit, UnitCost = item.UnitCost,
                    Quantity = Convert.ToInt32(item.Qty.GetValueOrDefault()),
                    SubItems = item.RequestSubItems.OrderBy(x => x.ItemNoIndex).Select(subItem => new CartSubItemViewModel
                    {
                        Id = subItem.Id, ParentItemId = item.PpmpItemId.Value, ItemNo = subItem.ItemNo,
                        Description = subItem.Description, Unit = subItem.Unit,
                        Quantity = subItem.Qty.GetValueOrDefault(), UnitCost = subItem.UnitCost.GetValueOrDefault()
                    }).ToList()
                }).ToList();

                if (isReturned)
                {
                    historyService.AddStatusHistory(DocumentTypes.PurchaseRequest, entity.Id, entity.PrNo ?? entity.CtrlNo,
                        PrStatuses.Returned, PrStatuses.Revising, "Revision Started",
                        "Purchase Request revision " + revisionNo + " started.", User.Identity.Name);
                    await _db.SaveChangesAsync();
                }
                transaction.Commit();

                RenumberCartItems(cart);
                SaveCart(cart);
                Session[AnnualFilterSessionKey] = new AnnualFilterState
                {
                    FiscalYear = cart.FiscalYear, Department = cart.DepartmentId, Category = "", SearchText = ""
                };
                return Json(new { success = true, redirectUrl = Url.Action("Cart", "Procurement") });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddToCart(Guid id)
        {
            var cart = GetCart();
            if (!CanWriteRevisionCart(cart))
                return Json(new { success = false, message = "This Purchase Request is no longer available for revision." });

            var item = await _ppmpService.PPMPItem.GetByIdAsync(id);

            if (item == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Procurement item was not found."
                });
            }

            var filter = Session[AnnualFilterSessionKey] as AnnualFilterState;
            var departments = await GetAuthorizedDepartmentsAsync();
            if (filter == null ||
                !filter.Department.HasValue ||
                !filter.FiscalYear.HasValue ||
                !departments.Any(x => x.Id == filter.Department.Value) ||
                item.PPMP == null ||
                item.PPMP.DeptId != filter.Department ||
                item.PPMP.ForYear != filter.FiscalYear)
            {
                Response.StatusCode = 403;
                return Json(new
                {
                    success = false,
                    message = "The item is outside your active procurement plan."
                });
            }

            if (cart.Items.Any() &&
                (cart.DepartmentId != filter.Department || cart.FiscalYear != filter.FiscalYear))
            {
                return Json(new
                {
                    success = false,
                    message = "Your cart belongs to another department or fiscal year. Empty it before changing procurement plans."
                });
            }

            cart.DepartmentId = filter.Department;
            cart.FiscalYear = filter.FiscalYear;

            if (cart.Items.Any(x => x.Id == item.Id))
            {
                return Json(new
                {
                    success = true,
                    alreadyInCart = true,
                    cartCount = cart.Items.Count,
                    message = "The item is already in your cart."
                });
            }

            if (item.QtyBal <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "This item has no remaining quantity available."
                });
            }

            cart.Items.Add(new CartItemViewModel
            {
                Id = item.Id,
                Code = item.Code,
                Description = item.Description,
                Unit = item.Unit,
                UnitCost = item.UnitCost,
                Quantity = item.QtyBal.Value
            });

            RenumberCartItems(cart);
            SaveCart(cart);            

            return Json(new
            {
                success = true,
                cartCount = cart.Items.Count
            });
        }

        public ActionResult Cart()
        {
            var cart = GetCart();
            if (!CanWriteRevisionCart(cart))
            {
                SaveCart(new CartViewModel());
                TempData["Message"] = "This Purchase Request is no longer available for revision.";
                return RedirectToAction("Index", "Requests");
            }
            return View(cart);
        }

        [HttpPost]
        public ActionResult CartSubItemsRead(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId)
        {
            var cartItem = GetCart().Items.SingleOrDefault(x => x.Id == parentItemId);
            var subItems = cartItem == null
                ? new List<CartSubItemViewModel>()
                : EnsureSubItems(cartItem);

            return Json(subItems.ToDataSourceResult(request));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCartSubItem(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId,
            CartSubItemViewModel subItem)
        {
            var cart = GetCart();
            if (!CanWriteRevisionCart(cart)) ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            var cartItem = cart.Items.SingleOrDefault(x => x.Id == parentItemId);

            // These values are generated and assigned by the server. Kendo sends
            // an empty Id for a newly inserted row, which MVC records as a
            // binding error for the non-nullable Guid before this action runs.
            ModelState.Remove("Id");
            ModelState.Remove("subItem.Id");
            ModelState.Remove("ParentItemId");
            ModelState.Remove("subItem.ParentItemId");
            ModelState.Remove("ItemNo");
            ModelState.Remove("subItem.ItemNo");

            if (cartItem == null)
            {
                ModelState.AddModelError("", "The parent cart item no longer exists.");
            }

            if (ModelState.IsValid)
            {
                subItem.Id = Guid.NewGuid();
                subItem.ParentItemId = parentItemId;
                EnsureSubItems(cartItem).Add(subItem);
                RenumberCartSubItems(cartItem);
                SaveCart(cart);
            }

            return Json(new[] { subItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCartSubItem(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId,
            CartSubItemViewModel subItem)
        {
            var cart = GetCart();
            if (!CanWriteRevisionCart(cart)) ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            var cartItem = cart.Items.SingleOrDefault(x => x.Id == parentItemId);
            var existing = cartItem == null
                ? null
                : EnsureSubItems(cartItem).SingleOrDefault(x => x.Id == subItem.Id);

            if (existing == null)
            {
                ModelState.AddModelError("", "The cart sub-item no longer exists.");
            }

            if (ModelState.IsValid)
            {
                existing.Description = subItem.Description;
                existing.Unit = subItem.Unit;
                existing.Quantity = subItem.Quantity;
                existing.UnitCost = subItem.UnitCost;
                RenumberCartSubItems(cartItem);
                SaveCart(cart);
                subItem = existing;
            }

            return Json(new[] { subItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveCartSubItem(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId,
            CartSubItemViewModel subItem)
        {
            var cart = GetCart();
            if (!CanWriteRevisionCart(cart)) ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            var cartItem = cart.Items.SingleOrDefault(x => x.Id == parentItemId);
            var existing = cartItem == null
                ? null
                : EnsureSubItems(cartItem).SingleOrDefault(x => x.Id == subItem.Id);

            if (existing != null && ModelState.IsValid)
            {
                cartItem.SubItems.Remove(existing);
                RenumberCartSubItems(cartItem);
                SaveCart(cart);
            }

            return Json(new[] { subItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        public ActionResult CartRead([DataSourceRequest] DataSourceRequest request)
        {
            var cart = GetCart();

            return Json(
                cart.Items.ToDataSourceResult(request),
                JsonRequestBehavior.AllowGet
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateCartItem(
            [DataSourceRequest] DataSourceRequest request,
            CartItemViewModel updatedItem)
        {
            var cart = GetCart();
            if (!CanWriteRevisionCart(cart)) ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");

            var cartItem = cart.Items.SingleOrDefault(x => x.Id == updatedItem.Id);

            if (cartItem == null)
            {
                ModelState.AddModelError("", "The cart item no longer exists.");
            }
            else if (updatedItem.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            }
            else if (String.IsNullOrWhiteSpace(updatedItem.Description))
            {
                ModelState.AddModelError("Description", "Description is required.");
            }
            else
            {
                // Get the current remaining quantity from your procurement-plan source.
                var procurementItem = await _ppmpService.PPMPItem.GetByIdAsync(updatedItem.Id);

                if (procurementItem == null)
                {
                    ModelState.AddModelError("", "The procurement item no longer exists.");
                }
                else if (updatedItem.Quantity > procurementItem.QtyBal.GetValueOrDefault() +
                    (cart.IsRevision && cart.RequestId.HasValue
                        ? (await _db.PPMPItemUsages.Where(x => x.PrId == cart.RequestId.Value && x.PpmpItemId == updatedItem.Id)
                            .Select(x => x.Qty).FirstOrDefaultAsync()).GetValueOrDefault()
                        : 0))
                {
                    ModelState.AddModelError(
                        "Quantity",
                        "Quantity cannot exceed the remaining quantity of " +
                        procurementItem.QtyBal + "."
                    );
                }
                else
                {
                    // Description is intentionally user-editable for the PR.
                    // Item identity, unit, and cost remain server-controlled.
                    cartItem.Description = updatedItem.Description.Trim();
                    cartItem.Quantity = updatedItem.Quantity;

                    SaveCart(cart);
                }
            }

            return Json(new[] { cartItem ?? updatedItem }
                .ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveCartItem(
            [DataSourceRequest] DataSourceRequest request,
            CartItemViewModel item)
        {
            var cart = GetCart();
            if (!CanWriteRevisionCart(cart)) ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");

            var cartItem = cart.Items.SingleOrDefault(x => x.Id == item.Id);

            if (cartItem != null && ModelState.IsValid)
            {
                cart.Items.Remove(cartItem);
                RenumberCartItems(cart);
                SaveCart(cart);
            }

            return Json(new[] { item }.ToDataSourceResult(request, ModelState));
        }

        [HttpGet]
        public JsonResult CartSummary()
        {
            var cart = GetCart();

            return Json(new
            {
                cartCount = cart.Items.Count,
                itemCount = cart.Items.Count,
                estimatedTotal = cart.EstimatedTotal
            }, JsonRequestBehavior.AllowGet);
        }        

        [HttpGet]
        public async Task<ActionResult> Checkout()
        {
            var cart = GetCart();

            if (!CanWriteRevisionCart(cart))
            {
                SaveCart(new CartViewModel());
                TempData["Message"] = "This Purchase Request is no longer available for revision.";
                return RedirectToAction("Index", "Requests");
            }

            if (!cart.Items.Any())
            {
                TempData["Message"] = "Your procurement cart is empty.";
                return RedirectToAction("Annual");
            }

            if (!cart.DepartmentId.HasValue || !cart.FiscalYear.HasValue)
            {
                TempData["Message"] = "Your department info is empty.";
                return RedirectToAction("Annual");
            }

            var departments = await GetAuthorizedDepartmentsAsync();
            var selectedDepartment = departments.FirstOrDefault(x => x.Id == cart.DepartmentId.Value);
            if (selectedDepartment == null)
            {
                return new HttpStatusCodeResult(403, "You are not authorized to use the cart department.");
            }

            ViewBag.Departments = departments.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Code
            });

            var checkoutToken = Guid.NewGuid().ToString("N");
            Session[CheckoutTokenSessionKey] = checkoutToken;

            var model = new PurchaseRequestViewModel
            {
                CheckoutToken = checkoutToken,
                ProcurementFiscalYear = cart.FiscalYear.Value,
                DeptId = selectedDepartment.Id,
                Department = selectedDepartment.Description,
                Items = cart.Items,
                RequestId = cart.RequestId,
                IsRevision = cart.IsRevision,
                Status = cart.Status,
                ReviewComment = cart.ReviewComment,
                RevisionNo = cart.RevisionNo
            };

            if (cart.IsRevision && cart.RequestId.HasValue)
            {
                var existing = await _db.Requests.AsNoTracking().FirstAsync(x => x.Id == cart.RequestId.Value);
                model.Fund = existing.Fund;
                model.FundSpecific = existing.FundSpecific;
                model.FPP = existing.FPP;
                model.Purpose = existing.Purpose;
                model.RequestedBy = existing.RequestedBy;
                model.RequestedDesig = existing.RequestedDesig;
                model.Availability = existing.Availability;
                model.AvaialbilityDesig = existing.AvaialbilityDesig;
                model.ApprovedBy = existing.ApprovedBy;
                model.ApprovedDesig = existing.ApprovedDesig;
                model.CtrlNo = existing.CtrlNo;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Checkout(PurchaseRequestViewModel model)
        {
            var cart = GetCart();

            // Do not use Items, costs, or quantities posted by the browser.
            model.Items = cart.Items;
            model.RequestId = cart.RequestId;
            model.IsRevision = cart.IsRevision;
            model.Status = cart.Status;
            model.ReviewComment = cart.ReviewComment;
            model.RevisionNo = cart.RevisionNo;

            if (!CanWriteRevisionCart(cart))
                ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");

            var expectedToken = Session[CheckoutTokenSessionKey] as string;
            if (String.IsNullOrWhiteSpace(expectedToken) ||
                !String.Equals(expectedToken, model.CheckoutToken, StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    "",
                    "This checkout has expired or was already submitted. Please return to your cart and try again.");
            }

            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Your procurement cart is empty.");
            }

            var departments = await GetAuthorizedDepartmentsAsync();
            var selectedDepartment = cart.DepartmentId.HasValue
                ? departments.FirstOrDefault(x => x.Id == cart.DepartmentId.Value)
                : null;

            if (selectedDepartment == null || !cart.FiscalYear.HasValue)
            {
                ModelState.AddModelError("", "The cart department or fiscal year is no longer valid.");
            }
            else
            {
                ModelState.Remove("DeptId");
                ModelState.Remove("Department");
                ModelState.Remove("ProcurementFiscalYear");
                model.DeptId = selectedDepartment.Id;
                model.Department = selectedDepartment.Description;
                model.ProcurementFiscalYear = cart.FiscalYear.Value;
            }

            foreach (var cartItem in cart.Items)
            {
                foreach (var subItem in EnsureSubItems(cartItem))
                {
                    if (String.IsNullOrWhiteSpace(subItem.Description) ||
                        String.IsNullOrWhiteSpace(subItem.Unit) ||
                        subItem.Quantity <= 0 ||
                        subItem.UnitCost < 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "All sub-items under " + cartItem.Code + " must have a description, unit, positive quantity, and non-negative unit cost.");
                        break;
                    }
                }

                var ppmpItem = await _ppmpService.PPMPItem.GetByIdAsync(cartItem.Id);

                if (ppmpItem == null)
                {
                    ModelState.AddModelError(
                        "",
                        cartItem.Code + " no longer exists in the procurement plan."
                    );

                    continue;
                }

                if (selectedDepartment != null &&
                    (ppmpItem.PPMP == null ||
                     ppmpItem.PPMP.DeptId != selectedDepartment.Id ||
                     ppmpItem.PPMP.ForYear != cart.FiscalYear))
                {
                    ModelState.AddModelError(
                        "",
                        cartItem.Code + " is outside the cart procurement plan.");
                    continue;
                }

                var ownUsage = cart.IsRevision && cart.RequestId.HasValue
                    ? await _db.PPMPItemUsages.Where(x => x.PrId == cart.RequestId.Value && x.PpmpItemId == cartItem.Id)
                        .Select(x => x.Qty).FirstOrDefaultAsync()
                    : 0;
                var availableQuantity = ppmpItem.QtyBal.GetValueOrDefault() + ownUsage.GetValueOrDefault();
                if (cartItem.Quantity <= 0 || cartItem.Quantity > availableQuantity)
                {
                    ModelState.AddModelError(
                        "",
                        cartItem.Code + " exceeds the current available quantity of " +
                        availableQuantity + "."
                    );
                }

                if (!ppmpItem.UnitCost.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        cartItem.Code + " has no unit cost."
                    );
                }

                cartItem.Code = ppmpItem.Code;
                cartItem.Unit = ppmpItem.Unit;
                cartItem.UnitCost = ppmpItem.UnitCost;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            /*
               Create the Purchase Request and its request lines here using
               _requestService. Use model.Purpose, model.Department, model.FundCluster,
               model.RequestedBy, and cart.Items.

               Do not use the PurchaseRequestNo posted by the client. Generate it
               server-side in the service/database.
            */

            // Example:
            // var requestId = await _requestService.CreateAsync(model, cart.Items);
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var pr = cart.IsRevision
                ? await _requestService.SaveRevisionCheckoutAsync(model, user, DateTime.Now)
                : await _requestService.SaveCheckoutAsync(model, user, DateTime.Now);

            Session.Remove(CheckoutTokenSessionKey);
            SaveCart(new CartViewModel());

            TempData["Message"] = cart.IsRevision
                ? $"Purchase request revision {pr.RevisionNo} was resubmitted successfully. Control Number is {pr.CtrlNo}."
                : $"Purchase request was submitted successfully. Control Number is {pr.CtrlNo}.";

            return RedirectToAction("Annual");
        }

        private async Task<List<Codextn>> GetAuthorizedDepartmentsAsync()
        {
            var userId = User.Identity.GetUserId();
            return (await _codextnService.GetUserDepartmentsAsync(userId))
                .OrderBy(x => x.Code)
                .ToList();
        }

        private bool CanWriteRevisionCart(CartViewModel cart)
        {
            if (cart == null || !cart.IsRevision)
                return true;
            if (!cart.RequestId.HasValue || cart.RequestId.Value == Guid.Empty)
                return false;

            var requestExists = _db.Requests.AsNoTracking().Any(x => x.Id == cart.RequestId.Value);
            if (!requestExists || !String.Equals(cart.RevisionUser, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                return false;
            var status = _db.DocumentStatusHistories.AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == cart.RequestId.Value)
                .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus).FirstOrDefault();
            return String.Equals(status, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase);
        }

        private CartViewModel GetCart()
        {
            var cart = Session[CartSessionKey] as CartViewModel;

            if (cart == null)
            {
                cart = new CartViewModel();
            }
            else if (cart.Items == null)
            {
                cart.Items = new List<CartItemViewModel>();
            }

            RenumberCartItems(cart);
            Session[CartSessionKey] = cart;

            return cart;
        }

        private void SaveCart(CartViewModel cart)
        {
            Session[CartSessionKey] = cart;
        }

        [HttpGet]
        public JsonResult CartCount()
        {
            var cart = GetCart();

            return Json(new
            {
                cartCount = cart.Items.Count
            }, JsonRequestBehavior.AllowGet);
        }

        private static void RenumberCartItems(CartViewModel cart)
        {
            for (var index = 0; index < cart.Items.Count; index++)
            {
                cart.Items[index].ItemNo = (index + 1).ToString();
                RenumberCartSubItems(cart.Items[index]);
            }
        }

        private static IList<CartSubItemViewModel> EnsureSubItems(CartItemViewModel cartItem)
        {
            if (cartItem.SubItems == null)
            {
                cartItem.SubItems = new List<CartSubItemViewModel>();
            }

            return cartItem.SubItems;
        }

        private static void RenumberCartSubItems(CartItemViewModel cartItem)
        {
            var subItems = EnsureSubItems(cartItem);
            for (var index = 0; index < subItems.Count; index++)
            {
                subItems[index].ParentItemId = cartItem.Id;
                subItems[index].ItemNo = cartItem.ItemNo + "." + (index + 1);
            }
        }
    }
}
