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
    [AppAuthorize("ORDERS")]
    public class OrdersController : Controller
    {
        private AppManEntities db = new AppManEntities();
        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult OrderRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.Orders
                .Select(s => new OrderVM
                {
                    Id = s.Id,
                    //PoYear = s.PoNo.Substring(0, 4),
                    //PoMonth = s.PoNo.Substring(5, 2),
                    //PoSeries = s.PoNo.Substring(8, 4),
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    PoMode = s.PoMode,
                    PoModeDesc = db.Codextns.Where(w => w.Code == s.PoMode && w.CodeMast.Code == "PROC_MODE").FirstOrDefault().Description,
                    PrNo = s.PrNo,
                    SupplierId = s.SupplierId,
                    SupplierName = s.Supplier.Name,
                    SupplierAddress = s.Supplier.Address,
                    SupplierTin = s.Supplier.TIN,
                    DeliveryPlace = s.DeliveryPlace,
                    DeliveryDate = s.DeliveryDate,
                    TermDelivery = s.TermDelivery,
                    TermPayment = s.TermPayment,
                    SignedBySuppName = s.SignedBySuppName,
                    SignedBySuppDate = s.SignedBySuppDate,
                    SignedByAuthName = s.SignedByAuthName,
                    SignedByAuthDesignation = s.SignedByAuthDesignation,
                    ResoNo = s.ResoNo,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    CertifiedCorrectDate = s.CertifiedCorrectDate
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
        public async Task<ActionResult> OrderDestroy([DataSourceRequest]DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    var entity = await db.Orders.FindAsync(model.Id);
                    db.Orders.Attach(entity);
                    // Delete the entity
                    db.Orders.Remove(entity);
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

        public async Task<ActionResult> _OrderAdd(string poNo)
        {
            var date = System.DateTime.Now;

            Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
            Access access = await accessTask;

            ViewBag.Access = access;

            OrderVM model;

            try
            {
                if (string.IsNullOrWhiteSpace(poNo))
                {                    
                    model = new OrderVM()
                    {
                        Id = Guid.NewGuid(),
                        PoDate = date,
                        PoYear = date.Year.ToString().Trim(),
                        PoMonth = date.Month.ToString().Trim().PadLeft(2, '0'),
                        PoNo = "",
                        PrNo = ""
                    };
                }
                else
                {
                    model = await db.Orders.Where(w => w.PoNo == poNo).Select(s => new OrderVM
                    {
                        Id = s.Id,
                        //PoYear = s.PoYear,
                        //PoMonth = s.PoMonth,
                        //PoSeries = s.PoSeries,
                        PoNo = s.PoNo,
                        PoDate = s.PoDate,
                        PoMode = s.PoMode,
                        PrNo = s.PrNo,
                        SupplierId = s.SupplierId,
                        SupplierName = s.Supplier.Name,
                        SupplierAddress = s.Supplier.Address,
                        SupplierTin = s.Supplier.TIN,
                        DeliveryPlace = s.DeliveryPlace,
                        DeliveryDate = s.DeliveryDate,
                        TermDelivery = s.TermDelivery,
                        TermPayment = s.TermPayment,
                        SignedBySuppName = s.SignedBySuppName,
                        SignedBySuppDate = s.SignedBySuppDate,
                        SignedByAuthName = s.SignedByAuthName,
                        SignedByAuthDesignation = s.SignedByAuthDesignation,
                        ResoNo = s.ResoNo,
                        CertifiedCorrectBy = s.CertifiedCorrectBy,
                        CertifiedCorrectDate = s.CertifiedCorrectDate
                    }).FirstOrDefaultAsync();                    
                }
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                return View("Error");
            }

            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderSave(OrderVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access Error", "Access Denied!");
                }

                Order entity = await db.Orders.Include(i => i.OrderItems).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
                if (entity == null) // Add
                {
                    if (!string.IsNullOrWhiteSpace(model.PoNo))
                    {
                        if (db.Orders.Any(a => a.PoNo == model.PoNo))
                        {
                            ModelState.AddModelError("PoNo", "P.O. number already exists!");
                        }
                    }
                }
                else
                {
                    if (db.Orders.Any(a => a.Id != model.Id && a.PoNo == model.PoNo))
                    {
                        ModelState.AddModelError("PoNo", "P.O. number already exists!");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    Guid userId = new Guid(User.Identity.GetUserId());
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (entity == null)
                    {

                        model.Id = Guid.NewGuid();
                        if (string.IsNullOrWhiteSpace(model.PoNo))
                        {
                            model.PoNo = NextPoNo((DateTime)model.PoDate);                        
                        }
                        model.InsertedBy = user;
                        model.InsertedDt = date;
                        model.UpdatedBy = user;
                        model.UpdatedDt = date;

                        entity = new Order()
                        {
                            Id = model.Id,
                            //PoYear = model.PoYear,
                            //PoMonth = model.PoMonth,
                            //PoSeries = model.PoNo.Split('-')[2],
                            PoNo = model.PoNo,
                            PoDate = model.PoDate,
                            PoMode = model.PoMode,
                            PrNo = model.PrNo,
                            SupplierId = model.SupplierId,
                            DeliveryPlace = model.DeliveryPlace,
                            DeliveryDate = model.DeliveryDate,
                            TermDelivery = model.TermDelivery,
                            TermPayment = model.TermPayment,
                            SignedBySuppName = model.SignedBySuppName,
                            SignedBySuppDate = model.SignedBySuppDate,
                            SignedByAuthName = model.SignedByAuthName,
                            SignedByAuthDesignation = model.SignedByAuthDesignation,
                            ResoNo = model.ResoNo,
                            CertifiedCorrectBy = model.CertifiedCorrectBy,
                            CertifiedCorrectDate = model.CertifiedCorrectDate,
                            InsertedBy = model.InsertedBy,
                            InsertedDt = model.InsertedDt,
                            UpdatedBy = model.UpdatedBy,
                            UpdatedDt = model.UpdatedDt                            
                        };
                        db.Orders.Add(entity);
                    }
                    else
                    {
                        //entity.PoYear = model.PoYear;
                        //entity.PoMonth = model.PoMonth;
                        //entity.PoSeries = model.PoSeries;
                        //entity.PoNo = model.PoNo_;
                        entity.PoDate = model.PoDate;
                        entity.PoMode = model.PoMode;
                        entity.PrNo = model.PrNo;
                        entity.SupplierId = model.SupplierId;
                        entity.DeliveryPlace = model.DeliveryPlace;
                        entity.DeliveryDate = model.DeliveryDate;
                        entity.TermDelivery = model.TermDelivery;
                        entity.TermPayment = model.TermPayment;
                        entity.SignedBySuppName = model.SignedBySuppName;
                        entity.SignedBySuppDate = model.SignedBySuppDate;
                        entity.SignedByAuthName = model.SignedByAuthName;
                        entity.SignedByAuthDesignation = model.SignedByAuthDesignation;
                        entity.ResoNo = model.ResoNo;
                        entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
                        entity.CertifiedCorrectDate = model.CertifiedCorrectDate;
                        entity.UpdatedBy = model.UpdatedBy;
                        entity.UpdatedDt = model.UpdatedDt;                        

                        db.Orders.Attach(entity);
                        db.Entry(entity).State = EntityState.Modified;
                    }

                    await db.SaveChangesAsync();

                    return Json(new { Errors = "", Model = model });
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Internal Error", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);

            }
            return Json(new { Errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors).Select(m => m.ErrorMessage).ToArray() });
        }

        public string NextPoNo(DateTime poDate)
        {
            string yyyy = poDate.Year.ToString().Trim();
            string mm = poDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var order = db.Orders.Where(w => w.PoDate.Value.Year == poDate.Year 
                && w.PoDate.Value.Month == poDate.Month).OrderByDescending(o => o.PoNo).FirstOrDefault();
            if (order == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(order.PoNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        public ActionResult _OrderItemRead([DataSourceRequest] DataSourceRequest request, Guid? orderId)
        {

            var data = db.OrderItems.Where(w => w.OrderId == orderId)
                .Select(s => new
                {
                    Id = s.Id,
                    PsCodeId = s.PsCodeId,
                    PsNo = s.PsCode.PsNo,
                    PsUnit = s.PsCode.UnitMeas,
                    PsItem = s.PsCode.ItemName,
                    Description = s.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                });

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemCreate([DataSourceRequest] DataSourceRequest request, OrderItem model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.Id = Guid.NewGuid();

                    OrderItem entity = new OrderItem()
                    {
                        Id = model.Id,
                        OrderId = model.OrderId,
                        PsCodeId = model.PsCodeId,
                        Description = model.Description,
                        Qty = model.Qty,
                        UnitCost = model.UnitCost,
                        Amount = model.Amount,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.OrderItems.Add(entity);
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
        public async Task<ActionResult> _OrderItemUpdate([DataSourceRequest] DataSourceRequest request, OrderItem model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("GridError", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    OrderItem entity = await db.OrderItems.FindAsync(model.Id);
                    entity.PsCodeId = model.PsCodeId;
                    entity.Description = model.Description;
                    entity.Qty = model.Qty;
                    entity.UnitCost = model.UnitCost;
                    entity.Amount = model.Amount;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.OrderItems.Attach(entity);
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
        public async Task<ActionResult> _OrderItemDestroy([DataSourceRequest]DataSourceRequest request, OrderItem model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    OrderItem entity = await db.OrderItems.FindAsync(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.OrderItems.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.OrderItems.Attach(entity);
                    // Delete the entity
                    db.OrderItems.Remove(entity);
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

        #region EXTRAS
        public JsonResult GetPoYyyyMm(DateTime poDate)
        {
            var poYear = poDate.Date.Year.ToString().Trim();
            var poMonth = poDate.Date.Month.ToString().Trim().PadLeft(2, '0');
            return Json(new { PoYear = poYear, PoMonth = poMonth }, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}