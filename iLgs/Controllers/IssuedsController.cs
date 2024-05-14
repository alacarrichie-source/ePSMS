//using iLgs.Models;
//using Kendo.Mvc.UI;
//using Kendo.Mvc.Extensions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;
//using System.Data.Entity;
//using Microsoft.AspNet.Identity;
//using iLgs.Utilities;
//using Newtonsoft.Json;


//namespace iLgs.Controllers
//{
//    [AppAuthorize("ISSUANCES")]
//    public class IssuedsController : Controller
//    {
//        private AppManEntities db = new AppManEntities();
//        // GET: Issuances
//        public ActionResult Index()
//        {
//            return View();
//        }

//        /*
//         * 
//         */
//        public ActionResult _IssuedOrder()
//        {
//            return PartialView();
//        }


//        /*
//         * 
//         */
//        public ActionResult _IssuedOrderRead([DataSourceRequest] DataSourceRequest request, Guid? orderId)
//        {
//            var data = db.Issueds.Where(w => w.OrderId == orderId)
//                .Select(s => new IssuedVM
//                {
//                    Id = s.Id,
//                    OrderId = s.OrderId,
//                    SerialNo = s.SerialNo,
//                    Fund = s.Fund,
//                    Date = s.Date,
//                    CertifiedBy = s.CertifiedBy,
//                    CertifiedDate = s.CertifiedDate,
//                    PostedBy = s.PostedBy,
//                    PostedDate = s.PostedDate
//                })
//                .AsQueryable();

//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };
//            return result;
//        }



//        /*
//         * 
//         */
//        public ActionResult Order()
//        {
//            return View();
//        }

//        /*
//         * 
//         */
//        public ActionResult OrderRead([DataSourceRequest] DataSourceRequest request)
//        {
//            var data = db.Orders.Include(i => i.OrderItems).Include(i => i.Issueds)
//                .Select(s => new OrderVM
//                {
//                    Id = s.Id,
//                    PoNo = s.PoNo,
//                    PoDate = s.PoDate,
//                    PoMode = s.PoMode,
//                    PrNo = s.PrNo,
//                    SupplierId = s.SupplierId,
//                    SupplierName = s.Supplier.Name,
//                    SupplierAddress = s.Supplier.Address,
//                    SupplierTin = s.Supplier.TIN,
//                    DeliveryPlace = s.DeliveryPlace,
//                    DeliveryDate = s.DeliveryDate,
//                    TermDelivery = s.TermDelivery,
//                    TermPayment = s.TermPayment,
//                    SignedBySuppName = s.SignedBySuppName,
//                    SignedBySuppDate = s.SignedBySuppDate,
//                    SignedByAuthName = s.SignedByAuthName,
//                    SignedByAuthDesignation = s.SignedByAuthDesignation,
//                    ResoNo = s.ResoNo,
//                    CertifiedCorrectBy = s.CertifiedCorrectBy,
//                    CertifiedCorrectDate = s.CertifiedCorrectDate,
//                    QtyTotal = s.OrderItems.Sum(t => t.Qty) ?? 0,
//                    QtyIssued = s.Issueds.Sum(t => t.IssuedItems.Sum(u => u.Qty)) ?? 0,
//                    QtyRemaining = (s.OrderItems.Sum(t => t.Qty) ?? 0) - (s.Issueds.Sum(t => t.IssuedItems.Sum(u => u.Qty)) ?? 0),
//                    TotalAmount = s.OrderItems.Sum(t => t.Amount) ?? 0
//                })
//                .AsQueryable();

//            var result = new JsonNetResult
//            {
//                Data = data.ToDataSourceResult(request),
//                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
//                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
//            };
//            return result;
//        }

//        /*
//         * 
//         */
//        public ActionResult _OrderItem()
//        {
//            return PartialView();
//        }

//        /*
//         * 
//         */
//        public ActionResult _OrderItemRead([DataSourceRequest] DataSourceRequest request, Guid? orderId)
//        {

