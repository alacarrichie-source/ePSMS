using CrystalDecisions.CrystalReports.Engine;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
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
using System.Configuration;
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
        private readonly ICustodianReportItemIssuanceParService _parIssuanceService;
        private readonly ICustodianReportItemIssuanceIcsService _icsIssuanceService;
        private readonly ICustodianReportItemIssuanceAreService _areIssuanceService;
        private readonly ICustodianReportItemIssuanceMrService _mrIssuanceService;
        private readonly ICustodianReportItemIssuanceRpcPpeService _rpcPpeIssuanceService;
        private readonly ICustodianReportSubmitForCountService _custodianReportSubmitForCountService;
        private readonly ICodextnService _codextnService;
        private readonly ICustodianReportUploadService _uploadService;
        private readonly ICustodianDeptUploadService _scanUploadService;        
        private readonly IItemCodeService _itemCodeService;
        private readonly IUserService _userService;
        private readonly IAnnexDService _annexDService;
        private readonly string _stockId, _ppeId, _transpoId;

        public CustodianReportController(AppManEntities db)
        {
            _db = db;
            _custodianReportService = new CustodianReportService(_db);
            _custodianReportItemService = new CustodianReportItemService(_db);
            _custodianReportItemStockService = new CustodianReportItemStockService(_db);
            _custodianReportItemPpeService = new CustodianReportItemPpeService(_db);
            _custodianReportItemVehicleService = new CustodianReportItemVehicleService(_db);
            _parIssuanceService = new CustodianReportItemIssuanceParService(_db);
            _icsIssuanceService = new CustodianReportItemIssuanceIcsService(_db);
            _areIssuanceService = new CustodianReportItemIssuanceAreService(_db);
            _mrIssuanceService = new CustodianReportItemIssuanceMrService(_db);
            _rpcPpeIssuanceService = new CustodianReportItemIssuanceRpcPpeService(_db);
            _custodianReportSubmitForCountService = new CustodianReportSubmitForCountService(_db);
            _codextnService = new CodextnService(_db);
            _uploadService = new CustodianReportUploadService(_db);
            _scanUploadService = new CustodianDeptUploadService(_db).Create("SCAN");
            _itemCodeService = new ItemCodeService(_db);
            _userService = new UserService(_db);
            _annexDService = new AnnexDService(_db);

            _stockId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.STOCK);
            _ppeId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.PPE);
            _transpoId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.VEHICLE);
        }

        //public CustodianReportController(AppManEntities db,
        //    ICustodianReportService custodianReportService,
        //    ICustodianReportItemService custodianReportItemService,
        //    ICustodianReportItemStockService custodianReportItemStockService,
        //    ICustodianReportItemPpeService custodianReportItemPpeService,
        //    ICustodianReportItemVehicleService custodianReportItemVehicleService,
        //    ICustodianReportItemIssuanceParService custodianReportItemIssuanceParService,
        //    ICustodianReportItemIssuanceIcsService custodianReportItemIssuanceIcsService,
        //    ICustodianReportItemIssuanceAreService custodianReportItemIssuanceAreService,
        //    ICustodianReportItemIssuanceMrService custodianReportItemIssuanceMrService,
        //    ICustodianReportItemIssuanceRpcPpeService custodianReportItemIssuanceRpcPpeService,
        //    ICustodianReportSubmitForCountService custodianReportSubmitForCountService,
        //    ICodextnService codextnService,
        //    ICustodianReportUploadService custodianReportUploadService,
        //    ICustodianDeptUploadService custodianScanUploadService,
        //    IItemCodeService itemCodeService,
        //    IUserService userService, IAnnexDService annexDService)
        //{
        //    _db = db;
        //    _custodianReportService = custodianReportService;
        //    _custodianReportItemService = custodianReportItemService;
        //    _custodianReportItemStockService = custodianReportItemStockService;
        //    _custodianReportItemPpeService = custodianReportItemPpeService;
        //    _custodianReportItemVehicleService = custodianReportItemVehicleService;
        //    _parIssuanceService = custodianReportItemIssuanceParService;
        //    _icsIssuanceService = custodianReportItemIssuanceIcsService;
        //    _areIssuanceService = custodianReportItemIssuanceAreService;
        //    _mrIssuanceService = custodianReportItemIssuanceMrService;
        //    _rpcPpeIssuanceService = custodianReportItemIssuanceRpcPpeService;
        //    _custodianReportSubmitForCountService = custodianReportSubmitForCountService;
        //    _codextnService = codextnService;
        //    _uploadService = custodianReportUploadService;
        //    _scanUploadService = custodianScanUploadService.Create("SCAN");
        //    _itemCodeService = itemCodeService;
        //    _userService = userService;
        //    _annexDService = annexDService;

        //    _stockId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.STOCK);
        //    _ppeId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.PPE);
        //    _transpoId = _custodianReportService.GetAccountGroupMenuId(CustodianAccountGroup.VEHICLE);
        //}

        public ActionResult Stock()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.STOCK;
            ViewBag.Title = "Custodian Report - Supplies";

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            if (isAdmin || _annexDService.IsAny(userName))
            {
                ViewBag.AnnexDUser = true;
            }
            else
            {
                ViewBag.AnnexDUser = false;
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;

            return View();
        }

        public ActionResult StockDemand()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.STOCK;
            ViewBag.Title = "Custodian Report - Supplies";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = true;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);            
            ViewBag.IsAdmin = isAdmin;

            return View("Stock");
        }

        public ActionResult StockUpdate()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.STOCK;
            ViewBag.Title = "Custodian Report - Supplies - Update Item Code";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            ViewBag.IsAdmin = isAdmin;

            return View();
        }

        public ActionResult StockQuery()
        {
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.STOCK;
            ViewBag.Title = "Custodian Report - Supplies";

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            if (isAdmin || _annexDService.IsAny(userName))
            {
                ViewBag.AnnexDUser = true;
            }
            else
            {
                ViewBag.AnnexDUser = false;
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            return View();
        }

        public ActionResult Ppe()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.PPE;
            ViewBag.Title = "Custodian Report - Equipment";            
            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            if (isAdmin || _annexDService.IsAny(userName))
            {
                ViewBag.AnnexDUser = true;
            }
            else
            {
                ViewBag.AnnexDUser = false;
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;
            ViewBag.IsView = false;

            return View();
        }

        public ActionResult PpeDemand()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.PPE;
            ViewBag.Title = "Custodian Report - Equipment";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = true;
            ViewBag.IsView = false;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            ViewBag.IsAdmin = isAdmin;

            return View("Ppe");
        }

        public ActionResult PpeView()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.PPE;
            ViewBag.Title = "Custodian Report - Equipment";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;
            ViewBag.IsView = true;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            ViewBag.IsAdmin = isAdmin;

            return View("Ppe");
        }

        public ActionResult PpeUpdate()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.PPE;
            ViewBag.Title = "Custodian Report - Equpment - Update Item Code";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;
            ViewBag.IsView = false;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            ViewBag.IsAdmin = isAdmin;

            return View();
        }

        public ActionResult PpeQuery()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.PPE;
            ViewBag.Title = "Custodian Report - Equipment";

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            if (isAdmin || _annexDService.IsAny(userName))
            {
                ViewBag.AnnexDUser = true;
            }
            else
            {
                ViewBag.AnnexDUser = false;
            }
            ViewBag.IsView = false;

            ViewBag.IsAdmin = isAdmin;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            return View();
        }

        public ActionResult Transpo()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.VEHICLE;
            ViewBag.Title = "Custodian Report - Vehicles";

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            if (isAdmin || _annexDService.IsAny(userName))
            {
                ViewBag.AnnexDUser = true;
            }
            else
            {
                ViewBag.AnnexDUser = false;
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;
            ViewBag.IsView = false;

            return View();
        }

        public ActionResult TranspoDemand()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.VEHICLE;
            ViewBag.Title = "Custodian Report - Vehicles";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = true;
            ViewBag.IsView = false;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            ViewBag.IsAdmin = isAdmin;

            return View("Transpo");
        }

        public ActionResult TranspoView()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.VEHICLE;
            ViewBag.Title = "Custodian Report - Vehicles";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;
            ViewBag.IsView = true;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            ViewBag.IsAdmin = isAdmin;

            return View("Transpo");
        }

        public ActionResult TranspoUpdate()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.VEHICLE;
            ViewBag.Title = "Custodian Report - Vehicles - Update Item Code";
            ViewBag.AnnexDUser = false;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsDemand = false;

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsView = false;

            return View();
        }

        public ActionResult TranspoQuery()
        {
            TempData["AllowIndexAccess"] = true; // Set a flag to allow Index access
            ViewBag.AccountGroup = (int?)CustodianAccountGroup.VEHICLE;
            ViewBag.Title = "Custodian Report - Vehicles";

            string userName = ControllerContext.HttpContext.User.Identity.Name;
            var isAdmin = _userService.IsUserNameAdmin(userName);
            if (isAdmin || _annexDService.IsAny(userName))
            {
                ViewBag.AnnexDUser = true;
            }
            else
            {
                ViewBag.AnnexDUser = false;
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            ViewBag.IsView = false;

            return View();
        }

        public ActionResult Uploads()
        {
            ViewBag.Title = "Custodian Report - Uploads";

            string userName = ControllerContext.HttpContext.User.Identity.Name;            
            ViewBag.ForYear = _custodianReportService.GetReportingYearEnd();
            
            return View();
        }

        public ActionResult UploadsRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? locationId)
        {
            var data = _uploadService.GetAllCustodianUploads(forYear, deptId, locationId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> SubmitForCount(Guid reportId, Guid? locationId, int? accountGroup, string url)
        {
            try
            {
                //var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                //Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                //Access access = await accessTask;
                //if (!access.AllowPost)
                //{
                //    ModelState.AddModelError("GridError", "Access Denied!");
                //}
                //else
                //{
                //    string user = ControllerContext.HttpContext.User.Identity.Name;
                //    DateTime date = System.DateTime.Now;

                //    await _custodianReportSubmitForCountService.SubmitAsync(reportId, locationId, url, user, date, true);
                //}

                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                await _custodianReportSubmitForCountService.SubmitAsync(reportId, locationId, url, user, date, true);
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
        public async Task<ActionResult> SubmitForCountNew(int? forYear, Guid? deptId, Guid? locationId, int? accountGroup, string url)
        {
            try
            {                
                string user = ControllerContext.HttpContext.User.Identity.Name;
                DateTime date = System.DateTime.Now;

                var data = await _custodianReportSubmitForCountService.SubmitNewAsync(forYear, deptId, locationId, accountGroup, url, user, date, true);
                return Json(new { Errors = "", ReportId = data.Id }, JsonRequestBehavior.AllowGet);
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
                //var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                //Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
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

            return Json(new { Errors = "", ReportId = reportId }, JsonRequestBehavior.AllowGet);
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
        public ActionResult _StockItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemStockService.GetAllByDeptAcctGroup(forYear, deptId, sectionId, accountGroup, user, isDemand);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _StockUpdateItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand, Guid? itemCodeId)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemStockService.GetAllByDeptAcctGroupItemCodeId(forYear, deptId, sectionId, accountGroup, user, isDemand, itemCodeId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _StockItemReadAll([DataSourceRequest] DataSourceRequest request, int? forYear, int? accountGroup)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemStockService.GetAllByAcctGroup(forYear, accountGroup, user);

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
                else 
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
        public ActionResult _PpeItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemPpeService.GetAllByDeptAcctGroup(forYear, deptId, sectionId, accountGroup, user, isDemand);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _PpeUpdateItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand, Guid? itemCodeId)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemPpeService.GetAllByDeptAcctGroupItemCodeId(forYear, deptId, sectionId, accountGroup, user, isDemand, itemCodeId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _PpeItemReadAll([DataSourceRequest] DataSourceRequest request, int? forYear, int? accountGroup)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemPpeService.GetAllByAcctGroup(forYear, accountGroup, user);

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
                else 
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
        public ActionResult _VehicleItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand, bool? isView)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemVehicleService.GetAllByDeptAcctGroup(forYear, deptId, sectionId, accountGroup, user, isDemand, isView);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _VehicleUpdateItemRead([DataSourceRequest] DataSourceRequest request, int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, bool? isDemand, Guid? itemCodeId, bool? isView)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemVehicleService.GetAllByDeptAcctGroupItemCodeId(forYear, deptId, sectionId, accountGroup, user, isDemand, itemCodeId, isView);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult _VehicleItemReadAll([DataSourceRequest] DataSourceRequest request, int? forYear, int? accountGroup)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportItemVehicleService.GetAllByAcctGroup(forYear, accountGroup, user);

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
                else
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
                else 
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
                else
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
                else
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
                else
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

        #region STOCK RPCPPE ISSUANCE
        public ActionResult _StockRpcPpeIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _StockRpcPpeIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _rpcPpeIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _StockRpcPpeIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
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

                    model = await _rpcPpeIssuanceService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _StockRpcPpeIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
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

                    model = await _rpcPpeIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _StockRpcPpeIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _stockId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcPpeIssuanceService.DeleteAsync(model, user, date);
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
                else
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
                else
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
                else 
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
                else
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

        #region PPE RPCPPE ISSUANCE
        public ActionResult _PpeRpcPpeIssuance(Guid? reportItemId)
        {
            ViewData["reportItemId"] = reportItemId;
            return PartialView();
        }

        public ActionResult _PpeRpcPpeIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _rpcPpeIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PpeRpcPpeIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
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

                    model = await _rpcPpeIssuanceService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _PpeRpcPpeIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
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

                    model = await _rpcPpeIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _PpeRpcPpeIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcPpeIssuanceService.DeleteAsync(model, user, date);
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
        public ActionResult _VehicleParIssuance(Guid? reportItemId, bool? isView)
        {
            ViewData["reportItemId"] = reportItemId;
            ViewBag.IsView = isView;
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
                else
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
        public ActionResult _VehicleIcsIssuance(Guid? reportItemId, bool? isView)
        {
            ViewData["reportItemId"] = reportItemId;
            ViewBag.IsView = isView;
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
                else
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
        public ActionResult _VehicleAreIssuance(Guid? reportItemId, bool? isView)
        {
            ViewData["reportItemId"] = reportItemId;
            ViewBag.IsView = isView;
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
                else
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
        public ActionResult _VehicleMrIssuance(Guid? reportItemId, bool? isView)
        {
            ViewData["reportItemId"] = reportItemId;
            ViewBag.IsView = isView;
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
                else
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

        #region VEHICLE RPCPPE ISSUANCE
        public ActionResult _VehicleRpcPpeIssuance(Guid? reportItemId, bool? isView)
        {
            ViewData["reportItemId"] = reportItemId;
            ViewBag.IsView = isView;
            return PartialView();
        }

        public ActionResult _VehicleRpcPpeIssuanceRead([DataSourceRequest] DataSourceRequest request, Guid? reportItemId)
        {
            var data = _rpcPpeIssuanceService.GetAll(reportItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _VehicleRpcPpeIssuanceCreate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
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

                    model = await _rpcPpeIssuanceService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _VehicleRpcPpeIssuanceUpdate([DataSourceRequest] DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
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

                    model = await _rpcPpeIssuanceService.UpdateAsync(model, user, date);
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

        public async Task<ActionResult> _VehicleRpcPpeIssuanceDestroy([DataSourceRequest]DataSourceRequest request, CustodianReportItemIssuanceRpcPpeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _ppeId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpcPpeIssuanceService.DeleteAsync(model, user, date);
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
        public async Task<ActionResult> StickerRpt(Guid? id, int? accountGroup)
        {
            //Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
            //Access access = await accessTask;
            //if (!access.AllowPrint)
            //{
            //    return new HttpStatusCodeResult(401, "Access Denied");
            //}

            //var rpci = _db.RPCIs.Find(id);
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            if (accountGroup == (int?)CustodianAccountGroup.STOCK)
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/StickerIcs.rpt"));
            }
            else
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/StickerPar.rpt"));
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
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
                table.ApplyLogOnInfo(logonInfo);
            }

            rpt.SetParameterValue("@cSource", "CUSTODIAN");
            rpt.SetParameterValue("@uSourceId", id.ToString());

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");

        }

        public async Task<ActionResult> CustodianStockRpt(Guid? id, int? accountGroup)
        {
            //Task<Access> accessTask = Access(User.Identity.GetUserId(), _transpoId);
            //Access access = await accessTask;
            //if (!access.AllowDelete)
            //{
            //    return new HttpStatusCodeResult(401, "Access Denied");
            //}

            //var rpci = _db.RPCIs.Find(id);
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
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
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
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
            //var itemTypeCode = model.ItemType_Code;
            //var itemCode = model.Item_Code;
            if (model.Id != Guid.Empty)
            {
                model = await _custodianReportItemStockService.GetByIdAsync(model.Id);
            }
            else
            {
                model.Multipliers = 0;
            }

            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
            if (!string.IsNullOrEmpty(partialView))
            {
                partialView = $"_Stock{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadPpeFields(CustodianReportItemPpeVM model)
        {
            //var itemTypeCode = model.ItemType_Code;
            //var itemCode = model.Item_Code;
            if (model.Id != Guid.Empty)
            {
                model = await _custodianReportItemPpeService.GetByIdAsync(model.Id);
            }

            //var itemCode = await _itemCodeService.GetByIdAsync(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
            if (!string.IsNullOrEmpty(partialView))
            {
                partialView = $"_Ppe{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> LoadVehicleFields(CustodianReportItemVehicleVM model)
        {
            //var itemTypeCode = model.ItemType_Code;
            //var itemCode = model.Item_Code;
            if (model.Id != Guid.Empty)
            {
                model = await _custodianReportItemVehicleService.GetByIdAsync(model.Id);
            }

            //var itemCode = await _itemCodeService.GetByIdAsync(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);

            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
            if (!string.IsNullOrEmpty(partialView))
            {
                partialView = $"_Vehicle{partialView}";
            }

            return PartialView(partialView, model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<JsonResult> GetStockNo(CustodianReportItem fields)
        {
            var stockNo = await _custodianReportItemStockService.GetStockNoAsync(fields);

            return Json(new { StockNo = stockNo }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<JsonResult> IsSubmitForCount(Guid? reportId, Guid? locationId)
        {
            var data = await _custodianReportSubmitForCountService.GetByLocationAsync(reportId, locationId);
            if (data == null || data.Status != "Submit")
            {
                return Json(new { IsSubmitForCount = false }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { IsSubmitForCount = true, UpdateDate = data.UpdatedDt.Value.ToShortDateString() }, JsonRequestBehavior.AllowGet);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<JsonResult> IsSubmitForCountNew(int? forYear, Guid? deptId, Guid? locationId, int? accountGroup)
        {
            var data = await _custodianReportSubmitForCountService.GetByCustodianAccountAsync(forYear, deptId, locationId, accountGroup);
            if (data == null || data.Status != "Submit")
            {
                return Json(new { IsSubmitForCount = false }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { IsSubmitForCount = true, UpdateDate = data.UpdatedDt.Value.ToShortDateString(), ReportId = data.ReportId }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<JsonResult> GetSetLotCount(int? forYear, Guid? deptId, Guid? locationId, Guid? custodianReportItemId, string setLotNo)
        {
            var count = await _custodianReportItemService.GetSetLotNoCountAsync(forYear, deptId, locationId, custodianReportItemId, setLotNo);
            var amount = await _custodianReportItemService.GetSetLotNoAmountAsync(forYear, deptId, locationId, custodianReportItemId, setLotNo);
            return Json(new { Count = count, Amount = amount }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }

        public async Task<ActionResult> ExcelExportReport(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup)
        {
            return await ExcelExport(forYear, deptId, sectionId, accountGroup, "", null, "", "", "", "");
        }

        public async Task<ActionResult> ExcelExportAll(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            return await ExcelExport(forYear, deptId, sectionId, accountGroup, mainAccount, asOf, subAccount1, subAccount2, subAccount3, subAccount4);
        }

        public async Task<ActionResult> ExcelExport(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            try
            {
                string exportFileName = "";
                if (accountGroup == (int?)CustodianAccountGroup.STOCK)
                {
                    exportFileName = "CustodianSupplies";
                }
                else if (accountGroup == (int?)CustodianAccountGroup.PPE)
                {
                    exportFileName = "CustodianEquipment";
                }
                else if (accountGroup == (int?)CustodianAccountGroup.VEHICLE)
                {
                    exportFileName = "CustodianVehicles";
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                var templateFilePath = Server.MapPath($"~/App_Data/{exportFileName}Template.xlsx");
                var stream = _custodianReportItemService.ProcessExcelFile(forYear, deptId, sectionId, templateFilePath, accountGroup, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, user);
                var locationCode = "ALL";
                if (deptId != null && deptId != Guid.Empty)
                {
                    locationCode = (await _codextnService.GetByIdAsync(deptId))?.Code;
                }
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{locationCode}_{exportFileName}_{DateTime.Now.ToShortDateString()}.xlsx");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        public async Task<ActionResult> ExcelExportAnnexAll(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            return await ExcelExportAnnex(forYear, deptId, sectionId, accountGroup, annex, mainAccount, asOf, subAccount1, subAccount2, subAccount3, subAccount4);
        }

        public async Task<ActionResult> ExcelExportAnnexReport(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string annex)
        {
            return await ExcelExportAnnex(forYear, deptId, sectionId, accountGroup, annex, "", null, "", "", "", "");
        }

        public async Task<ActionResult> ExcelExportAnnex(int? forYear, Guid? deptId, Guid? sectionId, int? accountGroup, string annex, string mainAccount, DateTime? asOf
            , string subAccount1, string subAccount2, string subAccount3, string subAccount4)
        {
            try
            {
                string exportFileName = "";
                if (accountGroup == (int?)CustodianAccountGroup.STOCK)
                {
                    exportFileName = $"CustodianSuppliesAnnex";
                }
                else if (accountGroup == (int?)CustodianAccountGroup.PPE)
                {
                    exportFileName = $"CustodianEquipmentAnnex";
                }
                else if (accountGroup == (int?)CustodianAccountGroup.VEHICLE)
                {
                    exportFileName = $"CustodianVehiclesAnnex";
                }

                string user = ControllerContext.HttpContext.User.Identity.Name;
                var templateFilePath = Server.MapPath($"~/App_Data/{exportFileName}Template.xlsx");
                var stream = _custodianReportItemService.ProcessExcelFileAnnex(forYear, deptId, sectionId, templateFilePath, accountGroup, annex, mainAccount, asOf
                    , subAccount1, subAccount2, subAccount3, subAccount4, user);
                var locationCode = "ALL";
                if (deptId != null && deptId != Guid.Empty)
                {
                    locationCode = (await _codextnService.GetByIdAsync(deptId))?.Code;
                }
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{locationCode}_{exportFileName}-{annex}_{DateTime.Now.ToShortDateString()}.xlsx");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }


        //public ActionResult ExcelExportAnnex(Guid? reportId, int? accountGroup, string annex)
        //{
        //    try
        //    {
        //        string exportFileName = "";
        //        if (accountGroup == (int?)CustodianAccountGroup.STOCK)
        //        {
        //            exportFileName = $"CustodianSuppliesAnnex";
        //        }
        //        else if (accountGroup == (int?)CustodianAccountGroup.PPE)
        //        {
        //            exportFileName = $"CustodianEquipmentAnnex";
        //        }
        //        else if (accountGroup == (int?)CustodianAccountGroup.VEHICLE)
        //        {
        //            exportFileName = $"CustodianVehiclesAnnex";
        //        }

        //        var templateFilePath = Server.MapPath($"~/App_Data/{exportFileName}Template.xlsx");
        //        var stream = _custodianReportItemService.ProcessExcelFileAnnex(reportId, templateFilePath, accountGroup, annex, "", null);

        //        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{exportFileName}-{annex}.xlsx");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new HttpStatusCodeResult(500, ex.Message);
        //    }
        //}

        #region IMAGE UPLOADS
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
                else
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
            var asOf = _db.CustodianReportItems.Where(w => w.Id == itemId).Select(s => s.CustodianReport.AsOf).FirstOrDefault();
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
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowDownload)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportService.Download(forYear, deptId, accountGroup, user, date);
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
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
                Access access = await accessTask;
                if (!access.AllowDownload)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _custodianReportService.Upload(forYear, deptId, accountGroup, user, date);
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

        public async Task<ActionResult> UpdateSetLotRemarks(int forYear)
        {
            try
            {
                await _custodianReportItemService.UpdateAllSetLotRemarksAsync(forYear);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }

            return new HttpStatusCodeResult(200, "Update Complete");            
        }

        #endregion


        [HttpPost]

        public async Task<ActionResult> UpdateItemCode(int? reportingYearEnd, string selectedIds, Guid? newItemId, int? accountGroup)
        {
            try
            {
                var menuId = _custodianReportService.GetAccountGroupMenuId(accountGroup);
                Task<Access> accessTask = Access(User.Identity.GetUserId(), menuId);
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

                    _custodianReportItemService.UpdateItemCode(reportingYearEnd, selectedIds, newItemId, accountGroup, user, date);
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
        public async Task<JsonResult> ValidateYear(int? year)
        {
            if (year == null || year == 0 || year < 1900)
            {
                return Json(new { Errors = "Invalid year." }, JsonRequestBehavior.AllowGet);
            }

            var data = await _custodianReportService.GetReportingYearEndAsync((int)year);
            if (data == null)
            {
                return Json(new { Errors = "Setup not found for this year." }, JsonRequestBehavior.AllowGet);
            }
            //else if (!string.IsNullOrWhiteSpace(data.Desc2) && data.Desc2.ToUpper() == "Y")
            //{
            //    return Json(new { Errors = "Entries for this year are already locked." }, JsonRequestBehavior.AllowGet);
            //}

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}