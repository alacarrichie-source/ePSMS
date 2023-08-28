using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Interfaces;
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
    [AppAuthorize("RIS")]
    public class RISController : Controller
    {
        private AppManEntities db = new AppManEntities();
        private IRisService risService;
        private IRisItemService risItemService;
        private IRisItemExtnService risItemExtnService;
        private ICodextnService codextnService;
        private IPsCodeService psCodeService;

        public RISController()
        {
            this.risService = new RisService(db);
            this.risItemService = new RisItemService(db);
            this.risItemExtnService = new RisItemExtnService(db);
            this.codextnService = new CodextnService(db);
            this.psCodeService = new PsCodeService(db);
        }

        // GET: RIS
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RISRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = risService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RISCreate([DataSourceRequest] DataSourceRequest request, RIS_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (await risService.GetByRisNoAsync(model.RisNo) != null)
                {
                    ModelState.AddModelError("RIS No.", "RIS No. already exists!");
                }
                
                if (model != null && ModelState.IsValid)
                {                    
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await risService.CreateAsync(model, user, date);                    
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
        public async Task<ActionResult> RISUpdate([DataSourceRequest] DataSourceRequest request, RIS_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }
                else if (await risService.IsPostedAsync(model.Id))
                {
                    ModelState.AddModelError("RIS No.", "RIS Number already Posted, cannot update!");
                }
                else if (await risService.IsPrPostedAsync(model.Id))
                {
                    ModelState.AddModelError("RIS No.", "This RIS No has a posted PR, cannot update!");
                }
                else if (await risService.GetAnyRisNoAsync(model.Id, model.RisNo))
                {
                    ModelState.AddModelError("RIS No.", "RIS No. already exists!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await risService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> RISDestroy([DataSourceRequest]DataSourceRequest request, RIS_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await risService.IsPostedAsync(model.Id))
                {
                    ModelState.AddModelError("RIS No.", "RIS Number already Posted, cannot delete!");
                }
                else if (await risService.IsPrPostedAsync(model.Id))
                {
                    ModelState.AddModelError("RIS No.", "This RIS No has a posted PR, cannot delete!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await risService.DeleteAsync(model, user, date);                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostRIS(Guid risId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await risService.GetByIdAsync(risId) == null)
                {
                    ModelState.AddModelError("RIS", "Invalid RIS Id");
                }
                else if (await risService.IsPostedAsync(risId))
                {
                    ModelState.AddModelError("RIS No.", "RIS Number already Posted, cannot post again!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await risService.PostAsync(risId, user, date);
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
        public async Task<ActionResult> UnpostRIS(Guid risId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await risService.GetByIdAsync(risId) == null)
                {
                    ModelState.AddModelError("RIS", "Invalid RIS Id");
                }
                else if (await risService.IsPrPostedAsync(risId))
                {
                    ModelState.AddModelError("RIS No.", "This RIS No has a posted PR, cannot unpost!");
                }
                else if (!(await risService.IsPostedAsync(risId)))
                {
                    ModelState.AddModelError("RIS No.", "RIS Number not yet posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await risService.UnpostAsync(risId, user, date);
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

        #region ITEMS
        public ActionResult _RISItem(Guid risId)
        {
            ViewData["risId"] = risId;
            return PartialView();
        }
        public async Task<ActionResult> _RISItemAddEdit(Guid risId, Guid? risItemId)
        {
            var data = await risItemService.GetVmByIdAsync(risItemId);
            if (data == null)
            {
                data = new RisItemVM()
                {
                    Id = Guid.NewGuid(),
                    RisId = risId
                };
            }
            ViewData["risItemId"] = risItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISItemSave(RisItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await risService.IsPrPostedAsync(model.Id))
                {
                    ModelState.AddModelError("RIS No.", "This RIS No has a posted PR, cannot update!");
                }
                else if (await risService.IsPostedAsync((Guid)model.RisId))
                {
                    ModelState.AddModelError("RIS No.", "RIS Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    
                    var entity = await risItemService.GetByIdAsync(model.Id);

                    if (entity == null)
                    {
                        model = await risItemService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await risItemService.UpdateAsync(model, user, date);
                    }

                    var risItemExtns = (List<RisItemExtnVM>)Newtonsoft.Json.JsonConvert.DeserializeObject(model.GridRisItemExtns, typeof(List<RisItemExtnVM>));
                    await risItemExtnService.SaveAsync(model.Id, risItemExtns, user, date);
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


        public ActionResult _RISItemRead([DataSourceRequest] DataSourceRequest request, Guid? risId)
        {
            var data = risItemService.GetByRisId(risId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISItemDestroy([DataSourceRequest]DataSourceRequest request, RisItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await risService.IsWithPrAsync(model.Id))
                {
                    ModelState.AddModelError("DeleteError", "This RIS No has a PR, cannot delete!");
                }
                else if (await risService.IsPostedAsync((Guid)model.RisId))
                {
                    ModelState.AddModelError("DeleteError", "RIS Number already Posted, cannot delete!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await risItemService.DeleteAsync(model, user, date);
                }

            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion        

        public ActionResult _RISItemExtnBatchRead([DataSourceRequest] DataSourceRequest request, Guid? risItemId, string psType)
        {
            var data = risItemExtnService.GetBatchInfo(risItemId, psType);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public async Task<ActionResult> RISRpt(string risNo)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "report_ris");
                Access access = await accessTask;
                if (access == null)
                {
                    throw new Exception("Access Denied!");
                }

            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                return View("Error");
            }

            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Ris.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = rpt.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = rpt.ReportDefinition.Sections;
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

            var lgu = codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cRisNo", risNo);
            rpt.SetParameterValue("LGU", lgu);
            
            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");            
        }

        public ActionResult Requisition()
        {
            return View();
        }

        public ActionResult RequisitionRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = psCodeService.GetMaintenanceView();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult Cart(List<PsCodeVM> cartItems)
        {
            //var cartItems = JsonConvert.DeserializeObject<List<PsCodeVM>>(localStorage.getItem("cartItems")) ?? new List<OrderItem>();
            return View(cartItems);
        }
    }
}