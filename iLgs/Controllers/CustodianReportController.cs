using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using iLgs.Services.CustodianReports;
using iLgs.Services.CustodianUploads;
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
using static iLgs.Models.Enums;

namespace iLgs.Controllers
{
    [AppAuthorize("CUSTODIANREPORT")]
    public class CustodianReportController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly ICustodianReportService _custodianReportService;
        private readonly ICustodianReportItemService _custodianReportItemService;
        private readonly ICustodianReportItemStockService _custodianReportItemStockService;
        private readonly ICustodianReportItemPpeService _custodianReportItemPpeService;
        private readonly ICustodianReportItemVehicleService _custodianReportItemVehicleService;
        private readonly ICodextnService _codextnService;
        private readonly ICustodianReportUploadService _uploadService;
        private readonly ICustodianReportItemIssuanceParService _parIssuanceService;
        private readonly ICustodianReportItemIssuanceIcsService _icsIssuanceService;
        private readonly ICustodianReportItemIssuanceAreService _areIssuanceService;
        private readonly ICustodianReportItemIssuanceMrService _mrIssuanceService;

        private readonly string _stockId, _ppeId, _transpoId;

        public CustodianReportController()
        {
            _db = new AppManEntities();
            _custodianReportService = new CustodianReportService(_db);
            _custodianReportItemService = new CustodianReportItemService(_db);
            _custodianReportItemStockService = new CustodianReportItemStockService(_db);
            _custodianReportItemPpeService = new CustodianReportItemPpeService(_db);
            _custodianReportItemVehicleService = new CustodianReportItemVehicleService(_db);
            _codextnService = new CodextnService(_db);
            _uploadService = new CustodianReportUploadService(_db);
            _parIssuanceService = new CustodianReportItemIssuanceParService(_db);
            _icsIssuanceService = new CustodianReportItemIssuanceIcsService(_db);
            _areIssuanceService = new CustodianReportItemIssuanceAreService(_db);
            _mrIssuanceService = new CustodianReportItemIssuanceMrService(_db);

            _stockId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.STOCK);
            _ppeId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.PPE);
            _transpoId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.VEHICLE);
        }

        public ActionResult Stock()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.STOCK;
            ViewBag.Title = "Custodian Report - Supplies";
            return View();
        }

        public ActionResult Ppe()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.PPE;
            ViewBag.Title = "Custodian Report - Equipment";
            return View();
        }

        public ActionResult Transpo()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.VEHICLE;
            ViewBag.Title = "Custodian Report - Vehicles";
            return View();
        }        

        #region CUSTODIAN REPORT
        // GET: Index
        public ActionResult Index()
        {
            if (TempData["AllowIndexAccess"] == null || !(bool)TempData["AllowIndexAccess"])
            {
                ViewBag.Error = "Access Denied!";
                return View("Error"); // Or some other handling
            }
            return View();
        }

        //public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid? deptId, int? accountGroup)
        //{
        //    var data = _custodianReportService.GetAllByDepartmentAccountGroup(deptId, accountGroup);

        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, CustodianReport model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report");
        //        Access access = await accessTask;
        //        if (!access.AllowAdd)
        //        {
        //            ModelState.AddModelError("Access", "Add Access Denied!");
        //        }


        //        if (model != null && ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _custodianReportService.CreateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError(error.Key, error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, CustodianReport model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("Access", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _custodianReportService.UpdateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError(error.Key, error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, CustodianReport model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "custodian_report");
        //        Access access = await accessTask;
        //        if (!access.AllowDelete)
        //        {
        //            ModelState.AddModelError("DeleteError", "Delete Access Denied!");
        //        }
        //        else
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _custodianReportService.DeleteAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("DeleteError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Post(Guid id, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportItemService.PostAsync(id, user, date);
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
        public async Task<ActionResult> UnPost(Guid id, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportItemService.UnPostAsync(id, user, date);
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

        public ActionResult _ReportItem(Guid reportId, int? accountGroup)
        {
            string partialView = "";

            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                partialView = "_StockItem";
            }
            else if (accountGroup == (int?)CustodianAccountGroup.PPE)
            {
                partialView = "_PpeItem";
            }
            else if (accountGroup == (int?)CustodianAccountGroup.VEHICLE)
            {
                partialView = "_VehicleItem";
            }

            ViewData["reportId"] = reportId;
            ViewBag.AccountGroup = accountGroup;
            return PartialView(partialView);
        }

        #region STOCK ITEM
        public ActionResult _StockItemRead([DataSourceRequest] DataSourceRequest request, Guid? deptId, int? accountGroup)
        {
            var data = _custodianReportItemStockService.GetAllByDeptAcctGroup(deptId, accountGroup);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockItemCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemStockVM model)
        {
            try
            {                
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemStockService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _StockItemUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemStockVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemStockService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _StockItemDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemStockVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemStockService.DeleteAsync(model, user, date);
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

        #region PPE ITEMS
        public ActionResult _PpeItemRead([DataSourceRequest] DataSourceRequest request, Guid? deptId, int? accountGroup)
        {
            var data = _custodianReportItemPpeService.GetAllByDeptAcctGroup(deptId, accountGroup);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeItemCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemPpeService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _PpeItemUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemPpeService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _PpeItemDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemPpeService.DeleteAsync(model, user, date);
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

        #region VEHICLE ITEMS
        public ActionResult _VehicleItemRead([DataSourceRequest] DataSourceRequest request, Guid? deptId, int? accountGroup)
        {
            var data = _custodianReportItemVehicleService.GetAllByDeptAcctGroup(deptId, accountGroup);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleItemCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemVehicleVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemVehicleService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _VehicleItemUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemVehicleVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemVehicleService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _VehicleItemDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemVehicleVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _custodianReportItemVehicleService.DeleteAsync(model, user, date);
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

        #region STOCK PAR ISSUANCE
        public ActionResult _StockParIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }
        
        public ActionResult _StockParIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _parIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockParIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockParIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _StockParIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.DeleteAsync(model, user, date);
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

        #region STOCK ICS ISSUANCE
        public ActionResult _StockIcsIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _StockIcsIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _icsIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockIcsIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockIcsIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _StockIcsIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.DeleteAsync(model, user, date);
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

        #region STOCK ARE ISSUANCE
        public ActionResult _StockAreIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _StockAreIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _areIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockAreIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockAreIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _StockAreIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.DeleteAsync(model, user, date);
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

        #region STOCK MR ISSUANCE
        public ActionResult _StockMrIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _StockMrIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _mrIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockMrIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockMrIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _StockMrIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.DeleteAsync(model, user, date);
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

        #region PPE PAR ISSUANCE
        public ActionResult _PpeParIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _PpeParIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _parIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeParIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeParIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _PpeParIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.DeleteAsync(model, user, date);
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

        #region PPE ICS ISSUANCE
        public ActionResult _PpeIcsIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _PpeIcsIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _icsIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeIcsIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeIcsIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _PpeIcsIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.DeleteAsync(model, user, date);
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

        #region PPE ARE ISSUANCE
        public ActionResult _PpeAreIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _PpeAreIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _areIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeAreIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeAreIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _PpeAreIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.DeleteAsync(model, user, date);
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

        #region PPE MR ISSUANCE
        public ActionResult _PpeMrIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _PpeMrIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _mrIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeMrIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeMrIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _PpeMrIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.DeleteAsync(model, user, date);
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

        #region VEHICLE PAR ISSUANCE
        public ActionResult _VehicleParIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _VehicleParIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _parIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleParIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleParIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _VehicleParIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _parIssuanceService.DeleteAsync(model, user, date);
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

        #region VEHICLE ICS ISSUANCE
        public ActionResult _VehicleIcsIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _VehicleIcsIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _icsIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleIcsIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleIcsIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _VehicleIcsIssuanceeDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceIcsVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsIssuanceService.DeleteAsync(model, user, date);
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

        #region VEHICLE ARE ISSUANCE
        public ActionResult _VehicleAreIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _VehicleAreIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _areIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleAreIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleAreIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _VehicleAreIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceAreVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _areIssuanceService.DeleteAsync(model, user, date);
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

        #region VEHICLE MR ISSUANCE
        public ActionResult _VehicleMrIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _VehicleMrIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _mrIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleMrIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("AddError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.CreateAsync(model, user, date);
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

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleMrIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _VehicleMrIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceMrVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _mrIssuanceService.DeleteAsync(model, user, date);
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

        #region PRINTOUTS
        public async Task<ActionResult> CustodianStockRpt(Guid? id, int? accountGroup)
        {
            Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
            Access access = await accessTask;
            if (!access.AllowDelete)
            {
                return new HttpStatusCodeResult(401, "Access Denied");
            }

            //var rpci = _db.RPCIs.Find(id);
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/CustodianReportStock.rpt"));
            }
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
            rpt.SetParameterValue("@dAsOf", null);
            rpt.SetParameterValue("@ureportId", id.ToString());

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");

        }
        #endregion

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadStockFields(CustodianReportItemStockVM model)
        {
            var itemTypeCode = model.ItemType_Code;
            var itemCode = model.Item_Code;
            if (model.Id != Guid.Empty)
            {
                model = await _custodianReportItemStockService.GetByIdAsync(model.Id);
            }

            string partialView = AllFieldsUtil.GetPartialField(itemTypeCode, itemCode);
            partialView = $"_Stock{partialView}";
            
            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadPpeFields(CustodianReportItemPpeVM model)
        {
            var itemTypeCode = model.ItemType_Code;
            var itemCode = model.Item_Code;
            if (model.Id != Guid.Empty)
            {
                model = await _custodianReportItemPpeService.GetByIdAsync(model.Id);
            }

            string partialView = AllFieldsUtil.GetPartialField(itemTypeCode, itemCode);
            partialView = $"_Ppe{partialView}";            

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadVehicleFields(CustodianReportItemVehicleVM model)
        {
            var itemTypeCode = model.ItemType_Code;
            var itemCode = model.Item_Code;
            if (model.Id != Guid.Empty)
            {
                model = await _custodianReportItemVehicleService.GetByIdAsync(model.Id);
            }

            string partialView = AllFieldsUtil.GetPartialField(itemTypeCode, itemCode);
            partialView = $"_Vehicle{partialView}";

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult GetStockNo(CustodianReportItem fields)
        {
            var stockNo = _custodianReportItemStockService.GetStockNo(fields);

            return Json(new { StockNo = stockNo }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }

        public async Task<ActionResult> ExcelExport(Guid reportId, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    return new HttpStatusCodeResult(401, "Access Denied");
                }

                string exportFileName = "";
                var report = await _custodianReportService.GetByIdAsync(reportId);
                if (report.AccountGroup == (int?)CustodianAccountGroup.STOCK)
                {
                    exportFileName = "CustodianSupplies";                    
                }
                else if (report.AccountGroup == (int?)CustodianAccountGroup.PPE)
                {
                    exportFileName = "CustodianEquipment";
                }
                else if (report.AccountGroup == (int?)CustodianAccountGroup.VEHICLE)
                {
                    exportFileName = "CustodianVehicles";
                }

                var templateFilePath = Server.MapPath($"~/App_Data/{exportFileName}Template.xlsx");
                var stream = _custodianReportItemService.ProcessExcelFile(reportId, templateFilePath, report.AccountGroup);

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{exportFileName}.xlsx");                
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        public async Task<ActionResult> ExcelExportAnnex(Guid reportId, string annex)
        {
            try
            {
                string exportFileName = "";
                var report = await _custodianReportService.GetByIdAsync(reportId);
                if (report.AccountGroup == (int?)CustodianAccountGroup.STOCK)
                {
                    exportFileName = $"CustodianSuppliesAnnex";
                }
                else if (report.AccountGroup == (int?)CustodianAccountGroup.PPE)
                {
                    exportFileName = $"CustodianEquipmentAnnex";
                }
                else if (report.AccountGroup == (int?)CustodianAccountGroup.VEHICLE)
                {
                    exportFileName = $"CustodianVehiclesAnnex";
                }

                var templateFilePath = Server.MapPath($"~/App_Data/{exportFileName}Template.xlsx");
                var stream = _custodianReportItemService.ProcessExcelFileAnnex(reportId, templateFilePath, report.AccountGroup, annex);

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{exportFileName}-{annex}.xlsx");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        #region UPLOADS
        public ActionResult _Images(Guid? imageId)
        {
            ViewData["imageId"] = imageId;
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
        
        public async Task<ActionResult> _ImagesDestroy([DataSourceRequest]DataSourceRequest request, Models.Upload model, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

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
        public async Task<ActionResult> _ImagesUpdate([DataSourceRequest] DataSourceRequest request, Models.Upload model, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

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

        public async Task<ActionResult> _ImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

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
    }
}