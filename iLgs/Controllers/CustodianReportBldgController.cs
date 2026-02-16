using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.CustodianReports;
using iLgs.Services.CustodianUploads;
using iLgs.Services.Items;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("CUSTODIANREPORTBLDG")]
    public class CustodianReportBldgController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly ICustodianReportService _custodianReportService;
        private readonly ICustodianReportBldgItemService _custodianReportBldgItemService;
        private readonly ICustodianBldgUploadService _uploadService;
        private readonly ICustodianReportSubmitForCountService _custodianReportSubmitForCountService;
        private readonly ICodextnService _codextnService;
        private readonly IItemCodeService _itemCodeService;

        public CustodianReportBldgController(AppManEntities db, ICustodianReportService custodianReportService,
            ICustodianReportBldgItemService custodianReportBldgItemService,
            ICustodianBldgUploadService custodianBldgUploadService, 
            ICustodianReportSubmitForCountService custodianReportSubmitForCountService,
            ICodextnService codextnService,
            IItemCodeService itemCodeService)
        {
            _db = db;
            _custodianReportService = custodianReportService;
            _custodianReportBldgItemService = custodianReportBldgItemService;
            _custodianReportSubmitForCountService = custodianReportSubmitForCountService;
            _uploadService = custodianBldgUploadService;
            _codextnService = codextnService;
            _itemCodeService = itemCodeService;
        }

        public ActionResult BldgQuery()
        {
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.BUILDING;
            ViewBag.Title = "Custodian Report - Structure - Query";
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsView = false;
            return View();
        }

        public ActionResult Index()
        {
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.BUILDING;
            ViewBag.Title = "Custodian Report - Structure";
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;
            ViewBag.IsView = false;
            return View();
        }

        public ActionResult BldgUpdate()
        {
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.BUILDING;
            ViewBag.Title = "Custodian Report - Structure - Update Item Code";
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;
            ViewBag.IsView = false;

            //string userName = ControllerContext.HttpContext.User.Identity.Name;
            //var isAdmin = _userService.IsUserNameAdmin(userName);
            //ViewBag.IsAdmin = isAdmin;

            return View();
        }

        public ActionResult BldgDemand()
        {
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.BUILDING;
            ViewBag.Title = "Custodian Report - Structure";
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = true;
            ViewBag.IsView = false;
            return View("Index");
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, int? accountGroup)
        {
            var data = _custodianReportService.GetAllByDepartmentAccountGroup(forYear, deptId, accountGroup);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, CustodianReport model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, CustodianReport model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, CustodianReport model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportService.DeleteAsync(model, user, date);
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
        public async Task<ActionResult> Post(Guid id)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportBldgItemService.PostAsync(id, user, date);
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
        public async Task<ActionResult> UnPost(Guid id)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportBldgItemService.UnPostAsync(id, user, date);
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
        public async Task<ActionResult> SubmitForCount(Guid reportId, Guid? locationId, int? accountGroup, string url)
        {
            try
            {                
                //Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                //Access access = await accessTask;
                //if (!access.AllowPost)
                //{
                //    ModelState.AddModelError("GridError", "Access Denied!");
                //}
                //else
                //{
                //    string user = ControllerContext.HttpContext.User.Identity.Name;
                //    DateTime date = System.DateTime.Now;
                    
                //    await _custodianReportSubmitForCountService.SubmitAsync(reportId, locationId, url, user, date, false);
                //}

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                await _custodianReportSubmitForCountService.SubmitAsync(reportId, locationId, url, user, date, false);
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
        public async Task<ActionResult> UnsubmitForCount(Guid reportId, Guid? locationId, int? accountGroup, string url)
        {
            try
            {                
                //Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                //Access access = await accessTask;
                //if (!access.AllowUnpost)
                //{
                //    ModelState.AddModelError("GridError", "Access Denied!");
                //}
                //else
                //{
                //    string user = ControllerContext.HttpContext.User.Identity.Name;
                //    DateTime date = System.DateTime.Now;
                    
                //    await _custodianReportSubmitForCountService.UnsubmitAsync(reportId, locationId, url, user, date);
                //}

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                await _custodianReportSubmitForCountService.UnsubmitAsync(reportId, locationId, url, user, date);
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


        //public ActionResult _ItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, int? accountGroup, bool? isDemand)
        //{
        //    var data = _custodianReportBldgItemService.GetAllByDeptAcctGroupOld(forYear, deptId, accountGroup, isDemand);

        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}

        public ActionResult _ItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand, bool? isView)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportBldgItemService.GetAllByDeptAcctGroup(forYear, deptId, sectionId, accountGroup, user, isDemand, isView);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        //public ActionResult _UpdateItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, int? accountGroup, bool? isDemand, Guid? itemCodeId)
        //{
        //    string user = ControllerContext.HttpContext.User.Identity.Name;
        //    var data = _custodianReportBldgItemService.GetAllByDeptAcctGroupItemCodeId(forYear, deptId, accountGroup, user, isDemand, itemCodeId);

        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}

        public ActionResult _UpdateItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand, Guid? itemCodeId, bool? isView)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportBldgItemService.GetAllByDeptAcctGroupItemCodeId(forYear, deptId, sectionId, accountGroup, user, isDemand, itemCodeId, isView);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public async Task<ActionResult> UpdateItemCode(int? reportingYearEnd, string selectedIds, Guid? newItemId, int? accountGroup)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.IsAdmin)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }
                else
                {
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportBldgItemService.UpdateItemCodeAsync(reportingYearEnd, selectedIds, newItemId, user, date);
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

        public ActionResult _ItemReadAll([DataSourceRequest] DataSourceRequest request, int? forYear, int? accountGroup)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportBldgItemService.GetAllByAcctGroup(forYear, accountGroup, user);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemCreate([DataSourceRequest] DataSourceRequest request, CustodianReportBldgItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportBldgItemService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _ItemUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportBldgItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportBldgItemService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _ItemDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportBldgItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportBldgItemService.DeleteAsync(model, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemSave(CustodianReportBldgItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;

                var entity = await _custodianReportBldgItemService.GetByIdAsync(model.Id);
                if (entity == null)
                {
                    if (!access.AllowAdd)
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }
                else
                {
                    if (!access.AllowEdit)
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (entity == null)
                    {
                        model = await _custodianReportBldgItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _custodianReportBldgItemService.UpdateAsync(model, user, date);
                    }

                    return Json(new { Errors = "", Model = model });
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

            return Json(new { Errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray() });
        }

        #region Phase Items
        public ActionResult _ItemPhase(Guid? reportItemId, bool? isView)
        {
            ViewData["reportItemId"] = reportItemId;
            ViewBag.IsView = isView;

            return PartialView();
        }

        public ActionResult _ItemPhaseRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _custodianReportBldgItemService.CustodianReportBldgItemPhase.GetByBldgItemId(reportItemId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ItemPhaseCreate([DataSourceRequest] DataSourceRequest request, CustodianReportBldgItemPhasVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportBldgItemService.CustodianReportBldgItemPhase.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _ItemPhaseUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportBldgItemPhasVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportBldgItemService.CustodianReportBldgItemPhase.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _ItemPhaseDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportBldgItemPhasVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportBldgItemService.CustodianReportBldgItemPhase.DeleteAsync(model, user, date);
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
        #endregion

        #region UPLOADS
        public ActionResult _Images(Guid? imageId, bool? isView)
        {
            ViewData["imageId"] = imageId;
            ViewBag.IsView = isView;

            return PartialView();
        }

        public ActionResult _ImagesAdd(Guid? imageId)
        {
            var model = new Models.Upload()
            {
                ImageId = imageId
            };
            ViewData["imageId"] = imageId;
            ViewData["fileSize"] = model.FileSize;
            return PartialView(model);
        }

        public ActionResult _ImagesRead([DataSourceRequest] DataSourceRequest request, Guid imageId)
        {
            var data = _uploadService.GetAllByImageId(imageId);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    ValidateReportingYearEnd(model.ImageId);

                    model = await _uploadService.DeleteAsync(model, user, date);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    ValidateReportingYearEnd(model.ImageId);

                    model = await _uploadService.UpdateAsync(model, user, date);
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


        [HttpPost]
        public async Task<ActionResult> _ImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    ValidateReportingYearEnd(model.ImageId);

                    model = await _uploadService.UploadAsync(files, model, user, date);
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

        public void ValidateReportingYearEnd(Guid? itemId)
        {
            var asOf = _db.CustodianReportBldgItems.Where(w => w.Id == itemId).Select(s => s.CustodianReport.AsOf).FirstOrDefault();
            _codextnService.ValidateReportingYearEnd(asOf.Value.Year);
        }

        public ActionResult DownloadFile(string fileName)
        {
            try
            {
                // Call the service to get the file bytes
                byte[] fileBytes = _uploadService.DownloadFile(fileName);

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
            var fileResult = await _uploadService.GetUploadedFileAsync(id);
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

        #region DOWNLOAD RECORDS
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Download(int? forYear, Guid? deptId, int? accountGroup)
        {
            try
            {                
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowDownload)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportService.DownloadBldg(forYear, deptId, accountGroup, user, date);
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
        #endregion  

        #region UPLOAD RECORDS
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Upload(int? forYear, Guid? deptId, int? accountGroup)
        {
            try
            {                
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report_bldg");
                Access access = await accessTask;
                if (!access.AllowDownload)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportService.UploadBldg(forYear, deptId, accountGroup, user, date);
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
        #endregion  


        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<JsonResult> GetStockNo(CustodianReportBldgItem fields)
        {
            var stockNo = await _custodianReportBldgItemService.GetStockNoAsync(fields);

            return Json(new { StockNo = stockNo }, JsonRequestBehavior.AllowGet);
        }

        //public async Task<ActionResult> ExcelExportAll(int? forYear, int? accountGroup)
        //{
        //    return await ExcelExport(forYear, null, accountGroup);
        //}

        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }

        public async Task<ActionResult> ExcelExportReport(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup)
        {
            return await ExcelExport(forYear, deptId, sectionId, accountGroup, "", null, null, "", "", "", "");
        }

        public async Task<ActionResult> ExcelExportAll(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string mainAccount, DateTime? asOf, DateTime? insertedAsOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            return await ExcelExport(forYear, deptId, sectionId, accountGroup, mainAccount, asOf, insertedAsOf, subAccount1, subAccount2, subAccount3, subAccount4);
        }

        //public async Task<ActionResult> ExcelExport(int? forYear, Guid? deptId, int? accountGroup)
        public async Task<ActionResult> ExcelExport(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string mainAccount, DateTime? asOf, DateTime? insertedAsOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            try
            {
                string exportFileName = "CustodianStructure";
                string user = ControllerContext.HttpContext.User.Identity.Name;
                var templateFilePath = Server.MapPath($"~/App_Data/{exportFileName}Template.xlsx");
                //var stream = _custodianReportBldgItemService.ProcessExcelFile(forYear, deptId, templateFilePath, accountGroup);
                var stream = _custodianReportBldgItemService.ProcessExcelFile(forYear, deptId, sectionId, templateFilePath, accountGroup, mainAccount
                    , asOf, insertedAsOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, user);
                string locationCode = "ALL";
                if (deptId != null && deptId != Guid.Empty)
                {
                    locationCode = (await _codextnService.GetByIdAsync(deptId))?.Code;
                }

                var sa1 = string.Empty;
                var sa2 = string.Empty;
                var sa3 = string.Empty;
                var sa4 = string.Empty;

                if (!string.IsNullOrWhiteSpace(subAccount1))
                {
                    sa1 = (await _itemCodeService.GetByCodeAsync(subAccount1)).Description;
                }

                if (!string.IsNullOrWhiteSpace(subAccount2))
                {
                    sa2 = (await _itemCodeService.GetByCodeAsync(subAccount2)).Description;
                }

                if (!string.IsNullOrWhiteSpace(subAccount3))
                {
                    sa3 = (await _itemCodeService.GetByCodeAsync(subAccount3)).Description;
                }

                if (!string.IsNullOrWhiteSpace(subAccount4))
                {
                    sa4 = (await _itemCodeService.GetByCodeAsync(subAccount4)).Description;
                }

                string fileName = $"{locationCode}_{exportFileName}_{mainAccount}_{sa1}_{sa2}_{sa3}_{sa4}_{DateTime.Now.ToShortDateString()}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        public async Task<ActionResult> ExcelExportAnnexAll(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string annex, string mainAccount
            , DateTime? asOf, DateTime? insertedAsOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            return await ExcelExportAnnex(forYear, deptId, sectionId, accountGroup, annex, mainAccount, asOf, insertedAsOf, subAccount1, subAccount2, subAccount3, subAccount4);
        }

        public async Task<ActionResult> ExcelExportAnnexReport(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string annex)
        {
            return await ExcelExportAnnex(forYear, deptId, sectionId, accountGroup, annex, "", null, null, "", "", "", "");
        }

        public async Task<ActionResult> ExcelExportAnnex(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string annex, string mainAccount
            , DateTime? asOf, DateTime? insertedAsOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            try
            {
                string exportFileName = $"CustodianStructureAnnex";                
                string user = ControllerContext.HttpContext.User.Identity.Name;
                var templateFilePath = Server.MapPath($"~/App_Data/{exportFileName}Template.xlsx");
                var stream = _custodianReportBldgItemService.ProcessExcelFileAnnex(forYear, deptId, sectionId, templateFilePath, accountGroup, annex, mainAccount
                    , asOf, insertedAsOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, user);
                var locationCode = "ALL";
                if (deptId != null && deptId != Guid.Empty)
                {
                    locationCode = (await _codextnService.GetByIdAsync(deptId))?.Code;
                }

                var sa1 = string.Empty;
                var sa2 = string.Empty;
                var sa3 = string.Empty;
                var sa4 = string.Empty;

                if (!string.IsNullOrWhiteSpace(subAccount1))
                {
                    sa1 = (await _itemCodeService.GetByCodeAsync(subAccount1)).Description;
                }

                if (!string.IsNullOrWhiteSpace(subAccount2))
                {
                    sa2 = (await _itemCodeService.GetByCodeAsync(subAccount2)).Description;
                }

                if (!string.IsNullOrWhiteSpace(subAccount3))
                {
                    sa3 = (await _itemCodeService.GetByCodeAsync(subAccount3)).Description;
                }

                if (!string.IsNullOrWhiteSpace(subAccount4))
                {
                    sa4 = (await _itemCodeService.GetByCodeAsync(subAccount4)).Description;
                }

                string fileName = $"{locationCode}_{exportFileName}-{annex}_{mainAccount}_{sa1}_{sa2}_{sa3}_{sa4}_{DateTime.Now.ToShortDateString()}.xlsx";

                //return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{locationCode}_{exportFileName}-{annex}_{DateTime.Now.ToShortDateString()}.xlsx");
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        public ActionResult _TransferPhaseItems(Guid id, Guid reportId, string custodianItemNo)
        {            
            CustodianReportBldgItemTransferVM model = new CustodianReportBldgItemTransferVM()
            {
                ReportId = reportId,
                SourceId = id,
                SourceCustodianItemNo = custodianItemNo
            };            
            
            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _TransferPhaseItemSave(CustodianReportBldgItemTransferVM model)
        {
            try
            {
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportBldgItemService.CustodianReportBldgItemPhase.TransferItemAsync(model, user, date);
                   
                    return Json(new { Errors = "", Model = model });
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

            return Json(new { Errors = ModelState.Where(ms => ms.Value.Errors.Any()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()) });
        }
    }
}