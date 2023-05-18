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
    [AppAuthorize("AIRS")]
    public class AIRsController : Controller
    {
        private AppManEntities db = new AppManEntities();
        // GET: 
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AIRRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.AIRs
                .Select(s => new AIR_VM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    PoNo = s.Order.PoNo,
                    Supplier = s.Order.Supplier.BusinessName,
                    PoDate = s.Order.PoDate,
                    Department = s.Order.DeliveryPlace,
                    Fund = s.Fund,
                    AIRNo = s.AIRNo,
                    AIRDate = s.AIRDate,
                    InvoiceNo = s.InvoiceNo,
                    InvoiceDate = s.InvoiceDate,
                    AcceptedDate = s.AcceptedDate,
                    IsComplete = s.IsComplete,
                    IsPartial = s.IsPartial,
                    Custodian = s.Custodian,
                    InspectedDate = s.InspectedDate,
                    IsInspected = s.IsInspected,
                    Officer = s.Officer,
                    Remarks = s.Remarks
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
        public async Task<ActionResult> AIRCreate([DataSourceRequest] DataSourceRequest request, AIR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (db.AIRs.Any(a => a.AIRNo == model.AIRNo))
                {
                    ModelState.AddModelError("AirNo", "AIR No. already exists!");
                }
                else
                {
                    var order = await db.Orders.FindAsync(model.OrderId);
                    if (order == null)
                    {
                        ModelState.AddModelError("PoNo", "Invalid PO No.!");
                    }
                    else
                    {
                        if (order.PoDate > model.AIRDate)
                        {
                            ModelState.AddModelError("AIR Date", "AIR date must be greather than or equal to P.O. date!");
                        }
                        if (order.PoDate > model.InvoiceDate)
                        {
                            ModelState.AddModelError("Invoice Date", "Invoice Date date must be greather than or equal to P.O. date!");
                        }
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.Id = Guid.NewGuid();
                    if (string.IsNullOrWhiteSpace(model.AIRNo))
                    {
                        model.AIRNo = NextAirNo((DateTime)model.AIRDate);
                    }
                    model.InsertedBy = user;
                    model.InsertedDt = date;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = new AIR()
                    {
                        Id = model.Id,
                        Fund = model.Fund,
                        AIRNo = model.AIRNo,
                        AIRDate = model.AIRDate,
                        OrderId = model.OrderId,                       
                        InvoiceNo = model.InvoiceNo,
                        InvoiceDate = model.InvoiceDate,
                        AcceptedDate = model.AcceptedDate,
                        IsComplete = model.IsComplete,
                        IsPartial = model.IsPartial,
                        Custodian = model.Custodian,
                        InspectedDate = model.InspectedDate,
                        IsInspected = model.IsInspected,
                        Officer = model.Officer,
                        Remarks = model.Remarks,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };


                    // include items during add
                    var orderItems = db.OrderItems.Where(w => w.OrderId == model.OrderId).ToList();
                    foreach(var orderItem in orderItems)
                    {
                        
                        AIRItem airItem = new AIRItem()
                        {
                            Id = Guid.NewGuid(),
                            AirId = entity.Id,
                            OrderItemId = orderItem.Id,
                            Qty = orderItem.Qty,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        entity.AIRItems.Add(airItem);
                    }

                    db.AIRs.Add(entity);
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
        public async Task<ActionResult> AIRUpdate([DataSourceRequest] DataSourceRequest request, AIR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (db.AIRs.Any(a => a.Id != model.Id && a.AIRNo == model.AIRNo))
                {
                    ModelState.AddModelError("AirNo", "AIR No. already exists!");
                }
                else
                {
                    var order = await db.Orders.FindAsync(model.OrderId);
                    if (order == null)
                    {
                        ModelState.AddModelError("PoNo", "Invalid PO No.!");
                    }
                    else
                    {
                        if (order.PoDate > model.AIRDate)
                        {
                            ModelState.AddModelError("AIR Date", "AIR date must be greather than or equal to P.O. date!");
                        }

                        if (order.PoDate > model.InvoiceDate)
                        {
                            ModelState.AddModelError("Invoice Date", "Invoice Date date must be greather than or equal to P.O. date!");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = await db.AIRs.FindAsync(model.Id);

                    entity.Fund = model.Fund;
                    entity.AIRNo = model.AIRNo;
                    entity.AIRDate = model.AIRDate;
                    entity.InvoiceNo = model.InvoiceNo;
                    entity.InvoiceDate = model.InvoiceDate;
                    entity.AcceptedDate = model.AcceptedDate;
                    entity.IsComplete = model.IsComplete;
                    entity.IsPartial = model.IsPartial;
                    entity.Custodian = model.Custodian;
                    entity.InspectedDate = model.InspectedDate;
                    entity.IsInspected = model.IsInspected;
                    entity.Officer = model.Officer;
                    entity.Remarks = model.Remarks;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    db.AIRs.Attach(entity);
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
        public async Task<ActionResult> AIRDestroy([DataSourceRequest]DataSourceRequest request, AIR_VM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    var entity = await db.AIRs.FindAsync(model.Id);
                    db.AIRs.Attach(entity);
                    // Delete the entity
                    db.AIRs.Remove(entity);
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
        
        public string NextAirNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.AIRs.Where(w => w.AIRDate.Value.Year == date.Year).OrderByDescending(o => o.AIRNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.AIRNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        public ActionResult _AIRItemRead([DataSourceRequest] DataSourceRequest request, Guid? airId)
        {

            var data = db.AIRItems.Where(w => w.AirId == airId)
                .Select(s => new
                {
                    Id = s.Id,
                    OrderItemId = s.OrderItemId,
                    PsNo = s.OrderItem.RequestItem.PsCode.PsNo,
                    PsItem = s.OrderItem.RequestItem.PsCode.ItemName,
                    OrderDescription = s.OrderItem.RequestItem.Description,
                    PsUnit = s.OrderItem.RequestItem.PsCode.UnitMeas,
                    Qty = s.Qty,
                    InsertedDt = s.InsertedDt
                });

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRItemCreate([DataSourceRequest] DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
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

                    AIRItem entity = new AIRItem()
                    {
                        Id = model.Id,
                        AirId = model.AirId,
                        OrderItemId = model.OrderItemId,
                        Qty = model.Qty,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.AIRItems.Add(entity);
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
        public async Task<ActionResult> _AIRItemUpdate([DataSourceRequest] DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    AIRItem entity = await db.AIRItems.FindAsync(model.Id);

                    entity.AirId = model.AirId;
                    entity.OrderItemId = model.OrderItemId;
                    entity.Qty = model.Qty;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.AIRItems.Attach(entity);
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
        public async Task<ActionResult> _AIRItemDestroy([DataSourceRequest]DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "airs");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    AIRItem entity = await db.AIRItems.FindAsync(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.AIRItems.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.AIRItems.Attach(entity);
                    // Delete the entity
                    db.AIRItems.Remove(entity);
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