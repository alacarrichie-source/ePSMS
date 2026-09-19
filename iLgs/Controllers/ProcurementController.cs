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

        public async Task<ActionResult> Annual(int? fiscalYear, Guid? department, string category, string searchText, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, "requests");
            if (!access.IsAllowed)
            {
                return new HttpStatusCodeResult(403, "Access to Procurement and Purchase Requests is denied.");
            }

            var isRevision = string.Equals(mode, CartModes.Revision, StringComparison.OrdinalIgnoreCase);
            var isDraftResume = string.Equals(mode, CartModes.Normal, StringComparison.OrdinalIgnoreCase) &&
                                requestId.HasValue &&
                                requestId.Value != Guid.Empty;
            CartViewModel activeCart;

            if (isRevision)
            {
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    TempData["Error"] = "A valid Purchase Request is required to revise items.";
                    return RedirectToAction("Index", "Requests");
                }

                var revisionCartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Revision, requestId.Value);
                if (revisionCartEntity == null)
                {
                    TempData["Error"] = "The revision session for this Purchase Request was not found or is no longer active.";
                    return RedirectToAction("Index", "Requests");
                }

                activeCart = await _cartService.GetActiveCartViewModelAsync(userId, CartModes.Revision, requestId.Value);
                if (!CanWriteRevisionCart(activeCart))
                {
                    TempData["Error"] = "This Purchase Request is no longer available for revision.";
                    return RedirectToAction("Index", "Requests");
                }

                fiscalYear = activeCart.FiscalYear;
                department = activeCart.DepartmentId;
            }
            else if (isDraftResume)
            {
                activeCart = await _cartService.GetActiveCartViewModelAsync(
                    userId,
                    CartModes.Normal,
                    requestId.Value);

                if (!activeCart.RequestId.HasValue || !CanWriteRevisionCart(activeCart))
                {
                    TempData["Error"] = "This Draft Purchase Request is no longer available to continue.";
                    return RedirectToAction("Index", "Requests");
                }

                fiscalYear = activeCart.FiscalYear;
                department = activeCart.DepartmentId;
            }
            else
            {
                activeCart = await _cartService.GetActiveWorkingNormalCartViewModelAsync(userId);

                if (activeCart.RequestId.HasValue && activeCart.RequestId.Value != Guid.Empty)
                {
                    isDraftResume = true;
                    requestId = activeCart.RequestId;

                    if (!CanWriteRevisionCart(activeCart))
                    {
                        TempData["Error"] = "This Draft Purchase Request is no longer available to continue.";
                        return RedirectToAction("Index", "Requests");
                    }

                    fiscalYear = activeCart.FiscalYear;
                    department = activeCart.DepartmentId;
                }
                else
                {
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
                }
            }

            var departments = (await _codextnService.GetUserDepartmentsAsync(userId))
                .OrderBy(x => x.Code)
                .ToList();

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
                IsDraftResume = isDraftResume,
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
        public async Task<ActionResult> AddToCart(Guid id, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, "requests");
            var isRevision = string.Equals(mode, CartModes.Revision, StringComparison.OrdinalIgnoreCase);
            var isDraftResume = string.Equals(mode, CartModes.Normal, StringComparison.OrdinalIgnoreCase) &&
                                requestId.HasValue &&
                                requestId.Value != Guid.Empty;
            CartViewModel cart;

            if (isRevision)
            {
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "A valid Purchase Request is required for revision." });
                }

                cart = await _cartService.GetActiveCartViewModelAsync(userId, CartModes.Revision, requestId.Value);
                if (!CanWriteRevisionCart(cart))
                {
                    Response.StatusCode = 403;
                    return Json(new { success = false, message = "This Purchase Request is no longer available for revision." });
                }
            }
            else if (isDraftResume)
            {
                cart = await _cartService.GetActiveCartViewModelAsync(userId, CartModes.Normal, requestId.Value);
                if (!CanWriteRevisionCart(cart))
                {
                    Response.StatusCode = 403;
                    return Json(new { success = false, message = "This Draft Purchase Request is no longer available to edit." });
                }
            }
            else
            {
                cart = await _cartService.GetActiveWorkingNormalCartViewModelAsync(userId);
                if (cart.RequestId.HasValue && cart.RequestId.Value != Guid.Empty)
                {
                    isDraftResume = true;
                    requestId = cart.RequestId;

                    if (!CanWriteRevisionCart(cart))
                    {
                        Response.StatusCode = 403;
                        return Json(new { success = false, message = "This Draft Purchase Request is no longer available to edit." });
                    }
                }
            }

            var hasRequiredAccess = isRevision || isDraftResume
                ? access.AllowEdit
                : access.AllowAdd;

            if (!access.IsAllowed || !hasRequiredAccess)
            {
                Response.StatusCode = 403;
                return Json(new
                {
                    success = false,
                    message = (isRevision || isDraftResume)
                        ? "Purchase Request edit access denied."
                        : "Add to cart access denied."
                });
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

            var targetDept = (isRevision || isDraftResume)
                ? cart.DepartmentId
                : (filter != null ? filter.Department : null);
            var targetYear = (isRevision || isDraftResume)
                ? cart.FiscalYear
                : (filter != null ? filter.FiscalYear : null);

            if (!targetDept.HasValue ||
                !targetYear.HasValue ||
                !departments.Any(x => x.Id == targetDept.Value) ||
                item.PPMP == null ||
                item.PPMP.DeptId != targetDept ||
                item.PPMP.ForYear != targetYear)
            {
                Response.StatusCode = 403;
                return Json(new
                {
                    success = false,
                    message = "The item is outside your active procurement plan."
                });
            }

            if (cart.Items.Any() &&
                (cart.DepartmentId != targetDept || cart.FiscalYear != targetYear))
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
                var targetMode = isRevision ? CartModes.Revision : CartModes.Normal;
                var targetRequestId = (isRevision || isDraftResume) ? requestId : null;
                var updatedCart = await _cartService.AddItemAsync(userId, id, User.Identity.Name, targetDept, targetYear, targetMode, targetRequestId);
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
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    TempData["Error"] = "Admin Edit context is missing or no longer available.";
                    return RedirectToAction("Posting", "Requests");
                }

                var adminAccess = await Access(userId, "requests");
                if (!adminAccess.IsAllowed || (!adminAccess.IsAdmin && !adminAccess.AllowEdit && !adminAccess.AllowPost))
                {
                    TempData["Error"] = "You are not authorized to perform an administrative edit.";
                    return RedirectToAction("Posting", "Requests");
                }

                var cartEntity = cartId.HasValue && cartId.Value != Guid.Empty
                    ? await _cartService.GetActiveCartEntityByIdAsync(userId, cartId.Value, CartModes.AdminEdit, requestId.Value)
                    : await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, requestId.Value);
                if (cartEntity == null || ProcurementCartService.GetCartMode(cartEntity) != CartModes.AdminEdit)
                {
                    TempData["Error"] = "Admin Edit cart not found or has expired.";
                    return RedirectToAction("Posting", "Requests");
                }

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
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    TempData["Message"] = "Revision context is missing or no longer available.";
                    return RedirectToAction("Index", "Requests");
                }

                var cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Revision, requestId.Value);
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

            // My Cart is the user's one active NORMAL working cart.
            // It may represent a new PR or an existing Draft PR.
            CartViewModel normalCart;

            if (requestId.HasValue && requestId.Value != Guid.Empty)
            {
                if (cartId.HasValue && cartId.Value != Guid.Empty)
                {
                    var normalEntity = await _cartService.GetActiveCartEntityByIdAsync(
                        userId,
                        cartId.Value,
                        CartModes.Normal,
                        requestId.Value);

                    normalCart = normalEntity == null
                        ? new CartViewModel()
                        : _cartService.MapEntityToViewModel(normalEntity);
                }
                else
                {
                    normalCart = await _cartService.GetActiveCartViewModelAsync(
                        userId,
                        CartModes.Normal,
                        requestId);
                }
            }
            else
            {
                normalCart = await _cartService.GetActiveWorkingNormalCartViewModelAsync(userId);
                if (normalCart.RequestId.HasValue)
                {
                    requestId = normalCart.RequestId;
                }
            }

            if (normalCart.RequestId.HasValue && !CanWriteRevisionCart(normalCart))
            {
                TempData["Message"] = "This Draft Purchase Request is no longer available to continue.";
                return RedirectToAction("Index", "Requests");
            }

            return View(normalCart);
        }

        [HttpPost]
        public async Task<ActionResult> CartSubItemsRead(
            [DataSourceRequest] DataSourceRequest request,
            Guid parentItemId, string mode = null, Guid? requestId = null)
        {
            var userId = User.Identity.GetUserId();
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
        public async Task<ActionResult> CartRead([DataSourceRequest] DataSourceRequest request, string mode = null, Guid? requestId = null, Guid? cartId = null)
        {
            var userId = User.Identity.GetUserId();
            CartViewModel cart;

            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) &&
                cartId.HasValue && cartId.Value != Guid.Empty)
            {
                var cartEntity = await _cartService.GetActiveCartEntityByIdAsync(
                    userId,
                    cartId.Value,
                    CartModes.AdminEdit,
                    requestId);
                cart = cartEntity == null
                    ? new CartViewModel()
                    : _cartService.MapEntityToViewModel(cartEntity);
            }
            else
            {
                cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            }

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
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    ModelState.AddModelError("", "Admin Edit context is missing.");
                    return Json(new[] { updatedItem }.ToDataSourceResult(request, ModelState));
                }

                var adminAccess = await Access(userId, "requests");
                if (!adminAccess.IsAllowed || (!adminAccess.IsAdmin && !adminAccess.AllowEdit && !adminAccess.AllowPost))
                {
                    ModelState.AddModelError("", "You are not authorized to perform an administrative edit.");
                    return Json(new[] { updatedItem }.ToDataSourceResult(request, ModelState));
                }

                var cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, requestId.Value);
                if (cartEntity == null)
                {
                    ModelState.AddModelError("", "Admin Edit cart not found or has expired.");
                    return Json(new[] { updatedItem }.ToDataSourceResult(request, ModelState));
                }

                var cart = _cartService.MapEntityToViewModel(cartEntity);
                if (!CanWriteAdminEditCart(cart))
                {
                    ModelState.AddModelError("", "This Purchase Request is no longer available for administrative edit.");
                    return Json(new[] { updatedItem }.ToDataSourceResult(request, ModelState));
                }

                var cartItem = cart.Items.SingleOrDefault(x => x.Id == updatedItem.Id);
                if (cartItem == null)
                {
                    ModelState.AddModelError("", "The cart item no longer exists.");
                    return Json(new[] { updatedItem }.ToDataSourceResult(request, ModelState));
                }

                if (cartItem.RequestItemId.HasValue)
                {
                    var reqItemExists = await _db.RequestItems.AnyAsync(ri => ri.Id == cartItem.RequestItemId.Value && ri.PrId == cart.RequestId.Value);
                    if (!reqItemExists)
                    {
                        ModelState.AddModelError("", "The item does not belong to the Purchase Request being edited.");
                        return Json(new[] { updatedItem }.ToDataSourceResult(request, ModelState));
                    }
                }

                if (string.IsNullOrWhiteSpace(updatedItem.Description))
                {
                    ModelState.AddModelError("Description", "Description is required.");
                }

                if (updatedItem.Quantity <= 0)
                {
                    ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
                }

                if (string.IsNullOrWhiteSpace(updatedItem.Unit))
                {
                    ModelState.AddModelError("Unit", "Unit is required.");
                }
                else
                {
                    var trimmedUnit = updatedItem.Unit.Trim();
                    var validUnit = await _db.Codextns.AnyAsync(c => c.CodeMast.Code == "UNIT" && c.Code == trimmedUnit);
                    if (!validUnit)
                    {
                        ModelState.AddModelError("Unit", string.Format("Invalid unit '{0}'. Please select a valid unit.", trimmedUnit));
                    }
                }

                if (!updatedItem.UnitCost.HasValue || updatedItem.UnitCost.Value <= 0)
                {
                    ModelState.AddModelError("UnitCost", "Unit Cost must be greater than zero.");
                }
                else
                {
                    var defaultUnitCost = await _db.PPMPItems
                        .Where(x => x.Id == updatedItem.Id)
                        .Select(x => x.UnitCost)
                        .FirstOrDefaultAsync();

                    if (!defaultUnitCost.HasValue || defaultUnitCost.Value <= 0)
                    {
                        ModelState.AddModelError("UnitCost", "The Annual Procurement item does not have a valid default Unit Cost.");
                    }
                    else if (updatedItem.UnitCost.Value > defaultUnitCost.Value)
                    {
                        ModelState.AddModelError(
                            "UnitCost",
                            string.Format(
                                "Unit Cost cannot exceed the Annual Procurement default Unit Cost of {0:N2}.",
                                defaultUnitCost.Value));
                    }
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        var result = await _cartService.UpdateItemAsync(
                            userId, updatedItem.Id, updatedItem.Quantity, updatedItem.Description, User.Identity.Name, mode, requestId, updatedItem.Unit, updatedItem.UnitCost);
                        if (result != null)
                        {
                            cartItem = result;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                    }
                }

                return Json(new[] { cartItem ?? updatedItem }.ToDataSourceResult(request, ModelState));
            }

            var normalCart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            if (!CanWriteRevisionCart(normalCart))
            {
                ModelState.AddModelError(
                    "",
                    normalCart.RequestId.HasValue
                        ? "This Draft Purchase Request is no longer available to edit."
                        : "This procurement cart is no longer available.");
            }

            var normItem = normalCart.Items.SingleOrDefault(x => x.Id == updatedItem.Id);
            var isDraftCart = normalCart.RequestId.HasValue &&
                              normalCart.RequestId.Value != Guid.Empty &&
                              !normalCart.IsRevision &&
                              !normalCart.IsAdminEdit;

            if (normItem == null)
            {
                ModelState.AddModelError("", "The cart item no longer exists.");
            }

            if (updatedItem.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            }

            if (string.IsNullOrWhiteSpace(updatedItem.Description))
            {
                ModelState.AddModelError("Description", "Description is required.");
            }

            if (isDraftCart)
            {
                if (string.IsNullOrWhiteSpace(updatedItem.Unit))
                {
                    ModelState.AddModelError("Unit", "Unit is required.");
                }
                else
                {
                    var trimmedUnit = updatedItem.Unit.Trim();
                    var validUnit = await _db.Codextns.AnyAsync(c =>
                        c.CodeMast.Code == "UNIT" &&
                        c.Code == trimmedUnit);

                    if (!validUnit)
                    {
                        ModelState.AddModelError(
                            "Unit",
                            string.Format("Invalid unit '{0}'. Please select a valid unit.", trimmedUnit));
                    }
                }

                if (!updatedItem.UnitCost.HasValue || updatedItem.UnitCost.Value <= 0)
                {
                    ModelState.AddModelError("UnitCost", "Unit Cost must be greater than zero.");
                }
                else
                {
                    var defaultUnitCost = await _db.PPMPItems
                        .Where(x => x.Id == updatedItem.Id)
                        .Select(x => x.UnitCost)
                        .FirstOrDefaultAsync();

                    if (!defaultUnitCost.HasValue || defaultUnitCost.Value <= 0)
                    {
                        ModelState.AddModelError("UnitCost", "The Annual Procurement item does not have a valid default Unit Cost.");
                    }
                    else if (updatedItem.UnitCost.Value > defaultUnitCost.Value)
                    {
                        ModelState.AddModelError(
                            "UnitCost",
                            string.Format(
                                "Unit Cost cannot exceed the Annual Procurement default Unit Cost of {0:N2}.",
                                defaultUnitCost.Value));
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _cartService.UpdateItemAsync(
                        userId,
                        updatedItem.Id,
                        updatedItem.Quantity,
                        updatedItem.Description,
                        User.Identity.Name,
                        mode,
                        requestId,
                        isDraftCart ? updatedItem.Unit : null,
                        isDraftCart ? updatedItem.UnitCost : null);

                    if (result != null)
                    {
                        normItem = result;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return Json(new[] { normItem ?? updatedItem }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveCartItem(
            [DataSourceRequest] DataSourceRequest request,
            CartItemViewModel item, string mode = null, Guid? requestId = null)
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
        public async Task<JsonResult> CartSummary(string mode = null, Guid? requestId = null, Guid? cartId = null)
        {
            var userId = User.Identity.GetUserId();
            CartViewModel cart;

            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase) &&
                cartId.HasValue && cartId.Value != Guid.Empty)
            {
                var cartEntity = await _cartService.GetActiveCartEntityByIdAsync(
                    userId,
                    cartId.Value,
                    CartModes.AdminEdit,
                    requestId);
                cart = cartEntity == null
                    ? new CartViewModel()
                    : _cartService.MapEntityToViewModel(cartEntity);
            }
            else
            {
                cart = await _cartService.GetActiveCartViewModelAsync(userId, mode, requestId);
            }

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
            var count = await _cartService.GetWorkingNormalCartCountAsync(userId);

            return Json(new
            {
                cartCount = count
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<ActionResult> Checkout(string mode = null, Guid? requestId = null, Guid? cartId = null)
        {
            var userId = User.Identity.GetUserId();

            ProcurementCart cartEntity = null;
            if (string.Equals(mode, CartModes.AdminEdit, StringComparison.OrdinalIgnoreCase))
            {
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    TempData["Error"] = "Admin Edit context is missing or no longer available.";
                    return RedirectToAction("Posting", "Requests");
                }

                var adminAccess = await Access(userId, "requests");
                if (!adminAccess.IsAllowed || (!adminAccess.IsAdmin && !adminAccess.AllowEdit && !adminAccess.AllowPost))
                {
                    TempData["Error"] = "You are not authorized to perform an administrative edit.";
                    return RedirectToAction("Posting", "Requests");
                }

                cartEntity = cartId.HasValue && cartId.Value != Guid.Empty
                    ? await _cartService.GetActiveCartEntityByIdAsync(userId, cartId.Value, CartModes.AdminEdit, requestId.Value)
                    : await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, requestId.Value);
                if (cartEntity == null)
                {
                    TempData["Error"] = "Admin Edit cart not found or has expired.";
                    return RedirectToAction("Posting", "Requests");
                }
            }
            else if (string.Equals(mode, CartModes.Revision, StringComparison.OrdinalIgnoreCase))
            {
                if (!requestId.HasValue || requestId.Value == Guid.Empty)
                {
                    TempData["Message"] = "Revision context is missing or no longer available.";
                    return RedirectToAction("Index", "Requests");
                }

                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Revision, requestId.Value);
                if (cartEntity == null)
                {
                    TempData["Message"] = "Revision cart not found or has expired.";
                    return RedirectToAction("Index", "Requests");
                }
            }
            else
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Normal, requestId);
            }

            if (cartEntity == null)
            {
                TempData["Message"] = "Your procurement cart is empty.";
                return RedirectToAction("Annual");
            }
            var cart = _cartService.MapEntityToViewModel(cartEntity);

            if (!CanWriteRevisionCart(cart))
            {
                await _cartService.AbandonCartAsync(cartEntity.Id, User.Identity.Name);
                TempData["Message"] = cart.IsRevision
                    ? "This Purchase Request is no longer available for revision."
                    : "This Draft Purchase Request is no longer available to continue.";
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
            if (cart.IsAdminEdit)
            {
                if (!access.IsAdmin && !access.AllowEdit && !access.AllowPost)
                {
                    return new HttpStatusCodeResult(403, "You are not authorized to perform an administrative edit.");
                }
            }
            else if (cart.IsRevision)
            {
                if (!access.AllowEdit)
                {
                    return new HttpStatusCodeResult(403, "You are not authorized to edit Purchase Requests.");
                }
            }
            else
            {
                var isExistingDraft = cart.RequestId.HasValue && cart.RequestId.Value != Guid.Empty;
                if (isExistingDraft)
                {
                    if (!access.AllowEdit || !CanWriteRevisionCart(cart))
                    {
                        return new HttpStatusCodeResult(403, "Only the original requester can continue this Draft Purchase Request.");
                    }
                }
                else if (!access.AllowAdd)
                {
                    return new HttpStatusCodeResult(403, "You are not authorized to create Purchase Requests.");
                }
            }

            var departments = await GetAuthorizedDepartmentsAsync();
            var selectedDepartment = departments.FirstOrDefault(x => x.Id == cart.DepartmentId.Value);
            if (selectedDepartment == null && cart.IsAdminEdit && cart.DepartmentId.HasValue)
            {
                selectedDepartment = await _db.Codextns.FirstOrDefaultAsync(x => x.Id == cart.DepartmentId.Value);
            }
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
                CartMode = cart.CartMode,
                SourceRequestId = cart.SourceRequestId,
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

            if (cart.RequestId.HasValue)
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
                if (cart.IsAdminEdit)
                {
                    model.AdminEditPrNo = existing.PrNo;
                    model.AdminEditCtrlNo = existing.CtrlNo;
                    model.AdminEditDepartment = existing.Department;
                    model.AdminEditStatus = cart.AdminEditStatus;
                }
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
                if (!model.SourceRequestId.HasValue || model.SourceRequestId.Value == Guid.Empty)
                {
                    ModelState.AddModelError("", "Admin Edit context is missing or no longer available.");
                    return View(model);
                }

                var adminAccess = await Access(userId, "requests");
                if (!adminAccess.IsAllowed || (!adminAccess.IsAdmin && !adminAccess.AllowEdit && !adminAccess.AllowPost))
                {
                    ModelState.AddModelError("", "You are not authorized to perform an administrative edit.");
                    return View(model);
                }

                cartEntity = model.CartId.HasValue && model.CartId.Value != Guid.Empty
                    ? await _cartService.GetActiveCartEntityByIdAsync(userId, model.CartId.Value, CartModes.AdminEdit, model.SourceRequestId.Value)
                    : await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, model.SourceRequestId.Value);
            }
            else if (string.Equals(model.CartMode, CartModes.Revision, StringComparison.OrdinalIgnoreCase))
            {
                if (!model.SourceRequestId.HasValue || model.SourceRequestId.Value == Guid.Empty)
                {
                    ModelState.AddModelError("", "Revision context is missing or no longer available.");
                    return View(model);
                }

                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Revision, model.SourceRequestId.Value);
            }
            else
            {
                cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.Normal, model.SourceRequestId);
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
            else if (!cart.IsRevision)
            {
                var isExistingDraft = cart.RequestId.HasValue && cart.RequestId.Value != Guid.Empty;
                if (isExistingDraft)
                {
                    if (!access.AllowEdit || !CanWriteRevisionCart(cart))
                    {
                        return new HttpStatusCodeResult(403, "Only the original requester can submit this Draft Purchase Request.");
                    }
                }
                else if (!access.AllowAdd)
                {
                    return new HttpStatusCodeResult(403, "You are not authorized to create Purchase Requests.");
                }
            }
            if (cart.IsRevision && !access.AllowEdit)
            {
                return new HttpStatusCodeResult(403, "You are not authorized to edit Purchase Requests.");
            }
            if (cart.IsRevision)
            {
                if (!string.Equals(cart.RevisionUser, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
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
                ModelState.AddModelError(
                    "",
                    cart.IsRevision
                        ? "This Purchase Request is no longer available for revision."
                        : "This Draft Purchase Request is no longer available to continue.");
            }

            var expectedToken = Session[CheckoutTokenSessionKey] as string;
            if (!string.IsNullOrWhiteSpace(expectedToken) &&
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
            if (selectedDepartment == null && cart.IsAdminEdit && cart.DepartmentId.HasValue)
            {
                selectedDepartment = await _db.Codextns.FirstOrDefaultAsync(x => x.Id == cart.DepartmentId.Value);
            }

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

                var ownUsage = cart.RequestId.HasValue
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
                if (!cart.IsAdminEdit)
                {
                    cartItem.Unit = ppmpItem.Unit;
                    cartItem.UnitCost = ppmpItem.UnitCost;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string user = ControllerContext.HttpContext.User.Identity.Name;
            var isExistingDraftCart = !cart.IsAdminEdit &&
                                      !cart.IsRevision &&
                                      cart.RequestId.HasValue &&
                                      cart.RequestId.Value != Guid.Empty;

            var pr = cart.IsAdminEdit
                ? await _requestService.SaveAdminEditCheckoutAsync(model, user, DateTime.Now)
                : (cart.IsRevision
                    ? await _requestService.SaveRevisionCheckoutAsync(model, user, DateTime.Now)
                    : (isExistingDraftCart
                        ? await _requestService.SaveDraftCheckoutAsync(model, user, DateTime.Now)
                        : await _requestService.SaveCheckoutAsync(model, user, DateTime.Now)));

            Session.Remove(CheckoutTokenSessionKey);

            if (cart.IsAdminEdit)
            {
                var isPostAfter = string.Equals(Request.Form["PostAfterAdminEdit"], "true", StringComparison.OrdinalIgnoreCase) || string.Equals(Request.Form["PostAfterAdminEdit"], "on", StringComparison.OrdinalIgnoreCase);
                TempData["Message"] = isPostAfter
                    ? string.Format("Purchase Request {0} was updated and posted successfully.", pr.AdminEditPrNo ?? pr.CtrlNo)
                    : "Purchase Request updated successfully. Please review the changes before posting.";
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
            if (!requestId.HasValue || requestId.Value == Guid.Empty)
            {
                TempData["Error"] = "Admin Edit context is missing.";
                return RedirectToAction("Posting", "Requests");
            }
            var cartEntity = await _cartService.GetActiveCartEntityAsync(userId, CartModes.AdminEdit, requestId.Value);
            if (cartEntity != null && ProcurementCartService.GetCartMode(cartEntity) == CartModes.AdminEdit)
            {
                await _cartService.AbandonCartAsync(cartEntity.Id, User.Identity.Name);
            }
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
            if (cart == null)
            {
                return false;
            }

            if (cart.IsAdminEdit)
            {
                return true;
            }

            if (!cart.RequestId.HasValue || cart.RequestId.Value == Guid.Empty)
            {
                return !cart.IsRevision;
            }

            var requestRecord = _db.Requests
                .AsNoTracking()
                .Where(x => x.Id == cart.RequestId.Value)
                .Select(x => new
                {
                    x.InsertedBy,
                    x.SubmittedBy
                })
                .FirstOrDefault();

            if (requestRecord == null)
            {
                return false;
            }

            var status = _db.DocumentStatusHistories.AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == cart.RequestId.Value)
                .OrderByDescending(x => x.ChangedDt)
                .ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus)
                .FirstOrDefault();

            if (cart.IsRevision)
            {
                if (!string.Equals(cart.RevisionUser, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return string.Equals(status, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.Equals(requestRecord.InsertedBy, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                status = !string.IsNullOrWhiteSpace(requestRecord.SubmittedBy)
                    ? PrStatuses.Submitted
                    : PrStatuses.Draft;
            }

            return string.Equals(status, PrStatuses.Draft, StringComparison.OrdinalIgnoreCase);
        }
    }
}
