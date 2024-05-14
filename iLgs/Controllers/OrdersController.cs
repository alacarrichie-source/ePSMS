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
using iLgs.Exceptions;

namespace iLgs.Controllers
{
    [AppAuthorize("ORDERS")]
    public class OrdersController : BaseController
    {
        private AppManEntities _db = new AppManEntities();
        private IOrderService _orderService;
        private IOrderItemService _orderItemService;
        private IOrderItemExtnService _orderItemExtnService;
        private IRequestService _requestService;
        private ICodextnService _codextnService;
        private IOrderItemUnitGroupService _unitGroupService;
        private IOrderItemUnitGroupDescriptionService _unitGroupDescriptionService;
        private IOrderItemUnitGroupDescriptionItemService _unitGroupDescriptionItemService;

        public OrdersController()
        {
            _orderService = new OrderService(_db);
            _orderItemService = new OrderItemService(_db);
            _orderItemExtnService = new OrderItemExtnService(_db);
            _requestService = new RequestService(_db);
            _codextnService = new CodextnService(_db);
            _unitGroupService = new OrderItemUnitGroupService(_db);
            _unitGroupDescriptionService = new OrderItemUnitGroupDescriptionService(_db);
            _unitGroupDescriptionItemService = new OrderItemUnitGroupDescriptionItemService(_db);
        }

        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult OrderRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _orderService.GetAll();

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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }

                //if (await orderService.GetByPoNoAsync(model.PoNo) != null)
                //{
                //    ModelState.AddModelError("PoNo", "P.O. number already exists!");
                //}
                //else
                //{
                //    var pr = await requestService.GetByIdAsync(model.PrId);
                //    if (pr == null)
                //    {
                //        ModelState.AddModelError("PrNo", "Invalid P.R. Number!");
                //    }
                //    else
                //    {
                //        if (pr.PrDate > model.PoDate)
                //        {
                //            ModelState.AddModelError("PoDate", "P.O. date must be greather than or equal to P.R. date!");
                //        }
                //    }
                //}
                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderService.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> OrderUpdate([DataSourceRequest] DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }                

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> OrderDestroy([DataSourceRequest]DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }                
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
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
            var data = await _orderItemService.GetByIdAsync(orderItemId);
            if (data == null)
            {
                data = new OrderItemVM()
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Mode = "A"
                };
            }            
            else
            {
                data.Mode = "E";
            }
            ViewData["orderItemId"] = orderItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemSave(OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await _orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await _orderItemService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await _orderItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _orderItemService.UpdateAsync(model, user, date);
                    }
                    //var orderItemExtns = (List<OrderItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridOrderItemExtns, typeof(List<OrderItemExtnVM>));
                    //await orderItemExtnService.SaveAsync(model.Id, orderItemExtns, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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
            var data = _orderItemService.GetByPoId(orderId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemCreate([DataSourceRequest] DataSourceRequest request, OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await _orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderItemService.CreateAsync(model, user, date);                    
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await _orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderItemService.UpdateAsync(model, user, date);

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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }                

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderItemService.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        #region UNIT GROUP
        public ActionResult _UnitGroup(Guid orderId)
        {
            ViewData["orderId"] = orderId;
            return PartialView();
        }

        public ActionResult _UnitGroupRead([DataSourceRequest] DataSourceRequest request, Guid? orderId)
        {
            var data = _unitGroupService.GetByOrderId(orderId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupUpdate([DataSourceRequest] DataSourceRequest request, OrderItemUnitGroupVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "order");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _unitGroupService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupDestroy([DataSourceRequest]DataSourceRequest request, OrderItemUnitGroupVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "order");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _unitGroupService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        #endregion

        #region UNIT GROUP DESCRIPTION
        public ActionResult _UnitGroupDescription(Guid unitGroupId)
        {
            ViewData["unitGroupId"] = unitGroupId;
            return PartialView();
        }

        public ActionResult _UnitGroupDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        {
            var data = _unitGroupDescriptionService.GetByUnitGroupId(unitGroupId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupDescriptionUpdate([DataSourceRequest] DataSourceRequest request, OrderItemUnitGroupDescriptionVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "order");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _unitGroupDescriptionService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        #endregion

        #region UNIT GROUP DESCRIPTION ITEMS        
        public ActionResult _UnitGroupDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        {
            var data = _unitGroupDescriptionItemService.GetByUnitGroupDescriptionId(unitGroupDescriptionId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupDescriptionItemUpdate([DataSourceRequest] DataSourceRequest request, OrderItemUnitGroupDescriptionItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "order");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _unitGroupDescriptionItemService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupDescriptionItemDestroy([DataSourceRequest]DataSourceRequest request, OrderItemUnitGroupDescriptionItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "order");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _unitGroupDescriptionItemService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion

        #region EXTRAS

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostOrders(Guid orderId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _orderService.PostAsync(orderId, user, date);
                }
            }
            catch (RecordNotFoundException e)
            {
                ModelState.AddModelError("", e.Message);
            }
            catch (PoNumberAlreadyPostedException e)
            {
                ModelState.AddModelError("", e.Message);
            }
            catch (RequiredFieldException e)
            {
                ModelState.AddModelError("", e.Message);
            }
            catch (PurchaseRequestNotYetPostedException e)
            {
                ModelState.AddModelError("", e.Message);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _orderService.UnpostAsync(orderId, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
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

        public ActionResult _OrderItemExtnBatchRead([DataSourceRequest] DataSourceRequest request, string mode, Guid? requestItemId, Guid? orderItemId, string psType)
        {
            /*
             * Need orderId: if mode == 'A' orderItemId is still null or invalid value 
             */
            var data = _orderItemExtnService.GetBatchInfo(mode, requestItemId, orderItemId, psType);
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
            string stringname = _db.Database.Connection.ConnectionString.ToString();
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

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("@cPoNo", poNo);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion
    }
}