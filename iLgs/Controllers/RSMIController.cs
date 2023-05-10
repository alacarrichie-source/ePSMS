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
    [AppAuthorize("RSMI")]
    public class RSMIController : Controller
    {
        private AppManEntities db = new AppManEntities();
        
        // GET: RSMI
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RSMIRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.RSMIs.AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult _RSMIAdd()
        {

            RSMIProcessVM model = new RSMIProcessVM()
            {
                DateFrom = DateTime.Now,
                DateTo = DateTime.Now
            };
            return PartialView(model);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RSMIAddSave([DataSourceRequest] DataSourceRequest request, RSMIProcessVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "rsmi");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }
                else
                {
                    if (db.RSMIs.Any(a => a.Date >= model.DateFrom && a.Date <= model.DateTo))
                    {
                        ModelState.AddModelError("Period", "Period entered already exists..");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var risDates = db.RISlips.Where(w => w.RisDate >= model.DateFrom && w.RisDate <= model.DateTo)
                                    .AsNoTracking()
                                    .GroupBy(g => new { g.RisDate, g.Fund })
                                    .Select(s => new { Date = s.Key.RisDate, Fund = s.Key.Fund }).ToList();

                    if (!risDates.Any())
                    {
                        ModelState.AddModelError("Period", "No RIS found on period entered.");
                    }
                    else
                    {
                        foreach (var risDate in risDates)
                        {
                            var serialNo = NextSerialNo(risDate.Date);
                            var entity = new RSMI()
                            {
                                Id = Guid.NewGuid(),
                                Date = risDate.Date,
                                Fund = risDate.Fund,
                                SerialNo = serialNo,
                                Custodian = model.Custodian,
                                PostedBy = model.PostedBy,
                                PostedDt = model.PostedDt,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            var riSlips = db.RISlips
                                .Where(w => w.RisDate == risDate.Date && w.Fund == risDate.Fund)
                                .AsNoTracking()
                                .Select(s => new { Id = s.Id  }).ToList();
                            foreach (var riSlip in riSlips)
                            {
                                var rsmiItem = new RSMIItem()
                                {
                                    Id = Guid.NewGuid(),
                                    RsmiId = entity.Id,
                                    RisId = riSlip.Id,
                                    InsertedBy = user,
                                    InsertedDt = date
                                };
                                entity.RSMIItems.Add(rsmiItem);

                                var riSlipItems = db.RISlipItems.Include(i => i.PsItem.PsStock.PsCode).Include(i => i.PsItem.OrderItem.Order).AsNoTracking().ToList();

                                foreach (var riSlipItem in riSlipItems)
                                {
                                    var rsmiRecap = new RSMIRecap()
                                    {
                                        Id = Guid.NewGuid(),
                                        RsmiId = entity.Id,
                                        PsItemId = riSlipItem.PsItem.Id,
                                        StockNo = riSlipItem.PsItem.PsStock.StockNo,
                                        Qty = riSlipItem.IssQty,
                                        UnitCost = riSlipItem.UnitCost,
                                        TotalCost = riSlipItem.Amount,
                                        AccountCode = ""
                                    };
                                    entity.RSMIRecaps.Add(rsmiRecap);
                                }
                            }
                            db.RSMIs.Add(entity);
                            await db.SaveChangesAsync();
                        }                        
                    }
                    
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
            else
            {
                return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
            }                        
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RSMICreate([DataSourceRequest] DataSourceRequest request, RSMIProcessVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "rsmi");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }
                else
                {
                    if (db.RSMIs.Any(a => a.Date >= model.DateFrom && a.Date <= model.DateTo))
                    {
                        ModelState.AddModelError("Period", "Period entered already exists..");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var risDates = db.RISlips.Where(w => w.RisDate >= model.DateFrom && w.RisDate <= model.DateTo)
                                    .AsNoTracking()
                                    .GroupBy(g => new { g.RisDate, g.Fund })
                                    .Select(s => new { Date = s.Key.RisDate, Fund = s.Key.Fund });

                    foreach(var risDate in risDates)
                    {
                        var serialNo = NextSerialNo(risDate.Date);
                        var entity = new RSMI()
                        {
                            Id = Guid.NewGuid(),
                            Date = risDate.Date,
                            Fund = risDate.Fund,
                            SerialNo = serialNo,                            
                            Custodian = model.Custodian,
                            PostedBy = model.PostedBy,
                            PostedDt = model.PostedDt,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        var riSlips = db.RISlips.Where(w => w.RisDate == risDate.Date && w.Fund == risDate.Fund)
                            .AsNoTracking()
                            .Select(s => new { Id = s.Id, RISlipItems = s.RISlipItems });
                        foreach(var riSlip in riSlips)
                        {
                            var rsmiItem = new RSMIItem()
                            {
                                Id = Guid.NewGuid(),
                                RsmiId = entity.Id,
                                RisId = riSlip.Id,
                                InsertedBy = user,
                                InsertedDt = date
                            };
                            entity.RSMIItems.Add(rsmiItem);

                            foreach(var riSlipItem in riSlip.RISlipItems)
                            {
                                var rsmiRecap = new RSMIRecap()
                                {
                                    Id = Guid.NewGuid(),
                                    RsmiId = entity.Id,
                                    PsItemId = riSlipItem.PsItem.Id,
                                    StockNo = riSlipItem.PsItem.PsStock.StockNo,
                                    Qty = riSlipItem.IssQty,
                                    UnitCost = riSlipItem.UnitCost,
                                    TotalCost = riSlipItem.Amount,
                                    AccountCode = ""
                                };
                                entity.RSMIRecaps.Add(rsmiRecap);
                            }
                        }

                        db.RSMIs.Add(entity);
                    }                    
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
        public async Task<ActionResult> RSMIUpdate([DataSourceRequest] DataSourceRequest request, RsmiVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "rsmi");
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

                    var entity = db.RSMIs.Find(model.Id);
                    entity.Custodian = model.Custodian;
                    entity.PostedBy = model.PostedBy;
                    entity.PostedDt = model.PostedDt;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    db.RSMIs.Attach(entity);
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
        public async Task<ActionResult> RSMIDestroy([DataSourceRequest]DataSourceRequest request, RsmiVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "rsmi");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = db.RSMIs.Find(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RSMIs.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    entity = db.RSMIs.Find(model.Id);
                    db.RSMIs.Attach(entity);
                    db.RSMIs.Remove(entity);
                    await db.SaveChangesAsync();                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult RSMIItemRead([DataSourceRequest] DataSourceRequest request, Guid? rsmiId)
        {
            var data = db.RISlipItems.Where(w => w.RISlip.RSMIItems.Any(a => a.RsmiId == rsmiId))
                .Select(s => new RSMIItemVM
                {
                    Id = s.Id,
                    RISNo = s.RISlip.RisNo,
                    StockNo = s.PsItem.PsStock.StockNo,
                    RCC = s.PsItem.OrderItem.RequestItem.Request.FPP,
                    ItemName = s.PsItem.PsStock.PsCode.ItemName,
                    QtyIss = s.IssQty,
                    Unit = s.PsItem.PsStock.PsCode.UnitMeas,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount
                }).AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public string NextSerialNo(DateTime? date)
        {
            string yyyy = date.Value.Year.ToString().Trim();
            string mm = date.Value.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.RSMIs.Where(w => w.Date.Value.Year == date.Value.Year && w.Date.Value.Month == date.Value.Month).OrderByDescending(o => o.SerialNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.SerialNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
    }
}