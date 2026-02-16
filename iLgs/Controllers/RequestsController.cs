using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PurchaseOrder;
using iLgs.Services.PurchaseRequest;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("REQUESTS")]
    public class RequestsController : BaseController
    {
        private readonly AppManEntities _db ;
        private readonly IOrderService _orderService;
        private readonly IRequestService _requestService;
        private readonly IRequestItemService _requestItemService;
        private readonly ICodextnService _codextnService;
        private readonly IRequestItemUnitGroupService _unitGroupService;
        private readonly IRequestItemUnitGroupDescriptionService _unitGroupDescriptionService;
        private readonly IRequestItemUnitGroupDescriptionItemService _unitGroupDescriptionItemService;

        public RequestsController(AppManEntities db,
            IOrderService orderService, 
            IRequestService requestService, 
            IRequestItemService requestItemService,
            ICodextnService codextnService, 
            IRequestItemUnitGroupService requestItemUnitGroupService, 
            IRequestItemUnitGroupDescriptionService requestItemUnitGroupDescriptionService,
            IRequestItemUnitGroupDescriptionItemService requestItemUnitGroupDescriptionItemService)
        {
            _db = db;
            _orderService = orderService;
            _requestService = requestService;
            _requestItemService = requestItemService;
            _codextnService = codextnService;
            _unitGroupService = requestItemUnitGroupService;
            _unitGroupDescriptionService = requestItemUnitGroupDescriptionService;
            _unitGroupDescriptionItemService = requestItemUnitGroupDescriptionItemService;
        }

        // GET: Requests
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> RequestRead([DataSourceRequest] DataSourceRequest request)
        {
            var userId = User.Identity.GetUserId();
            var data = await _requestService.GetAllAsync(userId);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RequestCreate([DataSourceRequest] DataSourceRequest request, RequestVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (await _requestService.GetByPrNoAsync(model.PrNo) != null)
                {
                    ModelState.AddModelError("PR No.", "PR number already exists!");
                }

                //else if (await _requestService.IsAnyRisNoAsync(model.Id, model.RisNo))
                //{
                //    ModelState.AddModelError("RIS No.", "RIS number already used by other PR!");
                //}

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> RequestUpdate([DataSourceRequest] DataSourceRequest request, RequestVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }
                else if (await _requestService.IsPostedAsync(model.Id))
                {
                    ModelState.AddModelError("PR No.", "PR Number already Posted, cannot update!");
                }
                else if (await _requestService.IsAnyPrNoAsync(model.Id, model.PrNo))
                {
                    ModelState.AddModelError("PR No.", "PR number already exists!");
                }
                else if (await _requestService.IsWithPOAsync(model.Id))
                {
                    var entity = await _requestService.GetByIdAsync(model.Id);
                    if (entity.RisId != model.Id)
                    {
                        ModelState.AddModelError("PO No.", "PO number already exists for this PR, cannot change RIS No.");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> RequestDestroy([DataSourceRequest]DataSourceRequest request, RequestVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }                
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestService.DeleteAsync(model, user, date);                    
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

        public ActionResult _RequestItem(Guid requestId)
        {
            ViewData["requestId"] = requestId;
            return PartialView();
        }

        public async Task<ActionResult> _RequestItemAddEdit(Guid prId, Guid? requestItemId, string setLotNo)
        {
            var data = await _requestItemService.GetVmByIdAsync(requestItemId);
            if (data == null)
            {
                data = new RequestItemVM()
                {
                    Id = Guid.NewGuid(),
                    PrId = prId
                };
            }
            ViewData["setLotNo"] = setLotNo;
            ViewData["requestItemId"] = requestItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RequestItemSave(RequestItemVM model)
        {
            try
            {                
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;

                var entity = await _requestItemService.GetByIdAsync(model.Id);
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
                
                if (await _requestService.IsPostedAsync(model.PrId))
                {
                    ModelState.AddModelError("PR No.", "PR Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                   
                    if (entity == null)
                    {
                        model = await _requestItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _requestItemService.UpdateAsync(model, user, date);
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

        public ActionResult _RequestItemRead([DataSourceRequest] DataSourceRequest request, Guid? prId)
        {
            var data = _requestItemService.GetByPrId(prId);
            
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

                
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RequestItemDestroy([DataSourceRequest]DataSourceRequest request, RequestItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await _requestService.IsPostedAsync((Guid)model.PrId))
                {
                    ModelState.AddModelError("DeleteError", "PR Number already Posted, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestItemService.DeleteAsync(model, user, date);                    
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
        public async Task<ActionResult> PostRequest(Guid requestId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await _requestService.GetByIdAsync(requestId) == null)
                {
                    ModelState.AddModelError("Request", "Invalid Request Id");
                }
                else if (await _requestService.IsPostedAsync(requestId))
                {
                    ModelState.AddModelError("PR No.", "PR Number already Posted, cannot post again!");
                }
                //else if (await _requestService.IsWithInvalidUnitCostAsync(requestId))
                //{
                //    ModelState.AddModelError("Unit Cost", "All PR Items must have unit cost!");
                //}

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.PostAsync(requestId, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnpostRequest(Guid requestId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await _requestService.GetByIdAsync(requestId) == null)
                {
                    ModelState.AddModelError("Request", "Invalid Request Id");
                }
                else if (!(await _requestService.IsPostedAsync(requestId)))
                {
                    ModelState.AddModelError("PR No.", "PR Number not yet posted, cannot unpost!");
                }
                else if (await _requestService.IsPoPostedAsync(requestId))
                {
                    ModelState.AddModelError("PO No.", "PO Number for this request is already posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.UnpostAsync(requestId, user, date);
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
        
        #region UNIT GROUP
        public ActionResult _UnitGroup(Guid requestId)
        {
            ViewData["requestId"] = requestId;
            return PartialView();
        }

        public ActionResult _UnitGroupRead([DataSourceRequest] DataSourceRequest request, Guid? requestId)
        {
            var data = _unitGroupService.GetByPrId(requestId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupUpdate([DataSourceRequest] DataSourceRequest request, RequestItemUnitGroupVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
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
        public async Task<ActionResult> _UnitGroupDestroy([DataSourceRequest]DataSourceRequest request, RequestItemUnitGroupVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
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
        public async Task<ActionResult> _UnitGroupDescriptionUpdate([DataSourceRequest] DataSourceRequest request, RequestItemUnitGroupDescriptionVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
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
        public async Task<ActionResult> _UnitGroupDescriptionItemUpdate([DataSourceRequest] DataSourceRequest request, RequestItemUnitGroupDescriptionItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
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
        #endregion

        #region PRINTOUTS
        public ActionResult PurchaseRequestRpt(string prNo)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Pr_.rpt"));
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
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
                table.ApplyLogOnInfo(logonInfo);
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("@cPrNo", prNo);
            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion

        public JsonResult GetModelDefault()
        {
            var approved = _db.Codextns.Where(w => w.CodeMast.Code == "APPROVED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();
            var availability = _db.Codextns.Where(w => w.CodeMast.Code == "CASH-AVAILABLE").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();

            var model = new RequestVM()
            {
                PrDate = DateTime.Now,
                ApprovedBy = approved?.Description,
                ApprovedDesig = approved?.Desc2,
                Availability = availability?.Description,
                AvaialbilityDesig = availability?.Desc2                
            };

            return Json(new { model }, JsonRequestBehavior.AllowGet);
        }
    }
}