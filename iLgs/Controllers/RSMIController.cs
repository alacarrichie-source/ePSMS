using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
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
    [AppAuthorize("RSMI")]
    public class RSMIController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly ICodextnService _codextnService;
        private readonly IRsmiService _rsmiService;
        private string _menuId = string.Empty;

        public RSMIController()
        {
            //_db = new AppManEntities();
            _codextnService = new CodextnService(_db);
            _rsmiService = new RsmiService(_db);
        }

        //public RSMIController(AppManEntities db, ICodextnService codextnService, IRsmiService rsmiService)
        //{
        //    _db = db;
        //    _codextnService = codextnService;
        //    _rsmiService = rsmiService;
        //}

        // GET: RSMI
        public async Task<ActionResult> Index()
        {
            _menuId = "rsmi";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["rsmi"] = _menuId;
            return View();
        }        

        public ActionResult RSMIRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _rsmiService.GetAll();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public async Task<ActionResult> RunningTotal()
        {
            _menuId = "rsmi_running_total";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["rsmi"] = _menuId;
            ViewBag.StartDate = new DateTime(DateTime.Now.Year-1, 1, 1);
            ViewBag.EndDate = new DateTime(DateTime.Now.Year-1, 12, 31);
            ViewBag.Type = "ALL";
            ViewBag.Fund = "ALL";
            ViewBag.DeptId = Guid.Empty;
            return View();
        }

        public ActionResult RunningTotalRead([DataSourceRequest] DataSourceRequest request, string type, DateTime? startDate, DateTime? endDate, string fund, Guid? deptId)
        {
            var data = _rsmiService.GetTotalList(type, startDate, endDate, fund, deptId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult _RSMIAdd()
        {

            RSMIProcessVM model = new RSMIProcessVM()
            {
                DateFrom = DateTime.Now,
                DateTo = DateTime.Now
            };
            return PartialView(model);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RSMIAddSave([DataSourceRequest] DataSourceRequest request, RSMIProcessVM model)
        {
            try
            {
                _menuId = TempData["rsmi"]?.ToString();
                TempData.Keep("rsmi");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rsmiService.GenerateAsync(model, user, date);
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
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RSMIUpdate([DataSourceRequest] DataSourceRequest request, RsmiVM model)
        {
            try
            {
                _menuId = TempData["rsmi"]?.ToString();
                TempData.Keep("rsmi");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rsmiService.UpdateAsync(model, user, date);
                    
                }
            }
            //catch (Exception e)
            //{
            //    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
            //         "please contact tech support with this message: " + e.Message);
            //}

            //return Json(new[] { model }.ToDataSourceResult(request, ModelState));
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RSMIDestroy([DataSourceRequest]DataSourceRequest request, RsmiVM model)
        {
            try
            {
                _menuId = TempData["rsmi"]?.ToString();
                TempData.Keep("rsmi");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rsmiService.DeleteAsync(model, user, date);
                }
            }
            //catch (Exception e)
            //{
            //    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
            //         "please contact tech support with this message: " + e.Message);
            //}

            //return Json(new[] { model }.ToDataSourceResult(request, ModelState));
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
        public async Task<ActionResult> RsmiPost(Guid? id)
        {
            try
            {
                _menuId = TempData["rsmi"]?.ToString();
                TempData.Keep("rsmi");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rsmiService.PostAsync(id, user, date);
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
        public async Task<ActionResult> RsmiUnPost(Guid? id)
        {
            try
            {
                _menuId = TempData["rsmi"]?.ToString();
                TempData.Keep("rsmi");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rsmiService.UnPostAsync(id, user, date);
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

        public ActionResult RSMIItemRead([DataSourceRequest] DataSourceRequest request, Guid? rsmiId)
        {
         
            var data = _db.RSMIItems.Where(w => w.RsmiId == rsmiId)
                .Select(s => new RSMIItemVM
                {
                    Id = s.Id,
                    RisNo = s.RisNo,
                    StockNo = s.StockNo,
                    RCC = s.RCC,
                    ItemName = s.ItemName,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount
                }).AsQueryable();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult RSMIRecapRead([DataSourceRequest] DataSourceRequest request, Guid? rsmiId)
        {

            var data = _db.RSMIRecaps.Where(w => w.RsmiId == rsmiId)
                .Select(s => new RSMIRecapVM
                {
                    Id = s.Id,
                    StockNo = s.StockNo,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    AccountCode = s.AccountCode
                }).AsQueryable();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        #region PRINTING
        public ActionResult _PrintRsmi()
        {
            var date = DateTime.Now;
            var model = new RsmiPrintVM()
            {
                DateFrom = date,
                DateTo = date
            };

            return PartialView(model);
        }

        public async Task<ActionResult> RsmiRpt(RsmiPrintVM model)
        {
            Sections crSections;
            ReportDocument crReportDocument, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            crReportDocument = new ReportDocument();
            crReportDocument.FileName = Server.MapPath(Url.Content("~/Reports/Rsmi.rpt"));

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = crReportDocument.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;
            crConnectionInfo.IntegratedSecurity = false;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = crReportDocument.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault()?.Description;

            crReportDocument.SetParameterValue("LGU", lgu);
            crReportDocument.SetParameterValue("WithDailyRecap", model.WithDailyRecap);
            crReportDocument.SetParameterValue("Custodian", model.Custodian ?? "");
            crReportDocument.SetParameterValue("@cFund", model.Fund);
            crReportDocument.SetParameterValue("@dBdate", model.DateFrom);
            crReportDocument.SetParameterValue("@dEdate", model.DateTo);
            crReportDocument.SetParameterValue("@cType", model.RpciType == "A" ? "" : model.RpciType);

            if (model.SavePrints)
            {
                Stream stream = crReportDocument.ExportToStream(CrystalDecisions.Shared.ExportFormatType.Excel);
                crReportDocument.Close();
                crReportDocument.Dispose();
                return File(stream, "application/xlsx");
            }
            else
            {
                Stream stream = crReportDocument.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                crReportDocument.Close();
                crReportDocument.Dispose();
                return File(stream, "application/pdf");                
            }
        }

        public ActionResult _PrintSum()
        {
            var date = DateTime.Now;
            var model = new RsmiPrintVM()
            {
                DateFrom = date,
                DateTo = date
            };

            return PartialView(model);
        }

        public async Task<ActionResult> RsmiSumRpt(RsmiPrintVM model)
        {
            Sections crSections;
            ReportDocument crReportDocument, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            crReportDocument = new ReportDocument();

            if (model.Type == "1")
            {
                crReportDocument.FileName = Server.MapPath(Url.Content("~/Reports/RsmiAcctSum.rpt"));
            }
            else
            {
                crReportDocument.FileName = Server.MapPath(Url.Content("~/Reports/RsmiPoSum.rpt"));
            }

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = crReportDocument.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;
            crConnectionInfo.IntegratedSecurity = false;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = crReportDocument.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault()?.Description;

            crReportDocument.SetParameterValue("LGU", lgu);
            crReportDocument.SetParameterValue("WithDailyRecap", model.WithDailyRecap);
            crReportDocument.SetParameterValue("Custodian", model.Custodian ?? "");
            crReportDocument.SetParameterValue("@cFund", model.Fund);
            crReportDocument.SetParameterValue("@dBdate", model.DateFrom);
            crReportDocument.SetParameterValue("@dEdate", model.DateTo);
            crReportDocument.SetParameterValue("@cType", model.RpciType == "A" ? "" : model.RpciType);

            if (model.SavePrints)
            {
                Stream stream = crReportDocument.ExportToStream(CrystalDecisions.Shared.ExportFormatType.Excel);
                crReportDocument.Close();
                crReportDocument.Dispose();
                return File(stream, "application/xlsx");
            }
            else
            {
                Stream stream = crReportDocument.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                crReportDocument.Close();
                crReportDocument.Dispose();
                return File(stream, "application/pdf");
            }
        }
        #endregion        

        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }
    }
}