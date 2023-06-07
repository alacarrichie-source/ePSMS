using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using iLgs.Utilities;
using Newtonsoft.Json;
using iLgs.Services.Interfaces;
using iLgs.Services;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;

namespace iLgs.Controllers
{
    [AppAuthorize("ORDERS")]
    public class OrdersController : Controller
    {
        AppManEntities db = new AppManEntities();
        IOrderService orderService;
        IOrderItemService orderItemService;
        IOrderItemExtnService orderItemExtnService;
        IRequestService requestService;

        public OrdersController()
        {
            this.orderService = new OrderService(db);
            this.orderItemService = new OrderItemService(db);
            this.orderItemExtnService = new OrderItemExtnService(db);
            this.requestService = new RequestService(db);
        }

        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult OrderRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = orderService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> OrderCreate([DataSourceRequest] DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }

                if (await orderService.GetByPoNoAsync(model.PoNo) != null)
                {
                    ModelState.AddModelError("PoNo", "P.O. number already exists!");
                }
                else
                {
                    var pr = await requestService.GetByIdAsync(model.PrId);
                    if (pr == null)
                    {
                        ModelState.AddModelError("PrNo", "Invalid P.R. Number!");
                    }
                    else
                    {
                        if (pr.PrDate > model.PoDate)
                        {
                            ModelState.AddModelError("PoDate", "P.O. date must be greather than or equal to P.R. date!");
                        }
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderService.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> OrderUpdate([DataSourceRequest] DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (await orderService.GetAnyPoNoAsync(model.Id, model.PoNo))
                {
                    ModelState.AddModelError("PO No.", "P.O. number already exists!");
                }
                else
                {
                    var pr = await requestService.GetByIdAsync(model.PrId);
                    if (pr == null)
                    {
                        ModelState.AddModelError("PR No.", "Invalid P.R. Number!");
                    }
                    else if (await orderService.IsPostedAsync(model.Id))
                    {
                        ModelState.AddModelError("PO NO.", "PO Number already Posted, cannot update!");
                    }
                    else if (pr.PrDate > model.PoDate)
                    {
                        ModelState.AddModelError("PO Date", "P.O. date must be greather than or equal to P.R. date!");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> OrderDestroy([DataSourceRequest]DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await orderService.IsPostedAsync(model.Id))
                {
                    ModelState.AddModelError("DeleteError", "PO Number already Posted, cannot delete!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult _OrderItem(Guid orderId)
        {
            ViewData["orderId"] = orderId;
            return PartialView();
        }
        public async Task<ActionResult> _OrderItemAddEdit(Guid orderId, Guid? orderItemId)
        {
            var data = await orderItemService.GetByIdAsync(orderItemId);
            if (data == null)
            {
                data = new OrderItemVM()
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId
                };
            }            
            ViewData["orderItemId"] = orderItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemSave(OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "order");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await orderItemService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await orderItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await orderItemService.UpdateAsync(model, user, date);
                    }
                    var orderItemExtns = (List<OrderItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridOrderItemExtns, typeof(List<OrderItemExtnVM>));
                    await orderItemExtnService.SaveAsync(model.Id, orderItemExtns, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult _OrderItemRead([DataSourceRequest] DataSourceRequest request, Guid? orderId)
        {
            var data = orderItemService.GetByPoId(orderId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemCreate([DataSourceRequest] DataSourceRequest request, OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderItemService.CreateAsync(model, user, date);                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemUpdate([DataSourceRequest] DataSourceRequest request, OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderItemService.UpdateAsync(model, user, date);

                    // TO DO: update stock card
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemDestroy([DataSourceRequest]DataSourceRequest request, OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("DeleteError", "PO Number already Posted, cannot delete!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderItemService.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message.ToString());
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        #region EXTRAS

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostOrders(Guid orderId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.GetByIdAsync(orderId) == null)
                {
                    ModelState.AddModelError("Order", "Invalid Order Id");
                }
                else if (await orderService.IsPostedAsync(orderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot post again!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await orderService.PostAsync(orderId, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UnpostOrders(Guid orderId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.GetByIdAsync(orderId) == null)
                {
                    ModelState.AddModelError("Order", "Invalid Order Id");
                }
                else if (!(await orderService.IsPostedAsync(orderId)))
                {
                    ModelState.AddModelError("PO No.", "PO Number not yet posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await orderService.UnpostAsync(orderId, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPoYyyyMm(DateTime poDate)
        {
            var poYear = poDate.Date.Year.ToString().Trim();
            var poMonth = poDate.Date.Month.ToString().Trim().PadLeft(2, '0');
            return Json(new { PoYear = poYear, PoMonth = poMonth }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _OrderItemExtnBatchRead([DataSourceRequest] DataSourceRequest request, Guid? orderItemId, Guid? psCodeId)
        {
            var data = orderItemExtnService.GetBatchInfo(orderItemId, psCodeId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }


        #endregion

        #region PRINTOUTS
        public ActionResult PurchaseOrderRpt(string poNo)
        {
            string stringname = db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Po_.rpt"));
            rpt.Load();
            rpt.Refresh();

            rpt.SetDatabaseLogon(un, pw, svr, db_);
            foreach (Table table in rpt.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = svr;
                logonInfo.ConnectionInfo.DatabaseName = db_;
                logonInfo.ConnectionInfo.UserID = un;
                logonInfo.ConnectionInfo.Password = pw;
                table.ApplyLogOnInfo(logonInfo);
            }

            rpt.SetParameterValue("@cPoNo", poNo);
            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion
    }
}