using CrystalDecisions.CrystalReports.Engine;
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
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class StocksController : Controller
    {
        private AppManEntities _db = new AppManEntities();
        private ICodextnService _codextnService;
        private IDirectoryService _directoryService;
        private IRisIssuedService _risIssuedService;
        private IPsCodeService _psCodeService;

        public StocksController()
        {
            _codextnService = new CodextnService(_db);
            _directoryService = new DirectoryService(_db);
            _risIssuedService = new RisIssuedService(_db);
            _psCodeService = new PsCodeService(_db);
        }

        // GET: Stocks
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, Guid? psId)
        {
            var data = _db.PsStocks.Where(w => w.PsId == psId)
                .AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Create([DataSourceRequest] DataSourceRequest request, PsStockVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var psNo = _db.PsCodes.Find(model.PsId).PsNo;

                    model.Id = Guid.NewGuid();
                    model.InsertedBy = user;
                    model.InsertedDt = date;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;
                    model.StockNo = NextStockNo(psNo);

                    var entity = new PsStock
                    {
                        Id = model.Id,
                        PsId = model.PsId,
                        Fund = model.Fund,
                        StockNo = model.StockNo,
                        StockName = model.StockName,
                        Description = model.Description,
                        Brand = model.Brand,
                        UnitMeas = model.UnitMeas,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };

                    _db.PsStocks.Add(entity);
                    await _db.SaveChangesAsync();
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
        public async Task<ActionResult> Update([DataSourceRequest] DataSourceRequest request, PsStockVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = _db.PsStocks.Find(model.Id);

                    entity.PsId = model.PsId;
                    entity.Fund = model.Fund;
                    entity.StockNo = model.StockNo;
                    entity.StockName = model.StockName;
                    entity.Description = model.Description;
                    entity.Brand = model.Brand;
                    entity.UnitMeas = model.UnitMeas;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    _db.PsStocks.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    await _db.SaveChangesAsync();
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
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, PsStockVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = _db.PsStocks.Find(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    _db.PsStocks.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    //db.PsStocks.Attach(model);
                    // Delete the entity
                    _db.PsStocks.Remove(entity);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await _db.SaveChangesAsync();
                    //db.Configuration.ValidateOnSaveEnabled = true;                
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult StockExtnRead([DataSourceRequest] DataSourceRequest request, string stockNo)
        {

            var data = _db.RisItemExtns.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.StockNo == stockNo)))
                .Select(s => new PsStockExtnVM
                {
                    Id = s.Id,
                    ItemCode = s.ItemNo,
                    ItemKey = s.ItemKey,
                    ItemValue = s.ItemValue
                }).AsQueryable();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, Guid? stockId)
        {
            var data = _db.PsItems.Where(w => w.PsStockId == stockId)
                .Select(s => new PsItemVM
                {
                    Id = s.Id,
                    Office = s.Office,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    UnitMeas = s.UnitMeas,
                    UnitCost = s.UnitCost,
                    QtyPo = s.QtyPo,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    QtyBal = s.QtyBal,
                    Days = s.Days,
                    StockNo = s.PsStock.StockNo,
                    Remarks = s.Remarks
                }).AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PsItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.Id = Guid.NewGuid();
                    model.InsertedBy = user;
                    model.InsertedDt = date;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = new PsItem
                    {
                        Id = model.Id,
                        PsStockId = model.PsStockId,
                        Office = model.Office,
                        RefNo = model.RefNo,
                        RefDate = model.RefDate,
                        RefType = model.RefType,
                        QtyPo = model.QtyPo,
                        Qty = model.Qty,
                        QtyIss = model.QtyIss,
                        QtyBal = model.QtyBal,
                        UnitMeas = model.UnitMeas,
                        UnitCost = model.UnitCost,
                        Remarks = model.Remarks,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };

                    _db.PsItems.Add(entity);
                    await _db.SaveChangesAsync();
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
        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, PsItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = await _db.PsItems.FindAsync(model.Id);
                    //var qtyIss = db.RISlipItems.Where(w => w.StockItemId == entity.Id).Sum(s => s.IssQty) ?? 0;
                    //var qtyBal = model.Qty - qtyIss;                    

                    entity.PsStockId = model.PsStockId;
                    entity.Office = model.Office;
                    entity.RefNo = model.RefNo;
                    entity.RefDate = model.RefDate;
                    entity.RefType = model.RefType;
                    entity.QtyPo = model.QtyPo;
                    entity.Qty = model.Qty;
                    entity.QtyIss = model.QtyIss;
                    entity.QtyBal = model.QtyBal;
                    entity.Days = model.Days;
                    entity.Remarks = model.Remarks;
                    entity.UnitMeas = model.UnitMeas;
                    entity.UnitCost = model.UnitCost;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    _db.PsItems.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    await _db.SaveChangesAsync();
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
        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, PsItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = _db.PsItems.Find(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    _db.PsItems.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    //db.PsStocks.Attach(model);
                    // Delete the entity
                    _db.PsItems.Remove(entity);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await _db.SaveChangesAsync();
                    //db.Configuration.ValidateOnSaveEnabled = true;                
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public string NextStockNo(string psNo)
        {
            string keyName = psNo;

            var data = _db.PsStocks.Where(w => w.PsCode.PsNo == psNo)
                .OrderByDescending(o => o.StockNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "001";
            }
            else
            {
                var sequence = (int.Parse(data.StockNo.Split('-')[1]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(3, '0');
            }
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

        #region ITEMS
        public ActionResult ItemsRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _psCodeService.GetStockItems();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }
        #endregion


        #region  GRID ISSUANCE
        public ActionResult _IssuanceRead([DataSourceRequest] DataSourceRequest request, string stockNo)
        {
            var data = _risIssuedService.GetByStockNo(stockNo);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _IssuanceCreate([DataSourceRequest] DataSourceRequest request, RisIssuedVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risIssuedService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _IssuanceUpdate([DataSourceRequest] DataSourceRequest request, RisIssuedVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risIssuedService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _IssuanceDestroy([DataSourceRequest]DataSourceRequest request, RisIssuedVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "stocks");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _risIssuedService.DeleteAsync(model, user, date);
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

        #region PRINTOUTS
        public ActionResult StockCardRpt(string stockNo)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/StockCard.rpt"));
            rpt.SetDatabaseLogon(un, pw, svr, db_);

            rpt.Load();
            rpt.Refresh();

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
            var imagePath = _codextnService.GetByMastCode("DIRS").Where(w => w.Code == "IMAGE-ITEMS").FirstOrDefault().Description;

            rpt.SetParameterValue("@cStockNo", stockNo);
            rpt.SetParameterValue("ImagePath", imagePath);
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion
    }
}