//            var data = db.OrderItems.Include(i => i.IssuedItems)
//                .Where(w => w.OrderId == orderId)
//                .Select(s => new OrderItemVM
//                {
//                    Id = s.Id,
//                    PsCodeId = s.PsCodeId,
//                    PsNo = s.PsCode.PsNo,
//                    PsUnit = s.PsCode.UnitMeas,
//                    PsItem = s.PsCode.ItemName,
//                    Description = s.Description,
//                    Qty = s.Qty ?? 0,
//                    UnitCost = s.UnitCost,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt,
//                    QtyIssued = s.IssuedItems.Sum(t => t.Qty) ?? 0,
//                    QtyRemaining = (s.Qty ?? 0) - (s.IssuedItems.Sum(t => t.Qty) ?? 0)
//                });

//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        #region ISSUED ORDERS
//        public async Task<ActionResult> _IssuedOrderAdd(Guid orderId, string serialNo)
//        {
//            var date = System.DateTime.Now;

//            Task<Access> accessTask = Access(User.Identity.GetUserId(), "issueds");
//            Access access = await accessTask;

//            ViewBag.Access = access;

//            IssuedVM model;

//            try
//            {
//                if (string.IsNullOrWhiteSpace(serialNo))
//                {
//                    var order = await db.Orders.FindAsync(orderId);
//                    model = new IssuedVM()
//                    {
//                        Id = Guid.NewGuid(),
//                        OrderId = orderId,
//                        Date = date,
//                        SerialYear = order.PoYear,
//                        SerialMonth = order.PoMonth,
//                        SerialSeries = "",
//                        SerialNo = ""
//                    };

//                    // To do: initialize issued items with balances of order items

//                }
//                else
//                {
//                    model = await db.Issueds.Where(w => w.SerialNo == serialNo).Select(s => new IssuedVM
//                    {
//                        Id = s.Id,
//                        SerialNo = s.SerialNo,
//                        Date = s.Date,
//                        Fund = s.Fund,
//                        CertifiedBy = s.CertifiedBy,
//                        CertifiedDate = s.CertifiedDate,
//                        PostedBy = s.PostedBy,
//                        PostedDt = s.PostedDt
//                    }).FirstOrDefaultAsync();
//                }
//            }
//            catch (Exception e)
//            {
//                ViewBag.Error = e.Message;
//                return View("Error");
//            }

