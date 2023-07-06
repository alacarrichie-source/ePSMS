using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using iLgs.Utilities;
using Newtonsoft.Json;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Data.SqlClient;
using iLgs.Services.Interfaces;
using iLgs.Services;
using System.IO;

namespace iLgs.Controllers
{
    [AppAuthorize("AIRS")]
    public class AIRsController : Controller
    {
        private AppManEntities db = new AppManEntities();
        private IAirService airService;
        private IAirItemService airItemService;
        private ICodextnService codextnService;
        private IOrderService orderService;
        private IOrderItemExtnService orderItemExtnService;

        public AIRsController()
        {
            this.airService = new AirService(db);
            this.airItemService = new AirItemService(db);
            this.codextnService = new CodextnService(db);
            this.orderService = new OrderService(db);
            this.orderItemExtnService = new OrderItemExtnService(db);
        }

        // GET: 
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AIRRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = airService.GetAll();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> AIRCreate([DataSourceRequest] DataSourceRequest request, AIR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (await airService.GetByAirNoAsync(model.AIRNo) != null)
                {
                    ModelState.AddModelError("AIR No.", "AIR No. already exists!");
                }
                else
                {
                    var order = await orderService.GetByIdAsync((Guid)model.OrderId);
                    if (order == null)
                    {
                        ModelState.AddModelError("PO No.", "Invalid PO No.!");
                    }
                    else
                    {
                        if (order.PoDate > model.AIRDate)
                        {
                            ModelState.AddModelError("AIR Date", "AIR date must be greather than or equal to P.O. date!");
                        }
                        if (order.PoDate > model.InvoiceDate)
                        {
                            ModelState.AddModelError("Invoice Date", "Invoice Date date must be greather than or equal to P.O. date!");
                        }
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await airService.CreateAsync(model, user, date);                    
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
        public async Task<ActionResult> AIRUpdate([DataSourceRequest] DataSourceRequest request, AIR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (await airService.GetAnyAirNoAsync(model.Id, model.AIRNo))
                {
                    ModelState.AddModelError("AIR No", "AIR No. already exists!");
                }
                else
                {
                    var order = await orderService.GetByIdAsync((Guid)model.OrderId);
                    if (order == null)
                    {
                        ModelState.AddModelError("PO No", "Invalid PO No.!");
                    }
                    else
                    {
                        if (order.PoDate > model.AIRDate)
                        {
                            ModelState.AddModelError("AIR Date", "AIR date must be greather than or equal to P.O. date!");
                        }

                        if (order.PoDate > model.InvoiceDate)
                        {
                            ModelState.AddModelError("Invoice Date", "Invoice Date date must be greather than or equal to P.O. date!");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await airService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> AIRDestroy([DataSourceRequest]DataSourceRequest request, AIR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model = await airService.DeleteAsync(model, user, date);                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult _AIRItem(Guid airId)
        {
            ViewData["airId"] = airId;
            return PartialView();
        }

        public async Task<ActionResult> _AIRItemAddEdit(Guid airId, Guid? airItemId)
        {
            var data = await airItemService.GetByIdAsync(airItemId);
            if (data == null)
            {
                data = new AIRItemVM()
                {
                    Id = Guid.NewGuid(),
                    AirId = airId,
                    Mode = "A"
                };
            }
            else
            {
                data.Mode = "E";
            }
            ViewData["airItemId"] = airItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRItemSave(AIRItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await airService.IsPostedAsync((Guid)model.AirId))
                {
                    ModelState.AddModelError("AIR No.", "AIR Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await airItemService.UpdateAsync(model, user, date);                    
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

        public ActionResult _AIRItemRead([DataSourceRequest] DataSourceRequest request, Guid? airId)
        {
            var data = airItemService.GetByAirId(airId);            
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRItemCreate([DataSourceRequest] DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await airItemService.CreateAsync(model, user, date);

                    // TO DO: save to stock card
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
        public async Task<ActionResult> _AIRItemUpdate([DataSourceRequest] DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await airItemService.UpdateAsync(model, user, date);

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
        public async Task<ActionResult> _AIRItemDestroy([DataSourceRequest]DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await airItemService.DeleteAsync(model, user, date);
                    // TO DO: update stocks
                }

            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);


            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> AIRRpt(string airNo)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "report_air");
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
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Air.rpt"));
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

            rpt.SetParameterValue("@cAirNo", airNo);
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostAIRs(Guid airId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await airService.GetByIdAsync(airId) == null)
                {
                    ModelState.AddModelError("AIR", "Invalid AIR Id");
                }
                else if (await airService.IsPostedAsync(airId))
                {
                    ModelState.AddModelError("AIR No.", "AIR Number already Posted, cannot post again!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await airService.PostAsync(airId, user, date);
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
        public async Task<ActionResult> UnpostAIRs(Guid airId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await airService.GetByIdAsync(airId) == null)
                {
                    ModelState.AddModelError("AIR", "Invalid AIR Id");
                }
                else if (!(await airService.IsPostedAsync(airId)))
                {
                    ModelState.AddModelError("AIR No.", "AIR Number not yet posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await airService.UnpostAsync(airId, user, date);
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
    }
}