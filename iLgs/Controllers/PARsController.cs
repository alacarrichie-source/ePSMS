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
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("PARS")]
    public class PARsController : Controller
    {
        private AppManEntities db = new AppManEntities();
        private IParService service;
        private IOrderService orderService;
        private IOrderItemService orderItemService;
        private IRisService risService;
        private ICodextnService codextnService;

        public PARsController()
        {
            this.service = new ParService(db);
            this.orderService = new OrderService(db);
            this.orderItemService = new OrderItemService(db);
            this.risService = new RisService(db);
            this.codextnService = new CodextnService(db);
        }

        // GET: PARs
        public ActionResult Index()
        {
            return View();
        }

        // GET: Issuance for PAR
        public ActionResult PARItem()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = service.GetAll();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, PAR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (await service.GetByParNoAsync(model.ParNo) != null)
                {
                    ModelState.AddModelError("PAR No.", "PAR number already exists!");
                }
                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await service.CreateAsync(model, user, date);
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, PAR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }
                else if (await service.IsPostedAsync(model.Id))
                {
                    ModelState.AddModelError("PAR No.", "PAR Number already Posted, cannot update!");
                }
                else if (await service.IsAnyParNoAsync(model.Id, model.ParNo))
                {
                    ModelState.AddModelError("PAR No.", "PAR number already exists!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await service.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PAR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await service.IsPostedAsync(model.Id))
                {
                    ModelState.AddModelError("DeleteError", "PR Number already Posted, cannot delete!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await service.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult OrdersRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = orderService.GetAllParOrders();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult OrderItemsRead([DataSourceRequest] DataSourceRequest request, Guid? poId)
        {
            var data = orderItemService.GetByPoId(poId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult _PARItem(Guid orderId, Guid orderItemId)
        {
            ViewData["orderd"] = orderId;
            ViewData["orderItemId"] = orderItemId;
            return PartialView();
        }

        public ActionResult _PARItemRead([DataSourceRequest] DataSourceRequest request, Guid? orderItemId)
        {
            var data = service.GetAcknowledgedOrderItems(orderItemId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }
        
        public async Task<ActionResult> _PARItemAddEdit(Guid? orderIdx, Guid? orderItemIdx, Guid? parIdx, Guid? parItemIdx)
        {
            var data = new PARAcknowledgementVM();
            if (parItemIdx == null)
            {
                data.OrderId = orderIdx;
                data.OrderItemId = orderItemIdx;
                data.ParId = Guid.NewGuid();
                data.ParItemId = Guid.NewGuid();
                data.Mode = "A";
            }
            else
            {
                data = await service.GetAcknowledgedOrderItemByItemId(parItemIdx);
                if (data == null)
                {
                    data = new PARAcknowledgementVM()
                    {
                        OrderId = orderIdx,
                        OrderItemId = orderItemIdx,
                        ParId = Guid.NewGuid(),
                        ParItemId = Guid.NewGuid(),
                        Mode = "A"
                    };
                }
                else
                {
                    data.Mode = "E";
                }
            }
            //var data = new PARAcknowledgementVM()
            //{
            //    OrderId = Guid.NewGuid(),
            //    OrderItemId = Guid.NewGuid(),
            //    ParId = Guid.NewGuid(),
            //    ParItemId = Guid.NewGuid(),
            //    Mode = "X"
            //};
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _PARItemSave(PARAcknowledgementVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await service.IsPostedAsync(model.ParId))
                {
                    ModelState.AddModelError("PAR No.", "PAR Number already Posted, cannot update!");
                }
                else if (model.ParDate == null)
                {
                    ModelState.AddModelError("PAR Date", "PAR Date is required, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    var remariningQty = await service.GetRemainingQty(model.OrderItemId, model.ParItemId);
                    if (remariningQty != null)
                    {
                        if (remariningQty < model.Qty)
                        {
                            ModelState.AddModelError("Qty", string.Format("Remaining Qty is only {0}", remariningQty));
                        }
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await service.GetByIdAsync(model.ParId);

                    if (entity == null)
                    {                        
                        model = await service.CreateAcknowledgementAsync(model, user, date);                        
                    }
                    else
                    {
                        model = await service.UpdateAcknowledgementAsync(model, user, date);
                    }                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message.ToString());
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
        public async Task<ActionResult> _PARItemDestroy([DataSourceRequest]DataSourceRequest request, PARAcknowledgementVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await service.IsPostedAsync(model.ParId))
                {
                    ModelState.AddModelError("DeleteError", "PO Number already Posted, cannot delete!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await service.DeleteAcknowledgementAsync(model, user, date);                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message.ToString());
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }


        [AcceptVerbs(HttpVerbs.Get)]
        public async Task<JsonResult> GetAmount(Guid orderItemId, int qty)
        {
            var orderItem = await orderItemService.GetByIdAsync(orderItemId);
            if (orderItem != null)
            {
                return Json(new { Errors = "", Amount = orderItem.UnitCost * qty }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Errors = "Invalid Order Id", Amount = 0 }, JsonRequestBehavior.DenyGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PARRpt(string parNo)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "report_par");
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
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Par.rpt"));
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

            rpt.SetParameterValue("@cParNo", parNo);
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostPAR(Guid parId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await service.GetByIdAsync(parId) == null)
                {
                    ModelState.AddModelError("PAR", "Invalid PAR Id");
                }
                else if (await service.IsPostedAsync(parId))
                {
                    ModelState.AddModelError("PAR No.", "PAR Number already Posted, cannot post again!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await service.PostAsync(parId, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnpostPAR(Guid parId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pars");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await service.GetByIdAsync(parId) == null)
                {
                    ModelState.AddModelError("PAR", "Invalid PAR Id");
                }
                else if (!(await service.IsPostedAsync(parId)))
                {
                    ModelState.AddModelError("PAR No.", "PAR Number not yet posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await service.UnpostAsync(parId, user, date);
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
    }
}