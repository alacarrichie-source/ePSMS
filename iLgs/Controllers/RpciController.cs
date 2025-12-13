using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.RPC;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("RPCI")]
    public class RpciController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IRpciService _rpciService;
        private readonly IRpciItemService _rpciItemService;
        private readonly ICodextnService _codextnService;

        public RpciController(AppManEntities db, IRpciService rpciService, IRpciItemService rpciItemService, ICodextnService codextnService)
        {
            _db = db;
            _rpciService = rpciService;
            _rpciItemService = rpciItemService;
            _codextnService = codextnService;
        }

        public ActionResult SemiExpendable()
        {
            ViewData["Type"] = "SE";
            ViewData["IsPosted"] = true;
            ViewBag.Title = "Semi-Expendable";
            ViewBag.Header = "Semi-Expendable";

            return View("Index");
        }

        public ActionResult NotPosted()
        {
            ViewBag.Type = "C";
            ViewData["IsPosted"] = false;
            ViewBag.Title = "Report on the Physical Count of Inventories (RPCI) - Not Posted Records";
            ViewBag.Header = "RPCI";

            return View("Index");
        }        


        // GET: Rpci
        public ActionResult Index()
        {
            ViewData["Type"] = "C";
            ViewData["IsPosted"] = true;
            ViewBag.Title = "Report on the Physical Count of Inventories (RPCI) - Posted Records";
            ViewBag.Header = "RPCI";

            return View();
        }

        public ActionResult RpciRead([DataSourceRequest] DataSourceRequest request, bool? isPosted, string type)
        {
            var data = _rpciService.GetAll(isPosted, type);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RpciCreate([DataSourceRequest] DataSourceRequest request, RPCI_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }


                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciService.GenerateAsync(model, user, date);
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
        public async Task<ActionResult> RpciUpdate([DataSourceRequest] DataSourceRequest request, RPCI_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> RpciDestroy([DataSourceRequest]DataSourceRequest request, RPCI_VM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciService.DeleteAsync(model, user, date);
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
        public async Task<ActionResult> RpciPost(Guid? rpciId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rpciService.PostAsync(rpciId, user, date);
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
        public async Task<ActionResult> RpciUnPost(Guid? rpciId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _rpciService.UnPostAsync(rpciId, user, date);
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

        public ActionResult _RpciItem(Guid rpciId)
        {
            ViewData["rpciId"] = rpciId;
            return PartialView();
        }


        //public async Task<ActionResult> _RpciItemAddEdit(Guid orderId, Guid? orderItemId)
        //{
        //    var data = await _rpciItemService.GetByIdAsync(orderItemId);
        //    if (data == null)
        //    {
        //        data = new OrderItemVM()
        //        {
        //            Id = Guid.NewGuid(),
        //            OrderId = orderId,
        //            Mode = "A"
        //        };
        //    }
        //    else
        //    {
        //        data.Mode = "E";
        //    }
        //    ViewData["orderItemId"] = orderItemId;
        //    return PartialView(data);
        //}


        public ActionResult _RpciItemRead([DataSourceRequest] DataSourceRequest request, Guid? rpciId)
        {
            var data = _rpciItemService.GetVmByRpciId(rpciId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RpciItemCreate([DataSourceRequest] DataSourceRequest request, RPCIItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciItemService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _RpciItemUpdate([DataSourceRequest] DataSourceRequest request, RPCIItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciItemService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _RpciItemDestroy([DataSourceRequest]DataSourceRequest request, RPCIItemVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rpci");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _rpciItemService.DeleteAsync(model, user, date);
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


        #region PRINTOUTS        
        public async Task<ActionResult> RpciRpt(Guid? id, string type, int save)
        {
            //var rpci = _db.RPCIs.Find(id);
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Rpci.rpt"));
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
            rpt.SetParameterValue("@uRpciId", id.ToString());
            rpt.SetParameterValue("@cType", type == "A" ? "" : type);
            if (type == "A") // ALL
            {
                rpt.SetParameterValue("TITLE", "REPORT ON THE PHYSICAL COUNT OF INVENTORIES");
            }
            if (type == "C") // consumables
            {
                rpt.SetParameterValue("TITLE", "REPORT ON THE PHYSICAL COUNT OF INVENTORIES - CONSUMABLES");
            }
            else if (type == "SE")
            {
                rpt.SetParameterValue("TITLE", "REPORT ON THE PHYSICAL COUNT OF SEMI-EXPENDABLE PROPERTY");
            }

            if (save == 0)
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/pdf");
            }
            else
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.Excel);                
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/xlsx", $"rpci.xls");
            }
        }

        public ActionResult _PrintList(Guid id)
        {
            var model = new RsmiPrintVM()
            {
                Id = id
            };
            return PartialView(model);
        }

        public ActionResult _PrintSum(bool isPosted)
        {
            var date = DateTime.Now;
            var model = new RsmiPrintVM()
            {
                IsPosted = isPosted,
                DateFrom = date,
                DateTo = date                
            };

            return PartialView(model);
        }

        public async Task<ActionResult> RpciSumRpt(RsmiPrintVM model)
        {            
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            if (model.Type == "1")
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpciAcctSum.rpt"));
            }
            else
            {
                rpt.FileName = Server.MapPath(Url.Content("~/Reports/RpciPoSum.rpt"));
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

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault()?.Description;
            var type = model.RpciType;

            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("@cType", type == "A" ? "" : type);
            rpt.SetParameterValue("@cFund", model.Fund);
            rpt.SetParameterValue("@dAsOfDate", model.DateFrom);
            rpt.SetParameterValue("@bIsPosted", model.IsPosted);
            if (type == "A")
            {
                rpt.SetParameterValue("TITLE", "REPORT ON THE PHYSICAL COUNT OF INVENTORIES");
            }
            else if (type == "C")
            {
                rpt.SetParameterValue("TITLE", "REPORT ON THE PHYSICAL COUNT OF INVENTORIES - CONSUMABLES");
            }
            else if (type == "SE")
            {
                rpt.SetParameterValue("TITLE", "REPORT ON THE PHYSICAL COUNT OF SEMI-EXPENDABLE PROPERTY");
            }

            if (model.SavePrints)
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.Excel);
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/xlsx", $"RpciSumRpt.xls");
            }
            else
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/pdf");
            }
        }
        #endregion
    }
}