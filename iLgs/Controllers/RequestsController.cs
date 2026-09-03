using CrystalDecisions.CrystalReports.Engine;
using iLgs.Ai.Services;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PPMP_;
using iLgs.Services.PurchaseOrder;
using iLgs.Services.PurchaseRequest;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("REQUESTS")]
    public class RequestsController : BaseController
    {
        private readonly IOrderService _orderService;
        private readonly IRequestService _requestService;
        private readonly ICodextnService _codextnService;
        private readonly IPPMPService _ppmpService;

        private string _menuId = string.Empty;
                
        public RequestsController()
        {
            _orderService = new OrderService(_db);
            _requestService = new RequestService(_db);
            _codextnService = new CodextnService(_db);
            _ppmpService = new PPMPService(_db);            
        }
        
        // GET: Requests
        public async Task<ActionResult> Index()
        {
            _menuId = "requests";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }
            TempData["requests"] = _menuId;

            ViewBag.IsSubmitted = false;
            return View();
        }

        public async Task<ActionResult> Posting()
        {
            _menuId = "requests_posting";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }
            TempData["requests"] = _menuId;

            ViewBag.IsSubmitted = true;
            return View("Index");
        }

        public async Task<ActionResult> RequestRead([DataSourceRequest] DataSourceRequest request, bool? isSubmitted)
        {
            var userId = User.Identity.GetUserId();
            var data = isSubmitted.HasValue
                ? await _requestService.GetAllAsync(userId, isSubmitted)
                : await _requestService.GetAllAsync(userId);

            if (isSubmitted == true)
            {
                data = data.OrderBy(o => o.PostedDt).ThenBy(o => o.SubmittedDt).ThenBy(o => o.CtrlNo);
            }
            else
            {
                data = data.OrderByDescending(o => o.PrDate)
                    .ThenByDescending(o => o.InsertedDt);
            }

            var requests = await data.ToListAsync();
            var requestIds = requests.Select(x => x.Id).ToList();
            var histories = await _db.DocumentStatusHistories
                .AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && requestIds.Contains(x.DocumentId))
                .OrderByDescending(x => x.ChangedDt)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
            var latestHistories = histories
                .GroupBy(x => x.DocumentId)
                .ToDictionary(x => x.Key, x => x.First());

            foreach (var purchaseRequest in requests)
            {
                DocumentStatusHistory latestHistory;
                if (latestHistories.TryGetValue(purchaseRequest.Id, out latestHistory) &&
                    !String.IsNullOrWhiteSpace(latestHistory.ToStatus))
                {
                    purchaseRequest.Status = NormalizePrStatus(latestHistory.ToStatus);
                    purchaseRequest.StatusRemarks = latestHistory.Remarks;
                }
                else if (!String.IsNullOrWhiteSpace(purchaseRequest.PostedBy) ||
                    purchaseRequest.PostedDt.HasValue)
                {
                    purchaseRequest.Status = PrStatuses.Posted;
                }
                else
                {
                    purchaseRequest.Status = !String.IsNullOrWhiteSpace(purchaseRequest.SubmittedBy)
                        ? PrStatuses.Submitted
                        : PrStatuses.Draft;
                }
            }

            if (isSubmitted == true)
            {
                requests = requests.Where(x => x.Status == PrStatuses.Submitted).ToList();
            }

            var result = new JsonNetResult
            {
                Data = requests.AsQueryable().ToDataSourceResult(request),
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
                _menuId = TempData["requests"]?.ToString();
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);                
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }
                                
                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestService.CreateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RequestUpdate([DataSourceRequest] DataSourceRequest request, RequestVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests", "requests_posting");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Update Access Denied!");
                }

                var existingRequest = await _db.Requests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == model.Id);
                var currentStatus = await GetRequestStatusAsync(existingRequest);
                if (existingRequest == null || currentStatus != PrStatuses.Draft)
                {
                    ModelState.AddModelError("Status", "Only a new Draft can be edited here. Returned Purchase Requests must use Revise PR.");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestService.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [HttpGet]
        public async Task<ActionResult> Review(Guid id)
        {
            var access = await Access(User.Identity.GetUserId(), "requests_posting");
            if (!access.IsAllowed || !access.AllowPost)
            {
                return new HttpStatusCodeResult(403, "Purchase Request review access denied.");
            }

            var entity = await _db.Requests
                .AsNoTracking()
                .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                .FirstOrDefaultAsync(x => x.Id == id);
            var status = await GetRequestStatusAsync(entity);
            if (entity == null || status != PrStatuses.Submitted || entity.PostedDt.HasValue)
            {
                return HttpNotFound("Only a Submitted Purchase Request can be reviewed.");
            }

            var model = new PurchaseRequestReviewViewModel
            {
                Id = entity.Id,
                CtrlNo = entity.CtrlNo,
                PrNo = entity.PrNo,
                PrDate = entity.PrDate,
                Department = entity.Department,
                Section = entity.Section,
                Purpose = entity.Purpose,
                FPP = entity.FPP,
                Fund = entity.Fund,
                FundSpecific = entity.FundSpecific,
                RequestedBy = entity.RequestedBy,
                RequestedDesig = entity.RequestedDesig,
                Availability = entity.Availability,
                AvaialbilityDesig = entity.AvaialbilityDesig,
                ApprovedBy = entity.ApprovedBy,
                ApprovedDesig = entity.ApprovedDesig,
                SubmittedBy = entity.SubmittedBy,
                SubmittedDt = entity.SubmittedDt,
                Status = status,
                StatusRemarks = (await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == entity.Id)
                    .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                    .Select(x => x.Remarks).FirstOrDefaultAsync()),
                EstimatedTotal = entity.RequestItems.Sum(x => x.TotalCost) ?? 0,
                Items = entity.RequestItems.OrderBy(x => x.ItemNoIndex).ThenBy(x => x.ItemNo).Select(item =>
                    new PurchaseRequestReviewItemViewModel
                    {
                        Id = item.Id,
                        ItemNo = item.ItemNo,
                        PpmpCode = item.PpmpCode,
                        Description = item.Description,
                        OtherDesc = item.OtherDesc,
                        Unit = item.Unit,
                        Qty = item.Qty,
                        UnitCost = item.UnitCost,
                        TotalCost = item.TotalCost,
                        SubItems = item.RequestSubItems.OrderBy(x => x.ItemNoIndex).ThenBy(x => x.ItemNo).Select(subItem =>
                            new PurchaseRequestReviewSubItemViewModel
                            {
                                ItemNo = subItem.ItemNo,
                                Description = subItem.Description,
                                Unit = subItem.Unit,
                                Qty = subItem.Qty,
                                UnitCost = subItem.UnitCost,
                                Total = subItem.Total
                            }).ToList()
                    }).ToList()
            };

            return PartialView("_Review", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostPurchaseRequest(PostPurchaseRequestViewModel model)
        {
            var access = await Access(User.Identity.GetUserId(), "requests_posting");
            if (!access.IsAllowed || !access.AllowPost)
                return Json(new { success = false, message = "Posting access denied." });

            if (!ModelState.IsValid || model.RequestId == Guid.Empty)
                return Json(new { success = false, message = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault() ?? "Please complete the required fields." });

            var prNumber = model.PrNumber.Trim();
            using (var transaction = _db.Database.BeginTransaction())
            {
                var entity = await _db.Requests.FirstOrDefaultAsync(x => x.Id == model.RequestId);
                var status = await GetRequestStatusAsync(entity);
                if (entity == null || status != PrStatuses.Submitted || entity.PostedDt.HasValue || !String.IsNullOrWhiteSpace(entity.PostedBy))
                    return Json(new { success = false, message = "Only a Submitted Purchase Request can be posted." });

                if (await _db.Requests.AnyAsync(x => x.Id != entity.Id && x.PrNo == prNumber))
                    return Json(new { success = false, message = "PR Number already exists." });

                if (String.IsNullOrWhiteSpace(entity.Availability) || String.IsNullOrWhiteSpace(entity.ApprovedBy))
                    return Json(new { success = false, message = "Cash availability and Approved By are required before posting." });

                var now = DateTime.Now;
                var user = User.Identity.Name;
                entity.PrNo = prNumber;
                entity.PrDate = model.PrDate.Value.Date;
                entity.PostedBy = user;
                entity.PostedDt = now;
                entity.UpdatedBy = user;
                entity.UpdatedDt = now;

                var usages = await _db.PPMPItemUsages.Where(x => x.PrId == entity.Id && x.Type != "PR").ToListAsync();
                foreach (var usage in usages) { usage.Type = "PR"; usage.Reference = prNumber; }

                new DocumentHistoryService(_db).AddStatusHistory(DocumentTypes.PurchaseRequest, entity.Id,
                    prNumber, PrStatuses.Submitted, PrStatuses.Posted, "Post", "Purchase Request reviewed and posted.", user);
                await _db.SaveChangesAsync();
                transaction.Commit();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReturnForRevision(ReturnPurchaseRequestViewModel model)
        {
            var access = await Access(User.Identity.GetUserId(), "requests_posting");
            if (!access.IsAllowed || !access.AllowPost)
            {
                return Json(new { success = false, message = "Return for revision access denied." });
            }

            if (!ModelState.IsValid || model.RequestId == Guid.Empty || String.IsNullOrWhiteSpace(model.ReviewComment))
            {
                return Json(new { success = false, message = "A review comment is required." });
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                var entity = await _db.Requests.FirstOrDefaultAsync(x => x.Id == model.RequestId);
                var status = await GetRequestStatusAsync(entity);
                if (entity == null || status != PrStatuses.Submitted || entity.PostedDt.HasValue ||
                    !String.IsNullOrWhiteSpace(entity.PostedBy))
                {
                    return Json(new { success = false, message = "Only a Submitted Purchase Request can be returned." });
                }

                if (entity.RequestItems.Any(x => x.OrderItemRequests.Any()))
                {
                    return Json(new { success = false, message = "This Purchase Request is already used by a Purchase Order." });
                }

                var now = DateTime.Now;
                var user = User.Identity.Name;
                entity.UpdatedBy = user;
                entity.UpdatedDt = now;
                new DocumentHistoryService(_db).AddStatusHistory(DocumentTypes.PurchaseRequest, entity.Id,
                    entity.PrNo ?? entity.CtrlNo, PrStatuses.Submitted, PrStatuses.Returned,
                    "Return", model.ReviewComment.Trim(), user);
                await _db.SaveChangesAsync();
                transaction.Commit();
            }

            return Json(new { success = true });
        }

        [NonAction]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResubmitRequest(Guid requestId)
        {
            var access = await Access(User.Identity.GetUserId(), "requests");
            if (!access.IsAllowed || !access.AllowEdit)
            {
                return Json(new { success = false, message = "Resubmit access denied." });
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                var entity = await _db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
                var status = await GetRequestStatusAsync(entity);
                if (entity == null || status != PrStatuses.Returned)
                {
                    return Json(new { success = false, message = "Only a Returned Purchase Request can be resubmitted." });
                }

                if (!access.IsAdmin &&
                    !String.Equals(entity.InsertedBy, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Only the original requester can resubmit this Purchase Request." });
                }

                if (entity.RequestItems.Any(x => x.OrderItemRequests.Any()))
                {
                    return Json(new { success = false, message = "This Purchase Request is already used by a Purchase Order." });
                }

                var now = DateTime.Now;
                var user = User.Identity.Name;
                entity.SubmittedBy = user;
                entity.SubmittedDt = now;
                entity.UpdatedBy = user;
                entity.UpdatedDt = now;
                _db.DocumentStatusHistories.Add(new DocumentStatusHistory
                {
                    Id = Guid.NewGuid(),
                    DocumentType = DocumentTypes.PurchaseRequest,
                    DocumentId = entity.Id,
                    DocumentNo = entity.PrNo ?? entity.CtrlNo,
                    FromStatus = PrStatuses.Returned,
                    ToStatus = PrStatuses.Submitted,
                    Action = "Resubmit",
                    Remarks = "Purchase Request revised and resubmitted.",
                    ChangedBy = user,
                    ChangedDt = now
                });
                await _db.SaveChangesAsync();
                transaction.Commit();
            }

            return Json(new { success = true });
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RequestDestroy([DataSourceRequest]DataSourceRequest request, RequestVM model)
        {
            try
            {
                _menuId = TempData["requests"]?.ToString();
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!await CanReviseRequestAsync(model.Id, access))
                {
                    ModelState.AddModelError("DeleteError", "Only an authorized Draft or Returned Purchase Request can be deleted.");
                }
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestService.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> _RequestItem(Guid requestId)
        {
            var access = await Access(User.Identity.GetUserId(), "requests", "requests_posting");
            ViewBag.CanRevise = await CanReviseRequestAsync(requestId, access);
            ViewData["requestId"] = requestId;
            return PartialView();
        }

        public async Task<ActionResult> _RequestItemAddEdit(Guid prId, Guid? requestItemId, string setLotNo)
        {
            var access = await Access(User.Identity.GetUserId(), "requests", "requests_posting");
            if (!await CanReviseRequestAsync(prId, access))
            {
                return new HttpStatusCodeResult(403, "Only Draft or Returned Purchase Requests can be revised.");
            }

            var data = await _requestService.RequestItem.GetVmByIdAsync(requestItemId);
            if (data == null)
            {
                data = new RequestItemVM()
                {
                    Id = Guid.NewGuid(),
                    PrId = prId
                };
            }
            ViewData["setLotNo"] = setLotNo;
            ViewData["requestItemId"] = requestItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RequestItemSave(RequestItemVM model)
        {
            try
            {
                _menuId = TempData["requests"]?.ToString();
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;

                var entity = await _requestService.RequestItem.GetByIdAsync(model.Id);
                var requestId = entity == null ? model.PrId : entity.PrId;
                if (!requestId.HasValue || !await CanReviseRequestAsync(requestId.Value, access))
                {
                    ModelState.AddModelError("Status", "Only Draft or Returned Purchase Requests can be revised by the original requester.");
                }
                if (entity == null)
                {
                    if (!access.AllowAdd)
                    {
                        ModelState.AddModelError("Access", "Access Denied!");
                    }
                }
                else
                {
                    if (!access.AllowEdit)
                    {
                        ModelState.AddModelError("Access", "Access Denied!");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (entity == null)
                    {
                        model = await _requestService.RequestItem.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _requestService.RequestItem.UpdateAsync(model, user, date);
                    }
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
                       .Select(ms => new
                       {
                           Key = ms.Key, // The field name
                           Message = ms.Value.Errors.Select(e =>
                           {
                               var errorMessage = e.ErrorMessage;
                               if (e.Exception != null)
                               {
                                   var exceptionMessage = e.Exception.Message;
                                   var innerExceptionMessage = e.Exception.InnerException?.Message;

                                   // Append exception details
                                   errorMessage += $" Exception: {exceptionMessage}";
                                   if (innerExceptionMessage != null)
                                   {
                                       errorMessage += $" InnerException: {innerExceptionMessage}";
                                   }
                               }

                               return errorMessage;
                           }).ToList() // List of messages for the current field
                       })
                       .ToList();

            if (errorList.Any())
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _RequestItemRead([DataSourceRequest] DataSourceRequest request, Guid? prId)
        {
            var data = _requestService.RequestItem.GetByPrId(prId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }


        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RequestItemDestroy([DataSourceRequest]DataSourceRequest request, RequestItemVM model)
        {
            try
            {
                _menuId = TempData["requests"]?.ToString();
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!model.PrId.HasValue || !await CanReviseRequestAsync(model.PrId.Value, access))
                {
                    ModelState.AddModelError("DeleteError", "Only Draft or Returned Purchase Request items can be deleted by the original requester.");
                }
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _requestService.RequestItem.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [NonAction]
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostRequest(Guid requestId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests_posting");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.PostAsync(requestId, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests_posting");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.UnpostAsync(requestId, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
        public async Task<ActionResult> SubmitRequest(Guid requestId)
        {
            try
            {
                _menuId = TempData["requests"]?.ToString();
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.SubmitAsync(requestId, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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
        public async Task<ActionResult> UnsubmitRequest(Guid requestId)
        {
            try
            {
                _menuId = TempData["requests"]?.ToString();
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.UnsubmitAsync(requestId, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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

        //#region UNIT GROUP
        //public ActionResult _UnitGroup(Guid requestId)
        //{
        //    ViewData["requestId"] = requestId;
        //    return PartialView();
        //}

        //public ActionResult _UnitGroupRead([DataSourceRequest] DataSourceRequest request, Guid? requestId)
        //{
        //    var data = _unitGroupService.GetByPrId(requestId);

        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> _UnitGroupUpdate([DataSourceRequest] DataSourceRequest request, RequestItemUnitGroupVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("UpdateError", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _unitGroupService.UpdateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError("UpdateError", error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> _UnitGroupDestroy([DataSourceRequest]DataSourceRequest request, RequestItemUnitGroupVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
        //        Access access = await accessTask;
        //        if (!access.AllowDelete)
        //        {
        //            ModelState.AddModelError("DeleteError", "Delete Access Denied!");
        //        }
        //        else
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _unitGroupService.DeleteAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("DeleteError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //#endregion

        //#region UNIT GROUP DESCRIPTION
        //public ActionResult _UnitGroupDescription(Guid unitGroupId)
        //{
        //    ViewData["unitGroupId"] = unitGroupId;
        //    return PartialView();
        //}

        //public ActionResult _UnitGroupDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        //{
        //    var data = _unitGroupDescriptionService.GetByUnitGroupId(unitGroupId);

        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> _UnitGroupDescriptionUpdate([DataSourceRequest] DataSourceRequest request, RequestItemUnitGroupDescriptionVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("UpdateError", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _unitGroupDescriptionService.UpdateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError("UpdateError", error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        //#endregion

        //#region UNIT GROUP DESCRIPTION ITEMS        
        //public ActionResult _UnitGroupDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        //{
        //    var data = _unitGroupDescriptionItemService.GetByUnitGroupDescriptionId(unitGroupDescriptionId);

        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> _UnitGroupDescriptionItemUpdate([DataSourceRequest] DataSourceRequest request, RequestItemUnitGroupDescriptionItemVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("UpdateError", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _unitGroupDescriptionItemService.UpdateAsync(model, user, date);
        //        }
        //    }
        //    catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
        //    {
        //        var errors = validationException.GetErrorsForModelState();
        //        foreach (var error in errors)
        //        {
        //            ModelState.AddModelError("UpdateError", error.Message);
        //        }
        //    }
        //    catch (ValidationException validationException)
        //    {
        //        ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}
        //#endregion

        #region PRINTOUTS
        public ActionResult PurchaseRequestRpt(string ctrlNo)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Pr_.rpt"));
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
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
                table.ApplyLogOnInfo(logonInfo);
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("@cCtrlNo", ctrlNo);
            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
        #endregion

        [HttpPost]
        public JsonResult GetModelDefault()
        {
            var approved = _db.Codextns.Where(w => w.CodeMast.Code == "APPROVED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();
            var availability = _db.Codextns.Where(w => w.CodeMast.Code == "CASH-AVAILABLE").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();

            var model = new RequestVM()
            {
                ApprovedBy = approved?.Description,
                ApprovedDesig = approved?.Desc2,
                Availability = availability?.Description,
                AvaialbilityDesig = availability?.Desc2
            };

            return Json(new { model }, JsonRequestBehavior.AllowGet);
        }

        #region PPMP ITEMS
        public async Task<ActionResult> _PpmpItems(Guid prId)
        {
            var data = await _requestService.GetByIdAsync(prId);
            
            ViewData["prId"] = prId;            
            return PartialView(data);
        }

        [HttpPost]
        public async Task<ActionResult> _PpmpItemSelectionSave(Guid? prId, string selectedIds)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "request");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }
                else
                {
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.RequestItem.CreateFromPpmpItemAsync(prId, selectedIds, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
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

        public ActionResult _PpmpItemRead([DataSourceRequest] DataSourceRequest request, Guid? prId)
        {
            var data = _ppmpService.PPMPItem.GetAvailableByRequestId(prId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }
        #endregion PPMP ITEMS

        private async Task<string> GetRequestStatusAsync(Request entity)
        {
            if (entity == null)
            {
                return null;
            }

            var historyStatus = await _db.DocumentStatusHistories
                .AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == entity.Id)
                .OrderByDescending(x => x.ChangedDt)
                .ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus)
                .FirstOrDefaultAsync();

            if (!String.IsNullOrWhiteSpace(historyStatus))
            {
                return NormalizePrStatus(historyStatus);
            }

            if (!String.IsNullOrWhiteSpace(entity.PostedBy) || entity.PostedDt.HasValue)
            {
                return PrStatuses.Posted;
            }

            return !String.IsNullOrWhiteSpace(entity.SubmittedBy)
                ? PrStatuses.Submitted
                : PrStatuses.Draft;
        }

        private async Task<bool> CanReviseRequestAsync(Guid requestId, Access access)
        {
            var entity = await _db.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == requestId);
            var status = await GetRequestStatusAsync(entity);
            if (entity == null || status != PrStatuses.Draft)
            {
                return false;
            }

            return status == PrStatuses.Draft || access.IsAdmin ||
                String.Equals(entity.InsertedBy, User.Identity.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePrStatus(string status)
        {
            switch ((status ?? String.Empty).Trim().ToUpperInvariant())
            {
                case "SUBMITTED":
                    return PrStatuses.Submitted;
                case "RETURNED":
                    return PrStatuses.Returned;
                case "REVISING":
                    return PrStatuses.Revising;
                case "POSTED":
                    return PrStatuses.Posted;
                case "CANCELLED":
                    return PrStatuses.Cancelled;
                default:
                    return PrStatuses.Draft;
            }
        }
    }
}
