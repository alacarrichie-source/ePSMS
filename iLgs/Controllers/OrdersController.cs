using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using iLgs.Utilities;
using Newtonsoft.Json;
using iLgs.Services.Interfaces;
using iLgs.Services;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using System.Data.SqlClient;
using System.IO;

namespace iLgs.Controllers
{
    [AppAuthorize("ORDERS")]
    public class OrdersController : Controller
    {
        AppManEntities db = new AppManEntities();
        IOrderService orderService;
        IRequestService requestService;

        public OrdersController()
        {
            this.orderService = new OrderService(db);
            this.requestService = new RequestService(db);
        }

        // GET: Codes
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult OrderRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = orderService.GetAll();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> OrderCreate([DataSourceRequest] DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Add Access Denied!");
                }

                if (await orderService.GetByPoNoAsync(model.PoNo) != null)
                {
                    ModelState.AddModelError("PoNo", "P.O. number already exists!");
                }
                else
                {
                    var pr = await requestService.GetByIdAsync(model.PrId);
                    if (pr == null)
                    {
                        ModelState.AddModelError("PrNo", "Invalid P.R. Number!");
                    }
                    else
                    {
                        if (pr.PrDate > model.PoDate)
                        {
                            ModelState.AddModelError("PoDate", "P.O. date must be greather than or equal to P.R. date!");
                        }
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> OrderUpdate([DataSourceRequest] DataSourceRequest request, OrderVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                if (await orderService.GetAnyPoNoAsync(model.Id, model.PoNo))
                {
                    ModelState.AddModelError("PO No.", "P.O. number already exists!");
                }
                else
                {
                    var pr = await requestService.GetByIdAsync(model.PrId);
                    if (pr == null)
                    {
                        ModelState.AddModelError("PR No.", "Invalid P.R. Number!");
                    }
                    else if (await orderService.IsPostedAsync(model.Id))
                    {
                        ModelState.AddModelError("PO NO.", "PO Number already Posted, cannot update!");
                    }
                    else if (pr.PrDate > model.PoDate)
                    {
                        ModelState.AddModelError("PO Date", "P.O. date must be greather than or equal to P.R. date!");
                    }
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderService.UpdateAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
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
                else if (await orderService.IsPostedAsync(model.Id))
                {
                    ModelState.AddModelError("DeleteError", "PO Number already Posted, cannot delete!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await orderService.DeleteAsync(model, user, date);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult _OrderItemRead([DataSourceRequest] DataSourceRequest request, Guid? orderId)
        {

            var data = db.OrderItems.Where(w => w.OrderId == orderId)
                .Select(s => new
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    RequestItemId = s.RequestItemId,
                    PsNo = s.RequestItem.PsCode.PsNo,
                    PsUnit = s.RequestItem.PsCode.UnitMeas,
                    PsItem = s.RequestItem.PsCode.ItemName,
                    Description = s.Description,
                    BrandName = s.BrandName,
                    OtherSpecs = s.OtherSpecs,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                });

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _OrderItemCreate([DataSourceRequest] DataSourceRequest request, OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
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
                        RequestItemId = model.RequestItemId,
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
        public async Task<ActionResult> _OrderItemUpdate([DataSourceRequest] DataSourceRequest request, OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    OrderItem entity = await db.OrderItems.FindAsync(model.Id);

                    entity.RequestItemId = model.RequestItemId;
                    entity.Description = model.Description;
                    entity.BrandName = model.BrandName;
                    entity.OtherSpecs = model.OtherSpecs;
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
        public async Task<ActionResult> _OrderItemDestroy([DataSourceRequest]DataSourceRequest request, OrderItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await orderService.IsPostedAsync((Guid)model.OrderId))
                {
                    ModelState.AddModelError("DeleteError", "PO Number already Posted, cannot delete!");
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostOrders(Guid orderId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.GetByIdAsync(orderId) == null)
                {
                    ModelState.AddModelError("Order", "Invalid Order Id");
                }
                else if (await orderService.IsPostedAsync(orderId))
                {
                    ModelState.AddModelError("PO No.", "PO Number already Posted, cannot post again!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await orderService.PostAsync(orderId, user, date);
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

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> UnpostOrders(Guid orderId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "orders");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await orderService.GetByIdAsync(orderId) == null)
                {
                    ModelState.AddModelError("Order", "Invalid Order Id");
                }
                else if (!(await orderService.IsPostedAsync(orderId)))
                {
                    ModelState.AddModelError("PO No.", "PO Number not yet posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await orderService.UnpostAsync(orderId, user, date);
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

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPoYyyyMm(DateTime poDate)
        {
            var poYear = poDate.Date.Year.ToString().Trim();
            var poMonth = poDate.Date.Month.ToString().Trim().PadLeft(2, '0');
            return Json(new { PoYear = poYear, PoMonth = poMonth }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region PRINTOUTS
        public ActionResult PurchaseOrderRpt(string poNo)
        {
            string stringname = db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Po_.rpt"));
            rpt.Load();
            rpt.Refresh();

            rpt.SetDatabaseLogon(un, pw, svr, db_);
            foreach (Table table in rpt.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = svr;
                logonInfo.ConnectionInfo.DatabaseName = db_;
                logonInfo.ConnectionInfo.UserID = un;
                logonInfo.ConnectionInfo.Password = pw;
                table.ApplyLogOnInfo(logonInfo);
            }

            rpt.SetParameterValue("@cPoNo", poNo);
            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion
    }
}