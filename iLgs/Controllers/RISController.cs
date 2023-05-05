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
    [AppAuthorize("RIS")]
    public class RISController : Controller
    {
        private AppManEntities db = new AppManEntities();
        // GET: 
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RISRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.RISlips
                .Select(s => new RISlipVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    PoNo = s.Order.PoNo,
                    PoDate = s.Order.PoDate,
                    Fund = s.Fund,
                    Division = s.Division,
                    Office = s.Office,
                    FPP = s.FPP,
                    RisNo = s.RisNo,
                    RisDate = s.RisDate,
                    Purpose = s.Purpose,
                    RequestedBy = s.RequestedBy,
                    RequestedDate = s.RequestedDate,
                    RequestedByDesignation = s.RequestedByDesignation,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedDate = s.ApprovedDate,
                    ApprovedByDesignation = s.ApprovedByDesignation,
                    IssuedBy = s.IssuedBy,
                    IssuedDate = s.IssuedDate,
                    IssuedByDesignation = s.IssuedByDesignation,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedDate = s.ReceivedDate,
                    ReceivedByDesignation = s.ReceivedByDesignation                    
                })
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
        public async Task<ActionResult> RISCreate([DataSourceRequest] DataSourceRequest request, RISlipVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (db.RISlips.Any(a => a.RisNo == model.RisNo))
                {
                    ModelState.AddModelError("RisNo", "RIS No. already exists!");
                }

                var order = await db.Orders.Include(i => i.Request).Where(w => w.Id == model.OrderId).FirstOrDefaultAsync();
                if (order == null)
                {
                    ModelState.AddModelError("PoNo", "Invalid PO No.!");
                }
                else
                {
                    model.Division = order.Request.Section;
                    model.Office = order.Request.Department;
                    model.Fund = order.Request.Fund;
                    model.FPP = order.Request.FPP;
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;                    

                    model.Id = Guid.NewGuid();
                    if (string.IsNullOrWhiteSpace(model.RisNo))
                    {
                        model.RisNo = NextRisNo((DateTime)model.RisDate);
                    }
                    model.InsertedBy = user;
                    model.InsertedDt = date;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = new RISlip()
                    {
                        Id = model.Id,
                        OrderId = model.OrderId,
                        Fund = model.Fund,
                        Division = model.Division,
                        Office = model.Office,
                        FPP = model.FPP,
                        RisNo = model.RisNo,
                        RisDate = model.RisDate,
                        Purpose = model.Purpose,
                        RequestedBy = model.RequestedBy,
                        RequestedDate = model.RequestedDate,
                        RequestedByDesignation = model.RequestedByDesignation,
                        ApprovedBy = model.ApprovedBy,
                        ApprovedDate = model.ApprovedDate,
                        ApprovedByDesignation = model.ApprovedByDesignation,
                        IssuedBy = model.IssuedBy,
                        IssuedDate = model.IssuedDate,
                        IssuedByDesignation = model.IssuedByDesignation,
                        ReceivedBy = model.ReceivedBy,
                        ReceivedDate = model.ReceivedDate,
                        ReceivedByDesignation = model.ReceivedByDesignation,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };


                    // include items with stocks during add
                    var stockItems = db.PsItems.Where(w => w.OrderItem.OrderId == model.OrderId).ToList();
                    foreach (var stockItem in stockItems)
                    {
                        RISlipItem item = new RISlipItem()
                        {
                            Id = Guid.NewGuid(),
                            RisId = entity.Id,
                            StockItemId = stockItem.Id,
                            ReqQty = stockItem.Qty,
                            IssQty = stockItem.Qty,
                            IssRemarks = "",                            
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        entity.RISlipItems.Add(item);
                    }

                    db.RISlips.Add(entity);
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
        public async Task<ActionResult> RISUpdate([DataSourceRequest] DataSourceRequest request, RISlipVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (db.RISlips.Any(a => a.Id != model.Id && a.RisNo == model.RisNo))
                {
                    ModelState.AddModelError("RisNo", "RIS No. already exists!");
                }

                var order = await db.Orders.Include(i => i.Request).Where(w => w.Id == model.OrderId).FirstOrDefaultAsync();
                if (order == null)
                {
                    ModelState.AddModelError("PoNo", "Invalid PO No.!");
                }
                else
                {
                    model.Division = order.Request.Section;
                    model.Office = order.Request.Department;
                    model.Fund = order.Request.Fund;
                    model.FPP = order.Request.FPP;


                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;
                    
                    var entity = await db.RISlips.FindAsync(model.Id);

                    // if there's changes in orderId: delete the previous then add the current
                    if (entity.OrderId != model.OrderId)
                    {
                        // delete previous
                        var slipItems = db.RISlipItems.Where(w => w.RisId == model.Id);
                        db.RISlipItems.RemoveRange(slipItems);                        

                        // add current
                        var stockItems = db.PsItems.Where(w => w.OrderItem.OrderId == model.OrderId).ToList();
                        foreach (var stockItem in stockItems)
                        {
                            RISlipItem item = new RISlipItem()
                            {
                                Id = Guid.NewGuid(),
                                RisId = entity.Id,
                                StockItemId = stockItem.Id,
                                ReqQty = stockItem.Qty,
                                IssQty = stockItem.Qty,
                                IssRemarks = "",
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            entity.RISlipItems.Add(item);
                        }
                    }

                    entity.OrderId = model.OrderId;
                    entity.Fund = model.Fund;
                    entity.Division = model.Division;
                    entity.Office = model.Office;
                    entity.FPP = model.FPP;
                    entity.RisNo = model.RisNo;
                    entity.RisDate = model.RisDate;
                    entity.Purpose = model.Purpose;
                    entity.RequestedBy = model.RequestedBy;
                    entity.RequestedDate = model.RequestedDate;
                    entity.RequestedByDesignation = model.RequestedByDesignation;
                    entity.ApprovedBy = model.ApprovedBy;
                    entity.ApprovedDate = model.ApprovedDate;
                    entity.ApprovedByDesignation = model.ApprovedByDesignation;
                    entity.IssuedBy = model.IssuedBy;
                    entity.IssuedDate = model.IssuedDate;
                    entity.IssuedByDesignation = model.IssuedByDesignation;
                    entity.ReceivedBy = model.ReceivedBy;
                    entity.ReceivedDate = model.ReceivedDate;
                    entity.ReceivedByDesignation = model.ReceivedByDesignation;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;                    

                    db.RISlips.Attach(entity);
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
        public async Task<ActionResult> RISDestroy([DataSourceRequest]DataSourceRequest request, RISlipVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = await db.RISlips.FindAsync(model.Id);
                    
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RISlips.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.RISlips.Attach(entity);
                    // Delete the entity
                    db.RISlips.Remove(entity);
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
        
        public string NextRisNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.RISlips.Where(w => w.RisDate.Value.Year == date.Year).OrderByDescending(o => o.RisNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.RisNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        public ActionResult _RISlipItemRead([DataSourceRequest] DataSourceRequest request, Guid? risId)
        {

            var data = db.RISlipItems.Where(w => w.RisId == risId)
                .Select(s => new RISlipItemVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    StockItemId = s.StockItemId,
                    Unit = s.PsItem.PsStock.PsCode.UnitMeas,
                    StockNo = s.PsItem.PsStock.StockNo,
                    Description = s.PsItem.PsStock.PsCode.ItemName.Trim() + (s.PsItem.PsStock.Description == null ? "" : " " + s.PsItem.PsStock.Description),
                    ReqQty = s.ReqQty,
                    IssQty = s.IssQty,
                    IssRemarks = s.IssRemarks,                    
                    InsertedDt = s.InsertedDt
                });

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RISlipItemCreate([DataSourceRequest] DataSourceRequest request, RISlipItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.Id = Guid.NewGuid();

                    RISlipItem entity = new RISlipItem()
                    {
                        Id = model.Id,
                        RisId = model.RisId,
                        StockItemId = model.StockItemId,
                        ReqQty = model.ReqQty,
                        IssQty = model.IssQty,
                        IssRemarks = model.IssRemarks,                        
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.RISlipItems.Add(entity);
                    await db.SaveChangesAsync();

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
        public async Task<ActionResult> _RISlipItemUpdate([DataSourceRequest] DataSourceRequest request, RISlipItem model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    RISlipItem entity = await db.RISlipItems.FindAsync(model.Id);

                    entity.RisId = model.RisId;
                    entity.StockItemId = model.StockItemId;
                    entity.ReqQty = model.ReqQty;
                    entity.IssQty = model.IssQty;
                    entity.IssRemarks = model.IssRemarks;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RISlipItems.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

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
        public async Task<ActionResult> _RISlipItemDestroy([DataSourceRequest]DataSourceRequest request, RISlipItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "ris");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    RISlipItem entity = await db.RISlipItems.FindAsync(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RISlipItems.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.RISlipItems.Attach(entity);
                    // Delete the entity
                    db.RISlipItems.Remove(entity);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await db.SaveChangesAsync();
                    //db.Configuration.ValidateOnSaveEnabled = true;   

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
    }
}