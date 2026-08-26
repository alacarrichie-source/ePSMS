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
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class ProcurementController : BaseController
    {
        private const string AnnualFilterSessionKey = "AnnualProcurementFilters";
        private const string CartSessionKey = "ProcurementCart";
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

            if (!departments.Any())
            {
                return new HttpStatusCodeResult(403, "No department is assigned to this user.");
            }

            department = department ?? departments.First().Id;

            Session[AnnualFilterSessionKey] = new AnnualFilterState
            {
                FiscalYear = fiscalYear,
                Department = department,
                Category = category ?? "",
                SearchText = searchText ?? ""
            };

            var items = _ppmpService.PPMPItem.GetAll()
                .Where(x => x.PPMP.DeptId == department && x.PPMP.ForYear == fiscalYear)
                .OrderBy(o => o.Code).ThenBy(o => o.Description)
                .ToList();

            if (!String.IsNullOrWhiteSpace(searchText))
            {
                items = items.Where(x =>
                    x.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.Code.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }

            var cart = GetCart();
            var cartItemIds = new HashSet<Guid>(cart.Items.Select(x => x.Id));

            foreach (var item in items)
            {
                item.IsInCart = cartItemIds.Contains(item.Id);
            }

            return View(new AnnualProcurementViewModel
            {
                FiscalYear = fiscalYear ?? 2026,
                Department = department,
                Category = category ?? "",
                SearchText = searchText,
                Items = items,
                CartCount = cart.Items.Count,
                FiscalYears = Options("2026", "2025", "2024"),
                Departments = departments,
                Categories = new[]
                {
            new SelectListItem { Text = "All Categories", Value = "" }
        }.Concat(Options("Office Supplies", "IT Equipment", "Furniture"))
            });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddToCart(Guid id)
        {
            var item = await _ppmpService.PPMPItem.GetByIdAsync(id);

            if (item == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Procurement item was not found."
                });
            }

            var cart = GetCart();

            if (cart.Items.Any(x => x.Id == item.Id))
            {
                return Json(new
                {
                    success = false,
                    alreadyInCart = true,
                    cartCount = cart.Items.Count,
                    message = "This item is already in your cart."
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
                Quantity = (int)item.QtyBal
            });

            RenumberCartItems(cart);
            SaveCart(cart);            

            return Json(new
            {
                success = true,
                cartCount = cart.Items.Count
            });
        }

        public ActionResult Cart() { return View(GetCart()); }

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

            var cartItem = cart.Items.SingleOrDefault(x => x.Id == updatedItem.Id);

            if (cartItem == null)
            {
                ModelState.AddModelError("", "The cart item no longer exists.");
            }
            else if (updatedItem.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1.");
            }
            else
            {
                // Get the current remaining quantity from your procurement-plan source.
                var procurementItem = await _ppmpService.PPMPItem.GetByIdAsync(updatedItem.Id);

                if (procurementItem == null)
                {
                    ModelState.AddModelError("", "The procurement item no longer exists.");
                }
                else if (updatedItem.Quantity > procurementItem.QtyBal)
                {
                    ModelState.AddModelError(
                        "Quantity",
                        "Quantity cannot exceed the remaining quantity of " +
                        procurementItem.QtyBal + "."
                    );
                }
                else
                {
                    // Only update quantity. Never trust the browser's item description or cost.
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

            var cartItem = cart.Items.SingleOrDefault(x => x.Id == item.Id);

            if (cartItem != null)
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

            if (!cart.Items.Any())
            {
                TempData["Message"] = "Your procurement cart is empty.";
                return RedirectToAction("Annual");
            }

            var hasIncomingFilters =
               Request.QueryString["fiscalYear"] != null ||
               Request.QueryString["department"] != null ||
               Request.QueryString["category"] != null ||
               Request.QueryString["searchText"] != null;

            var savedFilter = Session[AnnualFilterSessionKey] as AnnualFilterState;

            if (savedFilter == null)
            {
                TempData["Message"] = "Your department info is empty.";
                return RedirectToAction("Annual");
            }

            var deptId = savedFilter.Department;

            var userId = User.Identity.GetUserId();
            var departments = (await _codextnService.GetUserDepartmentsAsync(userId))
                .OrderBy(x => x.Code)
                .ToList();

            ViewBag.Departments = departments.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Code
            });

            return View(new PurchaseRequestViewModel
            {
                DeptId = deptId,
                Department = departments.FirstOrDefault(f => f.Id == deptId).Description,                
                Items = cart.Items
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Checkout(PurchaseRequestViewModel model)
        {
            var cart = GetCart();

            // Do not use Items, costs, or quantities posted by the browser.
            model.Items = cart.Items;

            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Your procurement cart is empty.");
            }

            foreach (var cartItem in cart.Items)
            {
                var ppmpItem = await _ppmpService.PPMPItem.GetByIdAsync(cartItem.Id);

                if (ppmpItem == null)
                {
                    ModelState.AddModelError(
                        "",
                        cartItem.Code + " no longer exists in the procurement plan."
                    );

                    continue;
                }

                if (ppmpItem.QtyBal <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        cartItem.Code + " no longer has an available quantity balance."
                    );
                }

                if (!cartItem.UnitCost.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        cartItem.Code + " has no unit cost."
                    );
                }
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
            var pr = await _requestService.SaveCheckoutAsync(model, user, DateTime.Now);

            SaveCart(new CartViewModel
            {
                Items = new List<CartItemViewModel>()
            });

            TempData["Message"] = $"Purchase request was submitted successfully. Control Number is {pr.CtrlNo}.";

            return RedirectToAction("Annual");
        }

        private static IEnumerable<SelectListItem> Options(params string[] values)
        {
            return values.Select(x => new SelectListItem { Text = x, Value = x });
        }

        private CartViewModel GetCartOld()
        {
            var cart = Session[CartSessionKey] as CartViewModel;

            if (cart == null)
            {
                cart = new CartViewModel
                {
                    Items = new List<CartItemViewModel>()
                };

                Session[CartSessionKey] = cart;
            }

            return cart;
        }

        private CartViewModel GetCart()
        {
            var cart = Session[CartSessionKey] as CartViewModel;

            if (cart == null)
            {
                cart = new CartViewModel
                {
                    Items = new List<CartItemViewModel>()
                };
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

        private List<CartItemViewModel> CurrentCart
        {
            get
            {
                var cart = Session[CartSessionKey] as List<CartItemViewModel>;

                if (cart == null)
                {
                    cart = new List<CartItemViewModel>();
                    Session[CartSessionKey] = cart;
                }

                return cart;
            }
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
            }
        }
    }
}