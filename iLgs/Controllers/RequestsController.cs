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

namespace iLgs.Controllers
{
    [AppAuthorize("REQUESTS")]
    public class RequestsController : Controller
    {
        private static AppManEntities db = new AppManEntities();
        private static IOrderService orderService = new OrderService(db);
        private static IRequestService requestService = new RequestService(db);

        // GET: Requests
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RequestRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = requestService.GetAll();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RequestCreate([DataSourceRequest] DataSourceRequest request, RequestVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (db.Requests.Any(a => a.PrNo == model.PrNo))
                {
                    ModelState.AddModelError("PrNo", "P.R. number already exists!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.Id = Guid.NewGuid();
                    if (string.IsNullOrWhiteSpace(model.PrNo))
                    {
                        model.PrNo = NextPrNo((DateTime)model.PrDate);
                    }
                    model.InsertedBy = user;
                    model.InsertedDt = date;
                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = new Request()
                    {
                        Id = model.Id,
                        Fund = model.Fund,
                        Department = model.Department,
                        Section = model.Section,
                        PrNo = model.PrNo,
                        PrDate = model.PrDate,
                        FPP = model.FPP,
                        Purpose = model.Purpose,
                        RequestedBy = model.RequestedBy,
                        RequestedDesig = model.RequestedDesig,
                        Availability = model.Availability,
                        AvaialbilityDesig = model.AvaialbilityDesig,
                        ApprovedBy = model.ApprovedBy,
                        ApprovedDesig = model.ApprovedDesig,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };

                    db.Requests.Add(entity);
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

        public string NextPrNo(DateTime prDate)
        {
            string yyyy = prDate.Year.ToString().Trim();
            string mm = prDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.Requests.Where(w => w.PrDate.Value.Year == prDate.Year).OrderByDescending(o => o.PrNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.PrNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RequestUpdate([DataSourceRequest] DataSourceRequest request, RequestVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }
                else if (await requestService.IsPosted(model.Id))
                {
                    ModelState.AddModelError("PR No.", "PR Number already Posted, cannot update!");
                }
                else if (db.Requests.Any(a => a.Id != model.Id && a.PrNo == model.PrNo))
                {
                    ModelState.AddModelError("PR No.", "PR number already exists!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = await db.Requests.FindAsync(model.Id);

                    entity.Fund = model.Fund;
                    entity.Department = model.Department;
                    entity.Section = model.Section;
                    entity.PrNo = model.PrNo;
                    entity.PrDate = model.PrDate;
                    entity.FPP = model.FPP;
                    entity.Purpose = model.Purpose;
                    entity.RequestedBy = model.RequestedBy;
                    entity.RequestedDesig = model.RequestedDesig;
                    entity.Availability = model.Availability;
                    entity.AvaialbilityDesig = model.AvaialbilityDesig;
                    entity.ApprovedBy = model.ApprovedBy;
                    entity.ApprovedDesig = model.ApprovedDesig;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    db.Requests.Attach(entity);
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
        public async Task<ActionResult> RequestDestroy([DataSourceRequest]DataSourceRequest request, RequestVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await requestService.IsPosted(model.Id))
                {
                    ModelState.AddModelError("DeleteError", "PR Number already Posted, cannot delete!");
                }
                else
                {
                    var entity = await db.Requests.FindAsync(model.Id);

                    db.Requests.Attach(entity);
                    // Delete the entity
                    db.Requests.Remove(entity);
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

        public ActionResult _RequestItemRead([DataSourceRequest] DataSourceRequest request, Guid? prId)
        {

            var data = db.RequestItems.Where(w => w.PrId == prId)
                .Select(s => new
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    PsCodeId = s.PsCodeId,
                    PsCode = s.PsCode.PsNo,
                    PsUnit = s.PsCode.UnitMeas,
                    PsItem = s.PsCode.ItemName,
                    Description = s.Description,
                    BrandName = s.BrandName,
                    OtherSpecs = s.OtherSpecs,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    InsertedDt = s.InsertedDt
                });

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RequestItemCreate([DataSourceRequest] DataSourceRequest request, RequestItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await requestService.IsPosted((Guid)model.PrId))
                {
                    ModelState.AddModelError("PR No.", "PR Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.Id = Guid.NewGuid();

                    RequestItem entity = new RequestItem()
                    {
                        Id = model.Id,
                        PrId = model.PrId,
                        PsCodeId = model.PsCodeId,
                        Description = model.Description,
                        BrandName = model.BrandName,
                        OtherSpecs = model.OtherSpecs,
                        Qty = model.Qty,
                        UnitCost = model.UnitCost,
                        TotalCost = model.TotalCost,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.RequestItems.Add(entity);
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
        public async Task<ActionResult> _RequestItemUpdate([DataSourceRequest] DataSourceRequest request, RequestItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await requestService.IsPosted((Guid)model.PrId))
                {
                    ModelState.AddModelError("PR No.", "PR Number already Posted, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    RequestItem entity = await db.RequestItems.FindAsync(model.Id);
                    entity.PsCodeId = model.PsCodeId;
                    entity.Description = model.Description;
                    entity.BrandName = model.BrandName;
                    entity.OtherSpecs = model.OtherSpecs;
                    entity.Qty = model.Qty;
                    entity.UnitCost = model.UnitCost;
                    entity.TotalCost = model.TotalCost;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RequestItems.Attach(entity);
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
        public async Task<ActionResult> _RequestItemDestroy([DataSourceRequest]DataSourceRequest request, RequestItemVM model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (await requestService.IsPosted((Guid)model.PrId))
                {
                    ModelState.AddModelError("DeleteError", "PR Number already Posted, cannot update!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    RequestItem entity = await db.RequestItems.FindAsync(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.RequestItems.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.RequestItems.Attach(entity);
                    // Delete the entity
                    db.RequestItems.Remove(entity);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostRequest(Guid requestId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "request");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (requestService.GetById(requestId) == null)
                {
                    ModelState.AddModelError("Request", "Invalid Request Id");
                }
                else if (await requestService.IsPosted(requestId))
                {
                    ModelState.AddModelError("PR No.", "PR Number already Posted, cannot post again!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await requestService.Post(requestId, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnpostRequest(Guid requestId)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "request");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (requestService.GetById(requestId) == null)
                {
                    ModelState.AddModelError("Request", "Invalid Request Id");
                }
                else if (!(await requestService.IsPosted(requestId)))
                {
                    ModelState.AddModelError("PR No.", "PR Number not yet posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await requestService.Unpost(requestId, user, date);
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
    }
}