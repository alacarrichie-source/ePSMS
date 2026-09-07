using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Requisition;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("REQUISITIONS")]
    public class RequisitionsController : BaseController
    {
        private const string AccessCode = "ris";
        private readonly IRisService _risService;

        public RequisitionsController()
        {
            _risService = new RisService(_db);
        }

        public ActionResult Index()
        {
            return View("~/Views/Requisitions/Index.cshtml");
        }

        public async Task<ActionResult> Read([DataSourceRequest] DataSourceRequest request, string search, string status, string department, string fund, DateTime? fromDate, DateTime? toDate)
        {
            var data = await FilteredQuery(search, status, department, fund, fromDate, toDate);
            return new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
        }

        public async Task<ActionResult> Summary(string search, string status, string department, string fund, DateTime? fromDate, DateTime? toDate)
        {
            var data = await FilteredQuery(search, status, department, fund, fromDate, toDate);
            var result = new RisSummaryViewModel
            {
                Total = await data.CountAsync(),
                Draft = await data.CountAsync(x => x.Status == "Draft"),
                Unposted = await data.CountAsync(x => x.Status == "Unposted"),
                Posted = await data.CountAsync(x => x.Status == "Posted")
            };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> FilterOptions()
        {
            var data = await _risService.GetAllAsync(User.Identity.GetUserId());
            var departments = await data.Where(x => x.Office != null && x.Office != "").Select(x => x.Office).Distinct().OrderBy(x => x).ToListAsync();
            var funds = await data.Where(x => x.Fund != null && x.Fund != "").Select(x => x.Fund).Distinct().OrderBy(x => x).ToListAsync();
            return Json(new { Departments = departments, Funds = funds }, JsonRequestBehavior.AllowGet);
        }

        private async Task<IQueryable<RIS_VM>> FilteredQuery(string search, string status, string department, string fund, DateTime? fromDate, DateTime? toDate)
        {
            var data = await _risService.GetAllAsync(User.Identity.GetUserId());
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                data = data.Where(x => (x.RisNo != null && x.RisNo.Contains(search)) ||
                    (x.Office != null && x.Office.Contains(search)) ||
                    (x.Purpose != null && x.Purpose.Contains(search)) ||
                    (x.RequestedBy != null && x.RequestedBy.Contains(search)));
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "All") data = data.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(department)) data = data.Where(x => x.Office == department);
            if (!string.IsNullOrWhiteSpace(fund)) data = data.Where(x => x.Fund == fund);
            if (fromDate.HasValue) data = data.Where(x => x.RisDate >= fromDate.Value);
            if (toDate.HasValue)
            {
                var exclusiveEnd = toDate.Value.Date.AddDays(1);
                data = data.Where(x => x.RisDate < exclusiveEnd);
            }
            return data;
        }

        public async Task<ActionResult> EligibleDepartments()
        {
            var names = await ((RisService)_risService).WizardDepartmentsAsync(User.Identity.GetUserId());
            return Json(names.Select(x => new { Text = x, Value = x }), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create()
        {
            return new HttpStatusCodeResult(409, "Use the RIS wizard to create and post RIS.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update()
        {
            return new HttpStatusCodeResult(409, "RIS editing is not available in this workflow.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(Guid risId)
        {
            if (await HasAccessAsync(a => a.AllowDelete, "Delete access denied.") && await CanAccessRis(risId))
            {
                try { await _risService.DeleteAsync(new RIS_VM { Id = risId }, User.Identity.Name, DateTime.Now); }
                catch (Exception ex) { AddServiceErrors(ex); }
            }
            return OperationResult();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Post(Guid risId)
        {
            if (await HasAccessAsync(a => a.AllowPost, "Access Denied!", "Access") && await CanAccessRis(risId))
            {
                try { await _risService.PostAsync(risId, User.Identity.Name, DateTime.Now); }
                catch (Exception exception) { AddServiceErrors(exception); }
            }
            return OperationResult();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Unpost(Guid risId)
        {
            if (await HasAccessAsync(a => a.AllowUnpost, "Access Denied!", "Access") && await CanAccessRis(risId))
            {
                try { await _risService.UnpostAsync(risId, User.Identity.Name, DateTime.Now); }
                catch (Exception exception) { AddServiceErrors(exception); }
            }
            return OperationResult();
        }

        public async Task<ActionResult> Items(Guid risId)
        {
            if (!await CanAccessRis(risId)) return HttpNotFound();
            ViewData["risId"] = risId;
            return PartialView("~/Views/RIS/_RISItem.cshtml");
        }

        public async Task<ActionResult> ItemsRead([DataSourceRequest] DataSourceRequest request, Guid? risId)
        {
            if (!risId.HasValue || !await CanAccessRis(risId.Value)) return HttpNotFound();
            var data = _risService.RisItem.GetByRisId(risId);
            return new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
        }

        public JsonResult GetModelDefault()
        {
            var code = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY")
                .AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();
            var model = new RIS_VM
            {
                RisDate = DateTime.Now,
                IssuedBy = code == null ? null : code.Description,
                IssuedByDesignation = code == null ? null : code.Desc2
            };
            return Json(new { model = model }, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> Print(string ctrlNo)
        {
            var visible = await _risService.GetAllAsync(User.Identity.GetUserId());
            if (!await visible.AnyAsync(x => x.CtrlNo == ctrlNo && x.IsPosted)) return HttpNotFound();
            return RedirectToAction("RISRpt", "RIS", new { ctrlNo = ctrlNo });
        }

        public async Task<ActionResult> PrintDepartment(string ctrlNo)
        {
            var visible = await _risService.GetAllAsync(User.Identity.GetUserId());
            if (!await visible.AnyAsync(x => x.CtrlNo == ctrlNo && x.IsPosted)) return HttpNotFound();
            return PartialView("~/Views/RIS/_PrintRisDepartment.cshtml",
                new RisPrintVM { CtrlNo = ctrlNo, AsOfDate = DateTime.Now });
        }


        public async Task<ActionResult> Wizard()
        {
            if (!await HasAccessAsync(a => a.AllowAdd && a.AllowPost, "Create and post access is required."))
                return new HttpStatusCodeResult(403);
            var key = Guid.NewGuid().ToString("N");
            Session["RIS.Submit." + key] = true;
            ViewBag.SubmissionKey = key;
            return View("~/Views/Requisitions/Wizard.cshtml");
        }

        public async Task<ActionResult> Candidates([DataSourceRequest] DataSourceRequest request, string department)
        {
            var query = await ((RisService)_risService).WizardCandidatesAsync(department, User.Identity.GetUserId());
            return Json(query.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> WizardData(Guid orderId, string department)
        {
            try
            {
                var data = await ((RisService)_risService).LoadWizardAsync(orderId, department, User.Identity.GetUserId());
                return new JsonNetResult { Data = data, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            catch (Exception ex)
            {
                AddServiceErrors(ex);
                return Json(new { Error = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)) }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitWizard(string payload, string submissionKey)
        {
            // MVC serializes requests in this session. Only a command token is
            // retained; no header, items or wizard progress is persisted.
            if (string.IsNullOrEmpty(submissionKey) || Session["RIS.Submit." + submissionKey] == null)
            {
                ModelState.AddModelError("", "This submission has already completed or expired. Check the RIS Index.");
                return OperationResult();
            }
            if (await HasAccessAsync(a => a.AllowAdd && a.AllowPost, "Create and post access is required."))
            {
                try
                {
                    var model = JsonConvert.DeserializeObject<RisWizardVM>(payload ?? "");
                    await ((RisService)_risService).SubmitWizardAsync(model, User.Identity.GetUserId(), User.Identity.Name);
                    Session.Remove("RIS.Submit." + submissionKey);
                }
                catch (Exception ex) { AddServiceErrors(ex); }
            }
            return OperationResult();
        }

        public async Task<ActionResult> ViewRis(Guid id)
        {
            var visible = await _risService.GetAllAsync(User.Identity.GetUserId());
            var header = await visible.SingleOrDefaultAsync(x => x.Id == id);
            if (header == null) return HttpNotFound();
            header.RisItems = await _risService.RisItem.GetByRisId(id).OrderBy(x => x.ItemNoIndex).ToListAsync();
            return View("~/Views/Requisitions/ViewRis.cshtml", header);
        }

        private async Task<bool> CanAccessRis(Guid id)
        {
            var visible = await _risService.GetAllAsync(User.Identity.GetUserId());
            if (await visible.AnyAsync(x => x.Id == id)) return true;
            ModelState.AddModelError("", "RIS not found or access denied.");
            return false;
        }

        private async Task<bool> HasAccessAsync(Func<Access, bool> permission, string message, string key = "")
        {
            var access = await Access(User.Identity.GetUserId(), AccessCode);
            if (access != null && permission(access)) return true;
            ModelState.AddModelError(key, message);
            return false;
        }

        private ActionResult GridResult(DataSourceRequest request, RIS_VM model)
        {
            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        private ActionResult OperationResult()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) && e.Exception != null ? e.Exception.Message : e.ErrorMessage)
                .Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            return Json(new { Errors = errors.Count == 0 ? (object)string.Empty : errors }, JsonRequestBehavior.DenyGet);
        }

        private void AddServiceErrors(Exception exception, string key = "")
        {
            var validationException = exception as ValidationException;
            if (validationException != null)
            {
                var invalidModel = validationException.InnerException as InvalidModelException;
                if (invalidModel != null)
                {
                    foreach (var error in validationException.GetErrorsForModelState())
                        ModelState.AddModelError(error.Key, error.Message);
                    return;
                }
                if (validationException.InnerException != null)
                {
                    ModelState.AddModelError(key, validationException.InnerException.Message);
                    return;
                }
            }
            ModelState.AddModelError(key, exception.Message);
        }
    }
}
