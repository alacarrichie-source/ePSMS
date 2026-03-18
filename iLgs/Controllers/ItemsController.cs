using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Codes;
using iLgs.Services.Items;
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
    [AppAuthorize("ITEMS")]
    public class ItemsController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly IItemTypeService _itemTypeService;
        private readonly IItemCodeService _itemCodeService;
        private readonly ICodextnService _codextnService;
        private readonly IDirectoryService _directoryService;

        public ItemsController()
        {
            //_db = new AppManEntities();
            _itemTypeService = new ItemTypeService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _codextnService = new CodextnService(_db);
            _directoryService = new DirectoryService(_db);
        }

        //public ItemsController(AppManEntities db, IItemTypeService itemTypeService, 
        //    IItemCodeService itemCodeService, ICodextnService codextnService, IDirectoryService directoryService)
        //{
        //    _db = db;
        //    _itemTypeService = itemTypeService;
        //    _itemCodeService = itemCodeService;
        //    _codextnService = codextnService;
        //    _directoryService = directoryService;        
        //}

        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _itemTypeService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, ItemTypeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemTypeService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, ItemTypeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemTypeService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, ItemTypeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemTypeService.DeleteAsync(model, user, date);
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

        #region ITEM CODES

        public ActionResult _ItemCodes(Guid itemTypeId)
        {
            ViewData["itemTypeId"] = itemTypeId;
            return PartialView();
        }

        public ActionResult ItemCodeRead([DataSourceRequest] DataSourceRequest request, Guid? itemTypeId)
        {
            var data = _itemCodeService.GetAllByItemTypeId(itemTypeId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCodeCreate([DataSourceRequest] DataSourceRequest request, ItemCodeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemCodeService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> ItemCodeUpdate([DataSourceRequest] DataSourceRequest request, ItemCodeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    model = await _itemCodeService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> ItemCodeDestroy([DataSourceRequest]DataSourceRequest request, ItemCodeVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemCodeService.DeleteAsync(model, user, date);
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

        //#region ITEM FIELDS
        //public ActionResult _ItemFields(Guid itemTypeId)
        //{
        //    ViewData["itemTypeId"] = itemTypeId;
        //    return PartialView();
        //}

        //public ActionResult ItemFieldRead([DataSourceRequest] DataSourceRequest request, Guid? itemTypeId)
        //{
        //    var data = _itemFieldService.GetAllbyItemTypeId(itemTypeId);
        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };

        //    return result;
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> ItemFieldCreate([DataSourceRequest] DataSourceRequest request, ItemFieldVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
        //        Access access = await accessTask;
        //        if (!access.AllowAdd)
        //        {
        //            ModelState.AddModelError("AddError", "Add Access Denied!");
        //        }

        //        if (model != null && ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _itemFieldService.CreateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError("AddError", error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("AddError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("AddError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> ItemFieldUpdate([DataSourceRequest] DataSourceRequest request, ItemFieldVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("UpdateError", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model.UpdatedBy = user;
        //            model.UpdatedDt = date;

        //            model = await _itemFieldService.UpdateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError("UpdateError", error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> ItemFieldDestroy([DataSourceRequest]DataSourceRequest request, ItemFieldVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
        //        Access access = await accessTask;
        //        if (!access.AllowDelete)
        //        {
        //            ModelState.AddModelError("DeleteError", "Delete Access Denied!");
        //        }
        //        else
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _itemFieldService.DeleteAsync(model, user, date);
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
        //#endregion

        #region ITEM PREVIEW    
        public ActionResult ItemCodePreview()
        {
            return View();
        }

        public async Task<ActionResult> ItemCodePreviewRead([DataSourceRequest] DataSourceRequest request, string category)
        {
            string userId = User.Identity.GetUserId();
            var admin = await GetUserInRole(userId, "admin");
            var sysadmin = await GetUserInRole(userId, sysAdmin);

            IQueryable<ItemCodePreviewVM> data = null;
            if (admin || sysadmin)
            {
                data = _itemCodeService.GetItemCodePreview(category);
            }
            else
            {
                data = _itemCodeService.GetItemCodePreviewByUser(category, userId);
            }

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }
        #endregion

        public string GetImageDir()
        {
            return _directoryService.GetItemImageDirectory();
        }


        [Authorize]
        public ActionResult GetItemByCategoryRead([DataSourceRequest] DataSourceRequest request, string category)
        {
            var data = _itemCodeService.GetItemsByCategory(category, "");
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult _QueryOrderItems(Guid psId, Guid stockId)
        {
            ViewData["psId"] = psId;
            var model = new QueryOrderItemsVM()
            {
                PsStockId = stockId
            };
            return PartialView(model);
        }

        public FileResult GetProductImage(Guid imageId)
        {
            var upload = _db.Uploads.Where(w => w.ImageId == imageId).FirstOrDefault();
            if (upload != null)
            {
                string networkImagePath = _directoryService.GetItemImageDirectory() + upload.FileName;
                byte[] imageBytes = System.IO.File.ReadAllBytes(networkImagePath);
                return File(imageBytes, "image/jpeg");
            }
            return null;
        }

        public ActionResult _PrintItem(Guid? itemTypeId)
        {
            var data = new ItemCodePrintVM()
            {
                CategoryId = itemTypeId,
                SavePrints = false
            };

            return PartialView(data);
        }

        public ActionResult TestDb()
        {
            try
            {
                var name = _db.Database.Connection.Database;
                string user = ControllerContext.HttpContext.User.Identity.Name;
                string conString = _db.Database.Connection.ConnectionString.ToString();
                SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);

                string un = decoder.UserID;
                string pw = decoder.Password;
                string svr = decoder.DataSource;
                string db_ = decoder.InitialCatalog;
                
                return Content($"Database Connected: Name = {name}, un = {un}, pw = {pw}, svr = {svr}, db = {db_}, integrated security = {decoder.IntegratedSecurity}" );
            }
            catch (Exception ex)
            {
                return Content("DB Error: " + ex.Message);
            }
        }


        public ActionResult ItemCodeRpt(ItemCodePrintVM model)
        {
            //try
            //{
            //    Task<Access> accessTask = Access(User.Identity.GetUserId(), "items");
            //    Access access = await accessTask;
            //    if (access == null)
            //    {
            //        throw new Exception("Access Denied!");
            //    }

            //}
            //catch (Exception e)
            //{
            //    ViewBag.Error = e.Message;
            //    return View("Error");
            //}

            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/ItemCode.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            //string pw = decoder.Password;
            string pw = rptKey;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            //string un = "sa_psms_lpc";
            //string pw = "grace2023@lpc";
            //string svr = "dataserver3";
            //string db_ = "ePSMS";

            crDatabase = rpt.Database;
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

            //string user = ControllerContext.HttpContext.User.Identity.Name;

            //var rpt = new ReportDocument();
            //rpt.Load(Server.MapPath("~/Reports/ItemCode.rpt"));
            //rpt.Refresh();

            //// get full EF connection string
            //string efConString = _db.Database.Connection.ConnectionString;

            //// extract provider connection string if EF uses metadata
            //string realConString = efConString;
            //if (efConString.Contains("provider connection string"))
            //{
            //    int start = efConString.IndexOf("provider connection string=\"") + "provider connection string=\"".Length;
            //    int end = efConString.LastIndexOf("\"");
            //    realConString = efConString.Substring(start, end - start);
            //}

            //var decoder = new SqlConnectionStringBuilder(realConString);

            //ConnectionInfo connectionInfo = new ConnectionInfo
            //{
            //    ServerName = "Ws2016",
            //    DatabaseName = "ePSMS",
            //    UserID = "sa_psms_lpc",
            //    Password = "grace2023@lpc",
            //    IntegratedSecurity = false
            //};

            //foreach (Table table in rpt.Database.Tables)
            //{
            //    TableLogOnInfo logonInfo = table.LogOnInfo;
            //    logonInfo.ConnectionInfo = connectionInfo;
            //    table.ApplyLogOnInfo(logonInfo);
            //    table.Location = table.Location; // keep table alias
            //}

            //foreach (ReportDocument subReport in report.Subreports)
            //{
            //    foreach (Table subTable in subReport.Database.Tables)
            //    {
            //        TableLogOnInfo logonInfo = subTable.LogOnInfo;
            //        logonInfo.ConnectionInfo = connectionInfo;
            //        subTable.ApplyLogOnInfo(logonInfo);
            //    }
            //}

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;
            var itemTypeId = model.CategoryId == Guid.Empty ? null : model.CategoryId.ToString();

            rpt.SetParameterValue("@cItemTypeId", itemTypeId);
            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("USER", user);

            if (model.SavePrints)
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.Excel);
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/xlsx", $"ItemCodeRpt.xls");
            }
            else
            {
                Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                rpt.Close();
                rpt.Dispose();
                return File(stream, "application/pdf");
            }            
        }

        private void ApplyConnectionInfo(ReportDocument report, ConnectionInfo connectionInfo)
        {
            foreach (Table table in report.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo = connectionInfo;
                table.ApplyLogOnInfo(logonInfo);
                table.Location = table.Location;
            }
        }

        public async Task<ActionResult> GetPartialView(Guid? id)
        {            
            return Content("Partial " + (await _itemCodeService.GetPartialViewAsync(id)));            
        }
    }
}