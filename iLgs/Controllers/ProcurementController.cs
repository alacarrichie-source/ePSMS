using iLgs.Ai.Services;
using iLgs.Models;
using iLgs.Services.Codes;
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
        private const string CheckoutTokenSessionKey = "ProcurementCheckoutToken";
        private readonly IOrderService _orderService;
        private readonly IRequestService _requestService;
        private readonly ICodextnService _codextnService;
        private readonly IPPMPService _ppmpService;
        private readonly ProcurementCartService _cartService;

        public ProcurementController()
        {
            _orderService = new OrderService(_db);
            _requestService = new RequestService(_db);
            _codextnService = new CodextnService(_db);
            _ppmpService = new PPMPService(_db);
            _cartService = new ProcurementCartService(_db);
        }

        [Serializable]
        private class AnnualFilterState
        {
            public int? FiscalYear { get; set; }
            public Guid? Department { get; set; }
            public string Category { get; set; }
            public string SearchText { get; set; }
        }

        public async Task<ActionResult> Annual(int? fiscalYear, Guid? department, string category, string searchText)
        {
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, "requests");
            if (!access.IsAllowed)
            {
                return new HttpStatusCodeResult(403, "Access to Procurement and Purchase Requests is denied.");
            }

            var activeAdminEditId = Session["ActiveAdminEditRequestId"] as Guid?;
            var activeCart = await _cartService.GetActiveCartViewModelAsync(userId, activeAdminEditId.HasValue ? CartModes.AdminEdit : null, activeAdminEditId);

            if (activeCart.IsAdminEdit)
            {
                TempData["Message"] = "New procurement items cannot be added while performing an administrative PR edit.";
                return RedirectToAction("Cart", new { mode = CartModes.AdminEdit, requestId = activeAdminEditId });
            }
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

            var departments = (await _codextnService.GetUserDepartmentsAsync(userId))
                .OrderBy(x => x.Code)
                .ToList();

            if (activeCart.IsRevision)
            {
                if (!CanWriteRevisionCart(activeCart))
                {
                    await _cartService.ClearCartAsync(userId, User.Identity.Name, CartModes.Revision, activeCart.RequestId);
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
                .Where(x => string.IsNullOrEmpty(category) || x.ProcMode == category);

            if (!string.IsNullOrWhiteSpace(searchText))
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

            var cartItemIds = new HashSet<Guid>(activeCart.Items.Select(x => x.Id));
            foreach (var item in items)
            {
                item.IsInCart = cartItemIds.Contains(item.Id);
            }

            var procurementModes = await availableItems
                .Where(x => x.PPMP.DeptId == department &&
                            x.PPMP.ForYear == fiscalYear &&
                            x.ProcMode != null &&
                            x.ProcMode != "")
                .Select(x => x.ProcMode)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return View(new AnnualProcurementViewModel
            {
                FiscalYear = fiscalYear.Value,
                Department = department.Value,
                Category = category ?? "",
                SearchText = searchText ?? "",
                Items = items,
                CartCount = activeCart.Items.Count,
                IsRevision = activeCart.IsRevision,
                RequestId = activeCart.RequestId,
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
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, "requests");
            if (!access.IsAllowed || !access.AllowEdit)
            {
                return Json(new { success = false, message = "Revision access denied." });
            }

            try
            {
                var revisionCart = await _cartService.StartRevisionCartAsync(id, userId, User.Identity.Name, access);
                Session[AnnualFilterSessionKey] = new AnnualFilterState
                {
                    FiscalYear = revisionCart.FiscalYear,
                    Department = revisionCart.DepartmentId,
                    Category = "",
                    SearchText = ""
                };
                return Json(new { success = true, redirectUrl = Url.Action("Cart", "Procurement", new { mode = CartModes.Revision, requestId = id }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddToCart(Guid id)
        {
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, "requests");
            if (!access.IsAllowed || !access.AllowAdd)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "Add to cart access denied." });
            }

            var cart = await _cartService.GetActiveCartViewModelAsync(userId);
            if (cart.IsAdminEdit)
            {
                return Json(new { success = false, message = "New procurement items cannot be added while performing an administrative PR edit." });
            }
            if (!CanWriteRevisionCart(cart))
            {
                return Json(new { success = false, message = "This Purchase Request is no longer available for revision." });
            }

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

            try
            {
                var updatedCart = await _cartService.AddItemAsync(userId, id, User.Identity.Name, filter.Department, filter.FiscalYear);
                return Json(new
                {
                    success = true,
                    cartCount = updatedCart.Items.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public async Task<ActionResult> Cart(string mode = null, Guid? requestId = null, Guid? cartId = null)
        {
            var userId = User.Identity.GetUserId();

            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                var resolvedRequestId = requestId ?? (Session["ActiveAdminEditRequestId"] as Guid?);

                var access = await Access(userId, "requests");
                if (!access.IsAllowed || (!access.IsAdmin && !access.AllowEdit && !access.AllowPost))
                {
                    TempData["Error"] = "You are not authorized to perform an administrative edit.";
                    return RedirectToAction("Posting", "Requests");
                }

                var cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, resolvedRequestId);
                if (cartEntity == null || ProcurementCartService.GetCartMode(cartEntity) != CartModes.AdminEdit)
                {
                    TempData["Error"] = "Admin Edit cart not found or has expired.";
                    return RedirectToAction("Posting", "Requests");
                }

                Session["ActiveAdminEditRequestId"] = cartEntity.RequestId;
                var cart = _cartService.MapEntityToViewModel(cartEntity);
                if (!CanWriteAdminEditCart(cart))
                {
                    TempData["Error"] = "This Purchase Request is no longer available for administrative edit.";
                    return RedirectToAction("Posting", "Requests");
                }
                return View(cart);
            }

            if (string.Equals(mode, CartModes.Revision, StringComparison.OrdinalIgnoreCase))
            {
                var resolvedRequestId = requestId ?? (Session["ActiveRevisionRequestId"] as Guid?);
                var cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Revision, resolvedRequestId);
                if (cartEntity == null)
                {
                    TempData["Message"] = "Revision cart not found or has expired.";
                    return RedirectToAction("Index", "Requests");
                }

                var cart = _cartService.MapEntityToViewModel(cartEntity);
                if (!CanWriteRevisionCart(cart))
                {
                    await _cartService.ClearCartAsync(userId, User.Identity.Name, CartModes.Revision, cart.RequestId);
                    TempData["Message"] = "This Purchase Request is no longer available for revision.";
                    return RedirectToAction("Index", "Requests");
                }
                return View(cart);
            }

            // Normal Cart (Default, e.g. clicking global navbar "My Cart")
            var normalCart = await _cartService.GetActiveCartViewModelAsync(userId, CartModes.Normal, null);
            return View(normalCart);
        }

        [HttpPost]
        public async Task<ActionResult> CartSubItemsRead(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) && !requestId.HasValue) { requestId = Session["ActiveAdminEditRequestId"] as Guid?; }
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            var cartItem = cart.Items.SingleOrDefault(x => x.Id == parentItemId);
            var subItems = cartItem == null
                ? new List<CartSubItemViewModel>()
                : cartItem.SubItems;

            return Json(subItems.ToDataSourceResult(request));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCartSubItem(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId,
            CartSubItemViewModel subItem, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            if (!CanWriteRevisionCart(cart))
            {
                ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            }

            var cartItem = cart.Items.SingleOrDefault(x => x.Id == parentItemId);

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
                try
                {
                    subItem = await _cartService.AddSubItemAsync(userId, parentItemId, subItem, User.Identity.Name, mode, requestId);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return Json(new[] { subItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateCartSubItem(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId,
            CartSubItemViewModel subItem, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            if (!CanWriteRevisionCart(cart))
            {
                ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    subItem = await _cartService.UpdateSubItemAsync(userId, parentItemId, subItem, User.Identity.Name, mode, requestId);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return Json(new[] { subItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveCartSubItem(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId,
            CartSubItemViewModel subItem, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            if (!CanWriteRevisionCart(cart))
            {
                ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _cartService.RemoveSubItemAsync(userId, parentItemId, subItem.Id, User.Identity.Name, mode, requestId);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return Json(new[] { subItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        public async Task<ActionResult> CartRead([DataSourceRequest] DataSourceRequest request, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) && !requestId.HasValue) { requestId = Session["ActiveAdminEditRequestId"] as Guid?; }
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);

            return Json(
                cart.Items.ToDataSourceResult(request),
                JsonRequestBehavior.AllowGet
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateCartItem(
            [DataSourceRequest] DataSourceRequest request,
            CartItemViewModel updatedItem, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) && !requestId.HasValue) { requestId = Session["ActiveAdminEditRequestId"] as Guid?; }
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            if (!CanWriteRevisionCart(cart))
            {
                ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            }

            var cartItem = cart.Items.SingleOrDefault(x => x.Id == updatedItem.Id);

            if (cartItem == null)
            {
                ModelState.AddModelError("", "The cart item no longer exists.");
            }
            else if (updatedItem.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            }
            else if (string.IsNullOrWhiteSpace(updatedItem.Description))
            {
                ModelState.AddModelError("Description", "Description is required.");
            }
            else
            {
                try
                {
                    var result = await _cartService.UpdateItemAsync(
                        userId, updatedItem.Id, updatedItem.Quantity, updatedItem.Description, User.Identity.Name, mode, requestId);
                    if (result != null)
                    {
                        cartItem = result;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Quantity", ex.Message);
                }
            }

            return Json(new[] { cartItem ?? updatedItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveCartItem(
            [DataSourceRequest] DataSourceRequest request,
            CartItemViewModel item, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) && !requestId.HasValue) { requestId = Session["ActiveAdminEditRequestId"] as Guid?; }
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            if (!CanWriteRevisionCart(cart))
            {
                ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _cartService.RemoveItemAsync(userId, item.Id, User.Identity.Name, mode, requestId);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return Json(new[] { item }.ToDataSourceResult(request, ModelState));
        }

        [HttpGet]
        public async Task<JsonResult> CartSummary(string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) && !requestId.HasValue) { requestId = Session["ActiveAdminEditRequestId"] as Guid?; }
            var cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);

            return Json(new
            {
                cartCount = cart.Items.Count,
                itemCount = cart.Items.Count,
                estimatedTotal = cart.EstimatedTotal
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> CartCount()
        {
            var userId = User.Identity.GetUserId();
            var count = await _cartService.GetCartCountAsync(userId);

            return Json(new
            {
                cartCount = count
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<ActionResult> Checkout(string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) && !requestId.HasValue)
            {
                requestId = Session["ActiveAdminEditRequestId"] as Guid?;
            }
            else if (string.Equals(mode, CartModes.Revision, StringComparison.OrdinalIgnoreCase) && !requestId.HasValue)
            {
                requestId = Session["ActiveRevisionRequestId"] as Guid?;
            }

            ProcurementCart cartEntity = null;
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, requestId);
            }
            else if (string.Equals(mode, CartModes.Revision, StringComparison.OrdinalIgnoreCase))
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Revision, requestId);
            }
            else
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Normal, null);
            }
            if (cartEntity == null)
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId);
            }
            if (cartEntity == null)
            {
                TempData["Message"] = "Your procurement cart is empty.";
                return RedirectToAction("Annual");
            }
            var cart = _cartService.MapEntityToViewModel(cartEntity);

            if (!CanWriteRevisionCart(cart))
            {
                await _cartService.ClearCartAsync(userId, User.Identity.Name, CartModes.Revision, cart.RequestId);
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

            var access = await Access(userId, "requests");
            if (!access.IsAllowed)
            {
                return new HttpStatusCodeResult(403, "Access to Purchase Requests is denied.");
            }
            if (!cart.IsRevision && !access.AllowAdd)
            {
                return new HttpStatusCodeResult(403, "You are not authorized to create Purchase Requests.");
            }
            if (cart.IsRevision && !access.AllowEdit)
            {
                return new HttpStatusCodeResult(403, "You are not authorized to edit Purchase Requests.");
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
                CartId = cartEntity.Id,
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
            var userId = User.Identity.GetUserId();
            ProcurementCart cartEntity = null;
            if (string.Equals(model.CartMode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                var reqId = model.SourceRequestId ?? (Session["ActiveAdminEditRequestId"] as Guid?);
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, reqId);
            }
            else if (string.Equals(model.CartMode, CartModes.Revision, StringComparison.OrdinalIgnoreCase))
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Revision, model.SourceRequestId);
            }
            else
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Normal, null);
            }
            if (cartEntity == null)
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId);
            }
            if (cartEntity == null)
            {
                ModelState.AddModelError("", "Your procurement cart is empty or has expired.");
                return View(model);
            }
            var cart = _cartService.MapEntityToViewModel(cartEntity);

            // Do not use Items, costs, or quantities posted by the browser.
            model.CartId = cartEntity.Id;
            model.Items = cart.Items;
            model.RequestId = cart.RequestId;
            model.IsRevision = cart.IsRevision;
            model.Status = cart.Status;
            model.ReviewComment = cart.ReviewComment;
            model.RevisionNo = cart.RevisionNo;
            model.CartMode = cart.CartMode;
            model.SourceRequestId = cart.SourceRequestId;

            var access = await Access(userId, "requests");
            if (!access.IsAllowed)
            {
                return new HttpStatusCodeResult(403, "Access to Purchase Requests is denied.");
            }
            if (cart.IsAdminEdit)
            {
                if (!access.AllowEdit && !access.IsAdmin)
                {
                    return new HttpStatusCodeResult(403, "You are not authorized to perform an administrative edit on Purchase Requests.");
                }
            }
            else if (!cart.IsRevision && !access.AllowAdd)
            {
                return new HttpStatusCodeResult(403, "You are not authorized to create Purchase Requests.");
            }
            if (cart.IsRevision && !access.AllowEdit)
            {
                return new HttpStatusCodeResult(403, "You are not authorized to edit Purchase Requests.");
            }
            if (cart.IsRevision)
            {
                if (!access.IsAdmin && !string.Equals(cart.RevisionUser, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpStatusCodeResult(403, "Only the original requester can submit this revision.");
                }
            }

            if (cart.IsAdminEdit)
            {
                if (!CanWriteAdminEditCart(cart))
                {
                    ModelState.AddModelError("", "This Purchase Request is no longer available for administrative edit.");
                }
            }
            else if (!CanWriteRevisionCart(cart))
            {
                ModelState.AddModelError("", "This Purchase Request is no longer available for revision.");
            }

            var expectedToken = Session[CheckoutTokenSessionKey] as string;
            if (string.IsNullOrWhiteSpace(expectedToken) ||
                !string.Equals(expectedToken, model.CheckoutToken, StringComparison.Ordinal))
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
                foreach (var subItem in cartItem.SubItems ?? Enumerable.Empty<CartSubItemViewModel>())
                {
                    if (string.IsNullOrWhiteSpace(subItem.Description) ||
                        string.IsNullOrWhiteSpace(subItem.Unit) ||
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

                var ownUsage = (cart.IsRevision || cart.IsAdminEdit) && cart.RequestId.HasValue
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

            string user = ControllerContext.HttpContext.User.Identity.Name;
            var pr = cart.IsAdminEdit
                ? await _requestService.SaveAdminEditCheckoutAsync(model, user, DateTime.Now)
                : (cart.IsRevision
                    ? await _requestService.SaveRevisionCheckoutAsync(model, user, DateTime.Now)
                    : await _requestService.SaveCheckoutAsync(model, user, DateTime.Now));

            Session.Remove(CheckoutTokenSessionKey);
            Session.Remove("ActiveAdminEditRequestId");

            if (cart.IsAdminEdit)
            {
                TempData["Message"] = "Purchase Request updated successfully. Please review the changes before posting.";
                return RedirectToAction("Posting", "Requests");
            }

            TempData["Message"] = cart.IsRevision
                ? string.Format("Purchase request revision {0} was resubmitted successfully. Control Number is {1}.", pr.RevisionNo, pr.CtrlNo)
                : string.Format("Purchase request was submitted successfully. Control Number is {0}.", pr.CtrlNo);

            return RedirectToAction("Annual");
        }

        private async Task<List<Codextn>> GetAuthorizedDepartmentsAsync()
        {
            var userId = User.Identity.GetUserId();
            return (await _codextnService.GetUserDepartmentsAsync(userId))
                .OrderBy(x => x.Code)
                .ToList();
        }

                [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public async Task<ActionResult> CancelAdminEdit(Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            var activeAdminEditId = requestId ?? (Session["ActiveAdminEditRequestId"] as Guid?);
            var cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, activeAdminEditId);
            if (cartEntity != null && ProcurementCartService.GetCartMode(cartEntity) == CartModes.AdminEdit)
            {
                await _cartService.AbandonCartAsync(cartEntity.Id, User.Identity.Name);
            }
            Session.Remove("ActiveAdminEditRequestId");
            TempData["Message"] = "Admin Edit cancelled. No changes were made to the Purchase Request.";
            return RedirectToAction("Posting", "Requests");
        }

        private bool CanWriteAdminEditCart(CartViewModel cart)
        {
            if (cart == null || !cart.IsAdminEdit)
            {
                return true;
            }
            if (!cart.RequestId.HasValue || cart.RequestId.Value == Guid.Empty)
            {
                return false;
            }

            var requestExists = _db.Requests.AsNoTracking().Any(x => x.Id == cart.RequestId.Value);
            if (!requestExists)
            {
                return false;
            }
            var status = _db.DocumentStatusHistories.AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == cart.RequestId.Value)
                .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus).FirstOrDefault();
            return string.Equals(status, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase);
        }

        private bool CanWriteRevisionCart(CartViewModel cart)
        {
            if (cart == null || !cart.IsRevision)
            {
                return true;
            }
            if (!cart.RequestId.HasValue || cart.RequestId.Value == Guid.Empty)
            {
                return false;
            }

            var requestExists = _db.Requests.AsNoTracking().Any(x => x.Id == cart.RequestId.Value);
            if (!requestExists || !string.Equals(cart.RevisionUser, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var status = _db.DocumentStatusHistories.AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == cart.RequestId.Value)
                .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus).FirstOrDefault();
            return string.Equals(status, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase);
        }
    }
}
