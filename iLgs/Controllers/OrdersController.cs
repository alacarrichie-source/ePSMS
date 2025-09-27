using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Codes;
using iLgs.Services.Items;
using iLgs.Services.PurchaseOrder;
using iLgs.Services.PurchaseRequest;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("ORDERS")]
    public class OrdersController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IOrderService _orderService;
        private readonly IOrderItemService _orderItemService;
        private readonly IRequestService _requestService;
        private readonly ICodextnService _codextnService;
        private readonly IOrderItemUnitGroupService _unitGroupService;
        private readonly IOrderItemUnitGroupDescriptionService _unitGroupDescriptionService;
        private readonly IOrderItemUnitGroupDescriptionItemService _unitGroupDescriptionItemService;
        private readonly IItemCodeService _itemCodeService;
        private readonly IAllFieldService _allFieldService;
        private readonly IOrderUploadService _uploadPoService;
        private readonly IOrderUploadService _uploadCafoaService;

        public OrdersController(AppManEntities db, IOrderService orderService, IOrderItemService orderItemService, IRequestService requestService,
            ICodextnService codextnService, 
            IOrderItemUnitGroupService orderItemUnitGroupService, IOrderItemUnitGroupDescriptionService orderItemUnitGroupDescriptionService,
            IOrderItemUnitGroupDescriptionItemService orderItemUnitGroupDescriptionItemService, IItemCodeService itemCodeService,
            IAllFieldService allFieldService, IOrderUploadService orderUploadService)
        {
            _db = db;
            _orderService = orderService;
            _orderItemService = orderItemService;
            _requestService = requestService;
            _codextnService = codextnService;
            _unitGroupService = orderItemUnitGroupService;
            _unitGroupDescriptionService = orderItemUnitGroupDescriptionService;
            _unitGroupDescriptionItemService = orderItemUnitGroupDescriptionItemService;
            _itemCodeService = itemCodeService;
            _allFieldService = allFieldService;
            _uploadPoService = orderUploadService.Create("ORDERS");
            _uploadCafoaService = orderUploadService.Create("CAFOA");
        }

        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> OrderRead([DataSourceRequest] DataSourceRequest request)
        {
            var userId = User.Identity.GetUserId();
            var data = await _orderService.GetAllAsync(userId);

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
                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderService.CreateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult _OrderItem(Guid orderId)
        {
            ViewData["orderId"] = orderId;
            return PartialView();
        }

        public async Task<ActionResult> _OrderItemAddEdit(Guid orderId, Guid? orderItemId, string setLotNo)
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
            ViewData["setLotNo"] = setLotNo;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemSave(OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;

                var entity = await _orderItemService.GetByIdAsync(model.Id);
                if (entity == null)
                {
                    if (!access.AllowAdd)
                    {
                        ModelState.AddModelError("Access", "Access Denied!");
                    }
                }
                else
                {
                    if (!access.AllowEdit)
                    {
                        ModelState.AddModelError("Access", "Access Denied!");
                    }
                }

                
                if (await _orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;                    

                    if (entity == null)
                    {
                        model = await _orderItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _orderItemService.UpdateAsync(model, user, date);
                    }                    
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }
            
            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
                       .Select(ms => new
                       {
                           Key = ms.Key, // The field name
                           Message = ms.Value.Errors.Select(e =>
                           {
                               var errorMessage = e.ErrorMessage;
                               if (e.Exception != null)
                               {
                                   var exceptionMessage = e.Exception.Message;
                                   var innerExceptionMessage = e.Exception.InnerException?.Message;

                                   // Append exception details
                                   errorMessage += $" Exception: {exceptionMessage}";
                                   if (innerExceptionMessage != null)
                                   {
                                       errorMessage += $" InnerException: {innerExceptionMessage}";
                                   }
                               }

                               return errorMessage;
                           }).ToList() // List of messages for the current field
                       })
                       .ToList();

            if (errorList.Any())
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdteError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupDestroy([DataSourceRequest]DataSourceRequest request, OrderItemUnitGroupVM model)
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

                    model = await _unitGroupService.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdateError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdateError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupDescriptionItemDestroy([DataSourceRequest]DataSourceRequest request, OrderItemUnitGroupDescriptionItemVM model)
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

                    model = await _unitGroupDescriptionItemService.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion

        #region EXTRAS
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetDescription(OrderItemVM fields)
        {
            var stockNo = _allFieldService.GetOrderStockNo(fields);

            return Json(new { Description = "", StockNo = stockNo }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadFields([System.Web.Http.FromBody] OrderItemVM model)
        {
            if (model.Id != Guid.Empty)
            {
                var allField = await _allFieldService.GetByIdAsync(model.Id);
                if (allField != null)
                {
                    model.AllField = allField;
                }
            }            

            var itemCode = await _itemCodeService.GetByIdAsync(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            if (!string.IsNullOrEmpty(partialView))
            {
                partialView = $"{partialView}";
            }
            return PartialView(partialView, model);
        }


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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
                if (!access.AllowUnpost)
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
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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

        //public ActionResult _OrderItemExtnBatchRead([DataSourceRequest] DataSourceRequest request, string mode, Guid? requestItemId, Guid? orderItemId, string psType)
        //{
        //    /*
        //     * Need orderId: if mode == 'A' orderItemId is still null or invalid value 
        //     */
        //    var data = _orderItemExtnService.GetBatchInfo(mode, requestItemId, orderItemId, psType);
        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };

        //    return result;
        //}
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
                logonInfo.ConnectionInfo.IntegratedSecurity = true;
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

        #region PO UPLOADS
        public ActionResult _Images(Guid? imageId, string postedBy)
        {
            ViewData["imageId"] = imageId;
            ViewData["postedBy"] = postedBy;            
            return PartialView();
        }

                
        public ActionResult _ImagesAdd(Guid? imageId)
        {
            var model = new Models.Upload()
            {
                ImageId = imageId,
                Description = "PO"
            };
            ViewData["imageId"] = imageId;
            ViewData["fileSize"] = model.FileSize;
            return PartialView(model);
        }

        public ActionResult _ImagesRead([DataSourceRequest] DataSourceRequest request, Guid imageId)
        {
            var data = _uploadPoService.GetAllByImageId(imageId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> _ImagesDestroy([DataSourceRequest]DataSourceRequest request, Models.Upload model)
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

                    model = await _uploadPoService.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ImagesUpdate([DataSourceRequest] DataSourceRequest request, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadPoService.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdateError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }


            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> _ImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model, int? accountGroup)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadPoService.UploadAsync(files, model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("AddError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("AddError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("AddError", e.Message);
            }

            var errorList = ModelState.Values.SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();

            if (errorList.Any())
            {
                var errorMessage = string.Join("\n", errorList);
                return Content(errorMessage);
            }

            return Content("");
        }

        public ActionResult DownloadFile(string fileName)
        {
            try
            {
                // Call the service to get the file bytes
                byte[] fileBytes = _uploadPoService.DownloadFile(fileName);

                // Return the file as a download
                return File(fileBytes, MimeMapping.GetMimeMapping(fileName), fileName);
            }
            catch (FileNotFoundException ex)
            {
                // Handle file not found case
                return HttpNotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                return new HttpStatusCodeResult(500, "Error downloading file: " + ex.Message);
            }
        }

        public async Task<ActionResult> PreviewUpload(Guid id)
        {
            var fileResult = await _uploadPoService.GetUploadedFileAsync(id);
            if (fileResult != null)
            {
                return fileResult; // Return the file result directly
            }
            else
            {
                return HttpNotFound("File not found"); // Handle not found case
            }
        }
        #endregion

        #region CAFOA UPLOADS
        public ActionResult _CafoaImages(Guid? imageId, string postedBy)
        {
            ViewData["imageId"] = imageId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }


        public ActionResult _CafoaImagesAdd(Guid? imageId)
        {
            var model = new Models.Upload()
            {
                ImageId = imageId,
                Description = "CAFOA"
            };
            ViewData["imageId"] = imageId;
            ViewData["fileSize"] = model.FileSize;
            return PartialView(model);
        }

        public ActionResult _CafoaImagesRead([DataSourceRequest] DataSourceRequest request, Guid imageId)
        {
            var data = _uploadCafoaService.GetAllByImageId(imageId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> _CafoaImagesDestroy([DataSourceRequest]DataSourceRequest request, Models.Upload model)
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

                    model = await _uploadCafoaService.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _CafoaImagesUpdate([DataSourceRequest] DataSourceRequest request, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadCafoaService.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdateError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }


            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> _CafoaImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model, int? accountGroup)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadCafoaService.UploadAsync(files, model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("AddError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("AddError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("AddError", e.Message);
            }

            var errorList = ModelState.Values.SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();

            if (errorList.Any())
            {
                var errorMessage = string.Join("\n", errorList);
                return Content(errorMessage);
            }

            return Content("");
        }

        public ActionResult CafoaDownloadFile(string fileName)
        {
            try
            {
                // Call the service to get the file bytes
                byte[] fileBytes = _uploadCafoaService.DownloadFile(fileName);

                // Return the file as a download
                return File(fileBytes, MimeMapping.GetMimeMapping(fileName), fileName);
            }
            catch (FileNotFoundException ex)
            {
                // Handle file not found case
                return HttpNotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                return new HttpStatusCodeResult(500, "Error downloading file: " + ex.Message);
            }
        }

        public async Task<ActionResult> CafoaPreviewUpload(Guid id)
        {
            var fileResult = await _uploadCafoaService.GetUploadedFileAsync(id);
            if (fileResult != null)
            {
                return fileResult; // Return the file result directly
            }
            else
            {
                return HttpNotFound("File not found"); // Handle not found case
            }
        }
        #endregion
    }
}