//            return PartialView(model);
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _IssuedOrderDestroy([DataSourceRequest]DataSourceRequest request, IssuedVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issueds");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
//                }
//                else
//                {
//                    var entity = await db.Issueds.FindAsync(model.Id);
//                    db.Issueds.Attach(entity);
//                    // Delete the entity
//                    db.Issueds.Remove(entity);
//                    // Or use DeleteObject if using a previous version of Entity Framework
//                    // Delete the entity in the database
//                    //db.Entry(model).State = System.Data.EntityState.Deleted;
//                    await db.SaveChangesAsync();
//                    //db.Configuration.ValidateOnSaveEnabled = true;                

//                    // To do: Update stocks
//                }
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
//                     "please contact tech support with this message: " + e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _IssuedOrderSave(IssuedVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issueds");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("Access Error", "Access Denied!");
//                }
               
//                Issued entity = await db.Issueds.Include(i => i.IssuedItems).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
//                if (entity == null) // Add
//                {
//                    if (!string.IsNullOrWhiteSpace(model.SerialSeries))
//                    {
//                        if (db.Issueds.Any(a => a.SerialNo == model.SerialNo_))
//                        {
//                            ModelState.AddModelError("SerialNo", "Serial No. already exists!");
//                        }
//                    }
//                }
//                else
//                {
//                    if (db.Issueds.Any(a => a.Id != model.Id && a.SerialNo == model.SerialNo_))
//                    {
//                        ModelState.AddModelError("SerialNo", "Serial No. already exists!");
//                    }
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    Guid userId = new Guid(User.Identity.GetUserId());
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    if (entity == null)
//                    {
//                        model.Id = Guid.NewGuid();
//                        model.SerialNo = NextSeriesNo(model.SerialYear, model.SerialMonth);
//                        model.InsertedBy = user;
//                        model.InsertedDt = date;
//                        model.UpdatedBy = user;
//                        model.UpdatedDt = date;

//                        entity = new Issued()
//                        {
//                            Id = model.Id,
//                            OrderId = model.OrderId,                            
//                            SerialNo = model.SerialNo,
//                            Date = model.Date,
//                            Fund = model.Fund,
//                            CertifiedBy = model.CertifiedBy,
//                            CertifiedDate = model.CertifiedDate,
//                            PostedBy = model.PostedBy,
//                            PostedDt = model.PostedDt,
//                            InsertedBy = model.InsertedBy,
//                            InsertedDt = model.InsertedDt,
//                            UpdatedBy = model.UpdatedBy,
//                            UpdatedDt = model.UpdatedDt
//                        };
//                        db.Issueds.Add(entity);
//                    }
//                    else
//                    {
//                        entity.SerialYear = model.SerialYear;
//                        entity.SerialMonth = model.SerialMonth;
//                        entity.SerialSeries = model.SerialSeries;
//                        entity.SerialNo = model.SerialNo_;
//                        entity.Date = model.Date;
//                        entity.Fund = model.Fund;
//                        entity.CertifiedBy = model.CertifiedBy;
//                        entity.CertifiedDt = model.CertifiedDt;
//                        entity.PostedBy = model.PostedBy;
//                        entity.PostedDt = model.PostedDt;
//                        entity.UpdatedBy = model.UpdatedBy;
//                        entity.UpdatedDt = model.UpdatedDt;

//                        db.Issueds.Attach(entity);
//                        db.Entry(entity).State = EntityState.Modified;
//                    }

//                    await db.SaveChangesAsync();

//                    return Json(new { Errors = "", Model = model });
//                }
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("Internal Error", "Unable to save changes, Try again, and if the problem persists " +
//                     "please contact tech support with this message: " + e.Message);

//            }
//            return Json(new { Errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray() });
//        }

//        public string NextSeriesNo(string serialYear, string serialMonth)
//        {
//            string yyyy = serialYear;
//            string mm = serialMonth;

//            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

//            string keyName = yyyy + "-" + mm;
//            // yyyy-mm-9999
//            // 123456789012

//            var issued = db.Issueds.Where(w => w.SerialYear == yyyy && w.SerialMonth == mm).OrderByDescending(o => o.SerialNo).FirstOrDefault();
//            if (issued == null)
//            {
//                return keyName + "-" + "0001";
//            }
//            else
//            {
//                var sequence = (int.Parse(issued.SerialSeries) + 1).ToString();
//                return keyName + "-" + sequence.PadLeft(4, '0');
//            }
//        }
//        #endregion

//        #region ISSUED ORDER ITEMS
//        /*
//         * 
//         */
//        public ActionResult _IssuedOrderItemRead([DataSourceRequest] DataSourceRequest request, Guid? issuedId)
//        {
//            var data = db.IssuedItems.Include(i => i.OrderItem.PsCode)
//                .Where(w => w.IssuedId == issuedId)
//                .Select(s => new IssuedItemVM
//                {
//                    Id = s.Id,
//                    IssuedId = s.IssuedId,
//                    OrderItemId = s.OrderItemId,
//                    RISNo = s.RISNo,
//                    RCCode = s.RCCode,
//                    StockNo = s.OrderItem.PsCode.PsNo,
//                    Item = s.OrderItem.PsCode.ItemName,
//                    Unit = s.OrderItem.PsCode.UnitMeas,
//                    Qty = s.Qty ?? 0,
//                    UnitCost = s.OrderItem.UnitCost,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt
//                });

//            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _IssuedOrderItemCreate([DataSourceRequest] DataSourceRequest request, IssuedItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issueds");
//                Access access = await accessTask;
//                if (!access.AllowAdd)
//                {
//                    ModelState.AddModelError("Access Error", "Access Denied!");
//                }

//                if (model != null && ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    model.Id = Guid.NewGuid();

//                    IssuedItem entity = new IssuedItem()
//                    {
//                        Id = model.Id,
//                        IssuedId = model.IssuedId,
//                        RISNo = model.RISNo,
//                        RCCode = model.RCCode,
//                        OrderItemId = model.OrderItemId,
//                        Qty = model.Qty,
//                        Amount = model.Amount,
//                        InsertedBy = user,
//                        InsertedDt = date,
//                        UpdatedBy = user,
//                        UpdatedDt = date
//                    };

//                    db.IssuedItems.Add(entity);
//                    await db.SaveChangesAsync();

//                    // TO DO: save to stock card
//                }
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                     "please contact tech support with this message: " + e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _IssuedOrderItemUpdate([DataSourceRequest] DataSourceRequest request, IssuedItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issueds");
//                Access access = await accessTask;
//                if (!access.AllowEdit)
//                {
//                    ModelState.AddModelError("Access Error", "Access Denied!");
//                }

//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    IssuedItem entity = await db.IssuedItems.FindAsync(model.Id);
//                    entity.RISNo = model.RISNo;
//                    entity.RCCode = model.RCCode;
//                    entity.OrderItemId = model.OrderItemId;
//                    entity.Qty = model.Qty;
//                    entity.Amount = model.Amount;
//                    entity.UpdatedBy = user;
//                    entity.UpdatedDt = date;

//                    db.IssuedItems.Attach(entity);
//                    db.Entry(entity).State = EntityState.Modified;
//                    await db.SaveChangesAsync();

//                    // TO DO: update stock card
//                }
//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                     "please contact tech support with this message: " + e.Message);
//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }

//        [AcceptVerbs(HttpVerbs.Post)]
//        public async Task<ActionResult> _IssuedOrderItemDestroy([DataSourceRequest]DataSourceRequest request, IssuedItemVM model)
//        {
//            try
//            {
//                Task<Access> accessTask = Access(User.Identity.GetUserId(), "issueds");
//                Access access = await accessTask;
//                if (!access.AllowDelete)
//                {
//                    ModelState.AddModelError("GridError", "Delete Access Denied!");
//                }
//                if (ModelState.IsValid)
//                {
//                    string user = ControllerContext.HttpContext.User.Identity.Name;
//                    DateTime date = System.DateTime.Now;

//                    IssuedItem entity = await db.IssuedItems.FindAsync(model.Id);

//                    entity.UpdatedBy = user;
//                    entity.UpdatedDt = date;

//                    db.IssuedItems.Attach(entity);
//                    db.Entry(entity).State = EntityState.Modified;
//                    await db.SaveChangesAsync();

//                    db.IssuedItems.Attach(entity);
//                    // Delete the entity
//                    db.IssuedItems.Remove(entity);
//                    // Or use DeleteObject if using a previous version of Entity Framework
//                    // Delete the entity in the database
//                    //db.Entry(model).State = System.Data.EntityState.Deleted;
//                    await db.SaveChangesAsync();
//                    //db.Configuration.ValidateOnSaveEnabled = true;   

//                    // TO DO: update stocks
//                }

//            }
//            catch (Exception e)
//            {
//                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
//                     "please contact tech support with this message: " + e.Message);


//            }

//            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
//        }
//        #endregion

//        #region EXTRAS
//        public JsonResult GetPoYyyyMm(DateTime poDate)
//        {
//            var poYear = poDate.Date.Year.ToString().Trim();
//            var poMonth = poDate.Date.Month.ToString().Trim().PadLeft(2, '0');
//            return Json(new { PoYear = poYear, PoMonth = poMonth }, JsonRequestBehavior.AllowGet);
//        }

//        #endregion
//    }
//}