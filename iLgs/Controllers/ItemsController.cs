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
using System.Data.SqlClient;
using CrystalDecisions.CrystalReports.Engine;
using System.IO;
using iLgs.Services.Interfaces;
using iLgs.Services;
using System.Configuration;
using CrystalDecisions.Shared;

namespace iLgs.Controllers
{
    [AppAuthorize("ITEMS")]
    public class ItemsController : Controller
    {
        private AppManEntities _db = new AppManEntities();
        private IItemTypeService _itemTypeService;
        private IItemCodeService _itemCodeService;
        private IItemFieldService _itemFieldService;
        private IPsCodeService _psCodeService;
        private ICodextnService _codextnService;
        private IDirectoryService _directoryService;
        private IRisIssuedService _risIssuedService;

        public ItemsController()
        {
            _itemTypeService = new ItemTypeService(_db);
            _itemCodeService = new ItemCodeService(_db);
            _itemFieldService = new ItemFieldService(_db);
            _psCodeService = new PsCodeService(_db);
            _codextnService = new CodextnService(_db);
            _directoryService = new DirectoryService(_db);
            _risIssuedService = new RisIssuedService(_db);
        }

        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }
        

        public ActionResult PsCode()
        {
            return View();
        }

        public ActionResult PsCodeRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _psCodeService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PsCodeCreate([DataSourceRequest] DataSourceRequest request, PsCodeVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pscodes");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCodeService.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PsCodeUpdate([DataSourceRequest] DataSourceRequest request, PsCodeVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pscodes");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCodeService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PsCodeDestroy([DataSourceRequest]DataSourceRequest request, PsCodeVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "pscodes");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _psCodeService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
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
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("AddError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("AddError", e.Message);
                }
            }
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, ItemTypeVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, ItemTypeVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
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
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("AddError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("AddError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCodeUpdate([DataSourceRequest] DataSourceRequest request, ItemCodeVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCodeDestroy([DataSourceRequest]DataSourceRequest request, ItemCodeVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }
            }        
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion

        #region ITEM FIELDS
        public ActionResult _ItemFields(Guid itemTypeId)
        {
            ViewData["itemTypeId"] = itemTypeId;
            return PartialView();
        }

        public ActionResult ItemFieldRead([DataSourceRequest] DataSourceRequest request, Guid? itemTypeId)
        {
            var data = _itemFieldService.GetAllbyItemTypeId(itemTypeId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemFieldCreate([DataSourceRequest] DataSourceRequest request, ItemFieldVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemFieldService.CreateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("AddError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("AddError", e.Message);
                }
            }        
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemFieldUpdate([DataSourceRequest] DataSourceRequest request, ItemFieldVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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

                    model = await _itemFieldService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("UpdateError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("UpdateError", e.Message);
                }
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemFieldDestroy([DataSourceRequest]DataSourceRequest request, ItemFieldVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _itemFieldService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "ServiceException")
                {
                    ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                         "please contact tech support with this message: " + e.Message);
                }
                else
                {
                    ModelState.AddModelError("DeleteError", e.Message);
                }            
            }
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }
        #endregion          

        public string GetImageDir()
        {
            return _directoryService.GetItemImageDirectory();
        }

        public ActionResult ItemMainRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _psCodeService.GetAll();
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


        //public async Task<ActionResult> _QueryOrderItemRead([DataSourceRequest] DataSourceRequest request, Guid psId)
        //{

        //    var data = db.OrderItems.Where(w => w.RequestItem.RisItem.PsCode.Id == psId && !w.PsItems.Any(a => a.OrderItemId == w.Id))
        //        .Select(s => new QueryOrderItemsVM
        //        {
        //            Id = s.Id,
        //            PsNo = s.RequestItem.RisItem.PsCode.PsNo,
        //            ItemName = s.RequestItem.RisItem.PsCode.ItemName,
        //            PoDate = s.Order.PoDate,
        //            PoNo = s.Order.PoNo,
        //            Qty = s.Qty
        //        });

        //    var result = new JsonNetResult
        //    {
        //        Data = await data.ToDataSourceResultAsync(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };
        //    return result;
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _QueryOrderItemSelectionSave([DataSourceRequest] DataSourceRequest request, QueryOrderItemsVM searchModel)
        {
            try
            {
                var gridIdList = searchModel.SelectedIds.Split(',');
                foreach (var gridId in gridIdList)
                {
                    var orderItemId = Guid.Parse(gridId);
                    var data = await _db.OrderItems.Where(w => w.Id == orderItemId)
                        .Select(s => new QueryOrderItemsVM
                        {
                            PoDate = s.Order.PoDate,
                            PoNo = s.Order.PoNo,
                            Qty = s.Qty
                        }).FirstOrDefaultAsync();

                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = new PsItem
                    {
                        Id = Guid.NewGuid(),
                        PsStockId = searchModel.PsStockId,
                        RefNo = data.PoNo,
                        RefDate = data.PoDate,
                        RefType = "",
                        Qty = data.Qty,
                        QtyIss = 0,
                        QtyBal = data.Qty,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    _db.PsItems.Add(entity);
                    await _db.SaveChangesAsync();
                }

                if (Request.IsAjaxRequest())
                {
                    var query = from state in ModelState.Values
                                from error in state.Errors
                                select error.ErrorMessage;

                    var errorList = query.ToList();
                    if (errorList.Count() > 0)
                    {
                        return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
                    }
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        #region ISSUED
        public ActionResult _RISIssuedRead([DataSourceRequest] DataSourceRequest request, string poNo, string stockNo)
        {
            var data = _risIssuedService.GetByPoNoStockNo(poNo, stockNo);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }
        #endregion

        

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

        #region HELPERS
        [Authorize]
        public JsonResult GetItems(string text)
        {

            //var model = db.ItemCodes.AsQueryable();
            var model = _db.Database.SqlQuery<ItemCodeVM>("Exec ItemCodes_GetItems {0}", text).AsQueryable();

            //if (!string.IsNullOrEmpty(text))
            //{
            //    model = model.Where(p => p.ItemSw == "Y" && (p.Description.Contains(text) || p.Code.Contains(text)));
            //}

            return Json(model.Select(c => new { Id = c.Id, Code = c.Code, Description = c.Description, Type = c.ItemType, ItemNo = c.ItemNo, MainDesc = c.MainDesc }), JsonRequestBehavior.AllowGet);
        }


        public JsonResult GetPsNo(string itemCode, string itemName)
        {

            var psNo = _psCodeService.PsNo(itemCode, itemName);
            return Json(new { Errors = "", PsNo = psNo}, JsonRequestBehavior.AllowGet);
        }
        #endregion

        public async Task<ActionResult> ItemCodeRpt(Guid itemTypeId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/ItemCode.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
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

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cItemTypeId", itemTypeId.ToString());
            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("USER", user);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
    }
}