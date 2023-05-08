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

namespace iLgs.Controllers
{
    [AppAuthorize("ITEMS")]
    public class ItemsController : Controller
    {
        private AppManEntities db = new AppManEntities();
        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.PsCodes.AsQueryable();            
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> ItemCreate([DataSourceRequest] DataSourceRequest request, PsCode model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
                    model.ItemDescription = model.ItemDescription ?? "";
                    model.ReorderPoint = model.ReorderPoint ?? 0;
                    model.DaysToConsume = model.DaysToConsume ?? 0;
                   
                    db.PsCodes.Add(model);
                    await db.SaveChangesAsync();                    
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
        public async Task<ActionResult> ItemUpdate([DataSourceRequest] DataSourceRequest request, PsCode model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "items");
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
                    model.UnitMeas = model.UnitMeas;

                    model.PsType = model.PsType.ToUpper();
                    model.ItemDescription = model.ItemDescription ?? "";
                    model.ReorderPoint = model.ReorderPoint ?? 0;
                    model.DaysToConsume = model.DaysToConsume ?? 0;
                    
                    db.PsCodes.Attach(model);
                    db.Entry(model).State = EntityState.Modified;
                    await db.SaveChangesAsync();
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
        public async Task<ActionResult> ItemDestroy([DataSourceRequest]DataSourceRequest request, PsCode model)
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

                    var entity = db.PsCodes.Find(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.PsCodes.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    //db.PsCodes.Attach(model);
                    // Delete the entity
                    db.PsCodes.Remove(entity);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await db.SaveChangesAsync();
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

        public ActionResult Maintenance()
        {
            return View();
        }

        public ActionResult ItemMainRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.PsCodes
                .Select(s => new PsCodeVM { 
                    Id = s.Id,
                    PsType = s.PsType,
                    PsNo = s.PsNo,
                    ItemName = s.ItemName,
                    UnitMeas = s.UnitMeas,                   
                    AdditionalInfo = db.Codextns.Where(w => w.CodeMast.Code == "PS_CODES" && w.Code == s.PsType).FirstOrDefault().Desc3
                }).AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult StockRead([DataSourceRequest] DataSourceRequest request, Guid? psId)
        {
            var data = db.PsStocks.Where(w => w.PsId == psId)
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
        public async Task<ActionResult> StockCreate([DataSourceRequest] DataSourceRequest request, PsStockVM model)
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

                    var psNo = db.PsCodes.Find(model.PsId).PsNo;

                    model.Id = Guid.NewGuid();
                    model.InsertedBy = user;
                    model.InsertedDt = date;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;
                    model.StockNo = NextStockNo(psNo);

                    var entity = new PsStock { 
                        Id = model.Id,
                        PsId = model.PsId,
                        StockNo = model.StockNo,
                        Description = model.Description,
                        Type = model.Type,
                        IdNo = model.IdNo,
                        Location = model.Location,
                        Area = model.Area,
                        TctNo = model.TctNo,
                        BrandName = model.BrandName,
                        Department = model.Department,
                        OtherSpecs = model.OtherSpecs,
                        SerialNo = model.SerialNo,
                        Color = model.Color,
                        EngineNo = model.EngineNo,
                        ChassisNo = model.ChassisNo,
                        ParNo = model.ParNo,
                        AccountableOfficer = model.AccountableOfficer,
                        CompletionDate = model.CompletionDate,
                        EstimatedLife = model.EstimatedLife,
                        Amount = model.Amount,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };

                    db.PsStocks.Add(entity);
                    await db.SaveChangesAsync();
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
        public async Task<ActionResult> StockUpdate([DataSourceRequest] DataSourceRequest request, PsStockVM model)
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

                    var entity = db.PsStocks.Find(model.Id);

                    entity.PsId = model.PsId;
                    entity.StockNo = model.StockNo;
                    entity.Description = model.Description;
                    entity.Type = model.Type;
                    entity.IdNo = model.IdNo;
                    entity.Location = model.Location;
                    entity.Area = model.Area;
                    entity.TctNo = model.TctNo;
                    entity.BrandName = model.BrandName;
                    entity.Department = model.Department;
                    entity.OtherSpecs = model.OtherSpecs;
                    entity.SerialNo = model.SerialNo;
                    entity.Color = model.Color;
                    entity.EngineNo = model.EngineNo;
                    entity.ChassisNo = model.ChassisNo;
                    entity.ParNo = model.ParNo;
                    entity.AccountableOfficer = model.AccountableOfficer;
                    entity.CompletionDate = model.CompletionDate;
                    entity.EstimatedLife = model.EstimatedLife;
                    entity.Amount = model.Amount;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;
                    
                    db.PsStocks.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();
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
        public async Task<ActionResult> StockDestroy([DataSourceRequest]DataSourceRequest request, PsStockVM model)
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

                    var entity = db.PsStocks.Find(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.PsStocks.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    //db.PsStocks.Attach(model);
                    // Delete the entity
                    db.PsStocks.Remove(entity);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await db.SaveChangesAsync();
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
            
            var data = db.PsStocks.Where(w => w.PsCode.PsNo == psNo)
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

        public ActionResult StockItemRead([DataSourceRequest] DataSourceRequest request, Guid? stockId)
        {
            var data = db.PsItems.Where(w => w.PsStockId == stockId)
                .Select(s => new PsItemVM { 
                    Id = s.Id,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    RefType = s.RefType,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    QtyBal = s.QtyBal,
                    Days = s.Days,
                    UnitCost = s.OrderItem.UnitCost
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
        public async Task<ActionResult> StockItemCreate([DataSourceRequest] DataSourceRequest request, PsItemVM model)
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
                        OrderItemId = model.OrderItemId,
                        RefNo = model.RefNo,
                        RefDate = model.RefDate,
                        RefType = model.RefType,
                        Qty = model.Qty,                        
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };

                    db.PsItems.Add(entity);
                    await db.SaveChangesAsync();
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
        public async Task<ActionResult> StockItemUpdate([DataSourceRequest] DataSourceRequest request, PsItemVM model)
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

                    var entity = await db.PsItems.FindAsync(model.Id);
                    var qtyIss = db.RISlipItems.Where(w => w.StockItemId == entity.Id).Sum(s => s.IssQty) ?? 0;
                    var qtyBal = model.Qty - qtyIss;

                    entity.PsStockId = model.PsStockId;
                    entity.OrderItemId = model.OrderItemId;
                    entity.RefNo = model.RefNo;
                    entity.RefDate = model.RefDate;
                    entity.RefType = model.RefType;
                    entity.Qty = model.Qty;
                    entity.QtyIss = qtyIss;
                    entity.QtyBal = qtyBal;
                    entity.Days = model.Days;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    db.PsItems.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();
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
        public async Task<ActionResult> StockItemDestroy([DataSourceRequest]DataSourceRequest request, PsItemVM model)
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

                    var entity = db.PsItems.Find(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.PsItems.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    //db.PsStocks.Attach(model);
                    // Delete the entity
                    db.PsItems.Remove(entity);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await db.SaveChangesAsync();
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

        public ActionResult _QueryOrderItems(Guid psId, Guid stockId)
        {
            ViewData["psId"] = psId;
            var model = new QueryOrderItemsVM()
            {
                PsStockId = stockId
            };
            return PartialView(model);
        }

        public async Task<ActionResult> _QueryOrderItemRead([DataSourceRequest] DataSourceRequest request, Guid psId)
        {

            var data = db.OrderItems.Where(w => w.RequestItem.PsCode.Id == psId && !w.PsItems.Any(a => a.OrderItemId == w.Id))
                .Select(s => new QueryOrderItemsVM
                {
                    Id = s.Id,
                    PsNo = s.RequestItem.PsCode.PsNo,
                    ItemName = s.RequestItem.PsCode.ItemName,
                    PoDate = s.Order.PoDate,
                    PoNo = s.Order.PoNo,
                    Qty = s.Qty
                });

            var result = new JsonNetResult
            {
                Data = await data.ToDataSourceResultAsync(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _QueryOrderItemSelectionSave([DataSourceRequest] DataSourceRequest request, QueryOrderItemsVM searchModel)
        {
            try
            {
                var gridIdList = searchModel.SelectedIds.Split(',');
                foreach (var gridId in gridIdList)
                {
                    var orderItemId = Guid.Parse(gridId);
                    var data = await db.OrderItems.Where(w => w.Id == orderItemId)
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
                        OrderItemId = orderItemId,
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

                    db.PsItems.Add(entity);
                    await db.SaveChangesAsync();
                }
                
                if (Request.IsAjaxRequest())
                {
                    var query = from state in ModelState.Values
                                from error in state.Errors
                                select error.ErrorMessage;

                    var errorList = query.ToList();
                    if (errorList.Count() > 0)
                    {
                        return Json(new {Errors = errorList }, JsonRequestBehavior.DenyGet);
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
    }
}