//using CrystalDecisions.CrystalReports.Engine;
//using iLgs.Models;
//using iLgs.Services;
//using iLgs.Services.Interfaces;
//using iLgs.Utilities;
//using Kendo.Mvc.Extensions;
//using Kendo.Mvc.UI;
//using Microsoft.AspNet.Identity;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Data.SqlClient;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;

//namespace iLgs.Controllers
//{
//    public class PropertiesController : Controller
//    {
//        private AppManEntities _db = new AppManEntities();
//        private ICodextnService _codextnService;
//        private IDirectoryService _directoryService;
//        private IRisIssuedService _risIssuedService;
//        private IPropertyService _propertyService;
//        private IPropertyItemService _propertyItemService;
//        private IPsCodeService _psCodeService;

//        public PropertiesController()
//        {
//            _codextnService = new CodextnService(_db);
//            _directoryService = new DirectoryService(_db);
//            _risIssuedService = new RisIssuedService(_db);
//            _propertyService = new PropertyService(_db);
//            _propertyItemService = new PropertyItemService(_db);
//            _psCodeService = new PsCodeService(_db);
//        }

//        #region PROPERTIES
//        // GET: Properties
//        public ActionResult Index()
//        {
//            return View();
//        }

//        public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid? psId)
//        {
//            var data = _propertyService.GetAllByPsId(psId);                
//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };

//            return result;
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, PropertyVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "properties");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("", "Add Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _propertyService.CreateAsync(model, user, date);                    
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, PropertyVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "properties");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("", "Update Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _propertyService.UpdateAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PropertyVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "properties");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }
//                else
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _propertyService.DeleteAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }
//        #endregion

//        public ActionResult StockExtnRead([DataSourceRequest] DataSourceRequest request, string stockNo)
//        {

//            var data = _db.RisItemExtns.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.StockNo == stockNo)))
//                .Select(s => new PsStockExtnVM
//                {
//                    Id = s.Id,
//                    ItemCode = s.ItemNo,
//                    ItemKey = s.ItemKey,
//                    ItemValue = s.ItemValue
//                }).AsQueryable();

//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };

//            return result;
//        }

//        #region PROPERTY ITEMS
//        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, Guid? psStockId)
//        {
//            var data = _propertyItemService.GetByPsStockId(psStockId);

//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };

//            return result;
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PropertyItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "stocks");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("", "Add Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _propertyItemService.CreateAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, PropertyItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "properties");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("", "Update Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _propertyItemService.UpdateAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, PropertyItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "properties");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }
//                else
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    await _propertyItemService.DeleteAsync(model, user, date);
//                }
//            }
//            catch (Exception e)
//            {
//                if (e.GetType().Name == "ServiceException")
//                {
//                    ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                         "please contact tech support with this message: " + e.Message);
//                }
//                else
//                {
//                    ModelState.AddModelError("", e.Message);
//                }
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }
//        #endregion

//        public string NextStockNo(string psNo)
//        {
//            string keyName = psNo;

//            var data = _db.PsStocks.Where(w => w.PsCode.PsNo == psNo)
//                .OrderByDescending(o => o.StockNo).FirstOrDefault();
//            if (data == null)
//            {
//                return keyName + "-" + "001";
//            }
//            else
//            {
//                var sequence = (int.Parse(data.StockNo.Split('-')[1]) + 1).ToString();
//                return keyName + "-" + sequence.PadLeft(3, '0');
//            }
//        }

//        public FileResult GetProductImage(Guid imageId)
//        {
//            var upload = _db.Uploads.Where(w => w.ImageId == imageId).FirstOrDefault();
//            if (upload != null)
//            {
//                string networkImagePath = _directoryService.GetItemImageDirectory() + upload.FileName;
//                byte[] imageBytes = System.IO.File.ReadAllBytes(networkImagePath);
//                return File(imageBytes, "image/jpeg");
//            }
//            return null;
//        }

//        #region ITEMS
//        public ActionResult ItemsRead([DataSourceRequest] DataSourceRequest request)
//        {
//            var data = _psCodeService.GetPropertyItems();
//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };

//            return result;
//        }
//        #endregion


//        #region  GRID ISSUANCE
//        public ActionResult _IssuanceRead([DataSourceRequest] DataSourceRequest request, string stockNo)
//        {
//            var data = _risIssuedService.GetByStockNo(stockNo);

//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };
//            return result;
//        }
//        #endregion  

//        #region PRINTOUTS
//        public ActionResult StockCardRpt(string stockNo)
//        {
//            string stringname = _db.Database.Connection.ConnectionString.ToString();
//            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

//            string un = decoder.UserID;
//            string pw = decoder.Password;
//            string svr = decoder.DataSource;
//            string db_ = decoder.InitialCatalog;

//            ReportClass rpt = new ReportClass();
//            rpt.FileName = Server.MapPath(Url.Content("~/Reports/StockCard.rpt"));
//            rpt.SetDatabaseLogon(un, pw, svr, db_);

//            rpt.Load();
//            rpt.Refresh();

//            foreach (Table table in rpt.Database.Tables)
//            {
//                var logonInfo = table.LogOnInfo;
//                logonInfo.ConnectionInfo.ServerName = svr;
//                logonInfo.ConnectionInfo.DatabaseName = db_;
//                logonInfo.ConnectionInfo.UserID = un;
//                logonInfo.ConnectionInfo.Password = pw;
//                table.ApplyLogOnInfo(logonInfo);
//            }

//            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;
//            var imagePath = _codextnService.GetByMastCode("DIRS").Where(w => w.Code == "IMAGE-ITEMS").FirstOrDefault().Description;

//            rpt.SetParameterValue("@cStockNo", stockNo);
//            rpt.SetParameterValue("ImagePath", imagePath);
//            rpt.SetParameterValue("LGU", lgu);

//            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
//            rpt.Close();
//            rpt.Dispose();
//            return File(stream, "application/pdf");
//        }
//        #endregion
//    }
//}