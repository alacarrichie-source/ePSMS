using System.Collections;
using System.Collections.Generic;
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

            var postingAccess = await Access(User.Identity.GetUserId(), "requests_posting", _menuId);
            ViewBag.AllowUnpost = postingAccess.IsAllowed && postingAccess.AllowUnpost;

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

            ViewBag.AllowUnpost = access.IsAllowed && access.AllowUnpost;

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

            var result = data.ToDataSourceResult(request);
            var _dataEnum = result.Data as System.Collections.IEnumerable; var pageItems = _dataEnum != null ? _dataEnum.OfType<RequestVM>().ToList() : null;
            if (pageItems != null && pageItems.Any())
            {
                var pageIds = pageItems.Select(x => x.Id).ToList();
                var returnedRemarks = await _db.DocumentStatusHistories
                    .AsNoTracking()
                    .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && pageIds.Contains(x.DocumentId) && x.ToStatus == PrStatuses.Returned)
                    .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                    .Select(x => new { x.DocumentId, x.Remarks, x.ChangedBy, x.ChangedDt })
                    .ToListAsync();

                var remarksMap = returnedRemarks
                    .GroupBy(x => x.DocumentId)
                    .ToDictionary(x => x.Key, x => x.First());

                var currentUserId = User.Identity.GetUserId();
                var currentUserName = User.Identity.Name;
                var postingAccess = await Access(currentUserId, "requests_posting");
                var mainAccess = await Access(currentUserId, "requests");
                foreach (var purchaseRequest in pageItems)
                {
                    if (!string.IsNullOrWhiteSpace(purchaseRequest.Status))
                    {
                        purchaseRequest.Status = NormalizePrStatus(purchaseRequest.Status);
                    }
                    else if (!string.IsNullOrWhiteSpace(purchaseRequest.PostedBy) || purchaseRequest.PostedDt.HasValue)
                    {
                        purchaseRequest.Status = PrStatuses.Posted;
                    }
                    else
                    {
                        purchaseRequest.Status = !string.IsNullOrWhiteSpace(purchaseRequest.SubmittedBy) ? PrStatuses.Submitted : PrStatuses.Draft;
                    }

                    if (remarksMap.ContainsKey(purchaseRequest.Id))
                    {
                        var hist = remarksMap[purchaseRequest.Id];
                        purchaseRequest.StatusRemarks = hist.Remarks;
                        purchaseRequest.StatusUser = hist.ChangedBy;
                        purchaseRequest.StatusDate = hist.ChangedDt;
                }
                    var isRequester = string.Equals(purchaseRequest.InsertedBy, currentUserName, StringComparison.OrdinalIgnoreCase);
                    var prStatus = purchaseRequest.Status ?? "";

                    purchaseRequest.CanView = true;
                    if (isSubmitted == true)
                    {
                        bool isSubmittedOrUnposted = string.Equals(prStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase) ||
                                                    string.Equals(prStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase);
                        bool isPosted = string.Equals(prStatus, PrStatuses.Posted, StringComparison.OrdinalIgnoreCase);

                        if (isSubmittedOrUnposted)
                        {
                            purchaseRequest.CanReview = postingAccess.IsAllowed;
                            purchaseRequest.CanPost = postingAccess.IsAllowed && postingAccess.AllowPost;
                            purchaseRequest.CanAdminEdit = postingAccess.IsAllowed && (postingAccess.IsAdmin || postingAccess.AllowEdit || postingAccess.AllowPost);
                            purchaseRequest.CanReturnForRevision = postingAccess.IsAllowed && postingAccess.AllowPost;
                        }
                        else if (isPosted)
                        {
                            purchaseRequest.CanPrint = true;
                            purchaseRequest.CanUnpost = postingAccess.IsAllowed && postingAccess.AllowUnpost;
                        }
                    }
                    else
                    {
                        // Department directory: read-only View/Print are department-wide.
                        // Requester-side mutations belong only to the original requester.
                        purchaseRequest.CanPrint = true;

                        if (string.Equals(prStatus, PrStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                        {
                            purchaseRequest.CanContinue = isRequester && mainAccess.IsAllowed && mainAccess.AllowEdit;
                            purchaseRequest.CanDelete = isRequester && mainAccess.IsAllowed && mainAccess.AllowDelete;
                        }
                        else if (string.Equals(prStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
                        {
                            purchaseRequest.CanRecallSubmission = isRequester && mainAccess.IsAllowed && mainAccess.AllowEdit;
                        }
                        else if (string.Equals(prStatus, PrStatuses.Returned, StringComparison.OrdinalIgnoreCase))
                        {
                            purchaseRequest.CanRevise = isRequester && mainAccess.IsAllowed && mainAccess.AllowEdit;
                        }
                        else if (string.Equals(prStatus, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase))
                        {
                            purchaseRequest.CanContinue = isRequester && mainAccess.IsAllowed && mainAccess.AllowEdit;
                        }
                    }
                }
            }

            return new JsonNetResult
            {
                Data = result,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
        }
        [HttpGet]
        public async Task<ActionResult> GetStatusCounts(bool? isSubmitted)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var data = isSubmitted.HasValue
                    ? await _requestService.GetAllAsync(userId, isSubmitted)
                    : await _requestService.GetAllAsync(userId);

                var items = await data.Select(x => new
                {
                    x.Id,
                    x.Status,
                    x.Department,
                    x.PostedBy,
                    x.PostedDt,
                    x.SubmittedBy,
                    x.SubmittedDt
                }).ToListAsync();

                int draftCount = 0;
                int submittedCount = 0;
                int returnedCount = 0;
                int revisingCount = 0;
                int unpostedCount = 0;
                int postedCount = 0;

                var departments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in items)
                {
                    string st;
                    if (!string.IsNullOrWhiteSpace(item.Status))
                    {
                        st = NormalizePrStatus(item.Status);
                    }
                    else if (!string.IsNullOrWhiteSpace(item.PostedBy) || item.PostedDt.HasValue)
                    {
                        st = PrStatuses.Posted;
                    }
                    else if (!string.IsNullOrWhiteSpace(item.SubmittedBy) || item.SubmittedDt.HasValue)
                    {
                        st = PrStatuses.Submitted;
                    }
                    else
                    {
                        st = PrStatuses.Draft;
                    }

                    if (string.Equals(st, PrStatuses.Draft, StringComparison.OrdinalIgnoreCase)) draftCount++;
                    else if (string.Equals(st, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase)) submittedCount++;
                    else if (string.Equals(st, PrStatuses.Returned, StringComparison.OrdinalIgnoreCase)) returnedCount++;
                    else if (string.Equals(st, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase)) revisingCount++;
                    else if (string.Equals(st, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase)) unpostedCount++;
                    else if (string.Equals(st, PrStatuses.Posted, StringComparison.OrdinalIgnoreCase)) postedCount++;

                    if (!string.IsNullOrWhiteSpace(item.Department))
                    {
                        departments.Add(item.Department.Trim());
                    }
                }

                int totalCount = isSubmitted == true
                    ? (submittedCount + unpostedCount + returnedCount + revisingCount + postedCount)
                    : items.Count;

                return Json(new
                {
                    success = true,
                    total = totalCount,
                    draft = draftCount,
                    submitted = submittedCount,
                    returned = returnedCount,
                    revising = revisingCount,
                    unposted = unpostedCount,
                    posted = postedCount,
                    departments = departments.OrderBy(d => d).ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> MyDrafts_Read([DataSourceRequest] DataSourceRequest request)
        {
            var userId = User.Identity.GetUserId();
            var userName = User.Identity.Name;
            var mainAccess = await Access(userId, "requests");
            var data = await _requestService.GetAllAsync(userId);
            var drafts = data.Where(w => (w.SubmittedBy == null || w.SubmittedBy == "") &&
                                         (w.PostedBy == null || w.PostedBy == "") &&
                                         string.Equals(w.InsertedBy, userName, StringComparison.OrdinalIgnoreCase))
                             .OrderByDescending(o => o.InsertedDt ?? o.PrDate);

            var result = drafts.ToDataSourceResult(request);
            var _draftsEnum = result.Data as System.Collections.IEnumerable;
            var pageDrafts = _draftsEnum != null ? _draftsEnum.OfType<RequestVM>().ToList() : null;
            if (pageDrafts != null)
            {
                foreach (var d in pageDrafts)
                {
                    d.CanView = true;
                    d.CanContinue = true;
                    d.CanDelete = mainAccess.AllowDelete;
                }
            }

            return new JsonNetResult
            {
                Data = result,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
        }

        [HttpGet]
        public async Task<ActionResult> ContinueDraftInfo(Guid id)
        {
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, "requests");

            if (!access.IsAllowed || !access.AllowEdit || !await CanEditDraftRequestAsync(id, access))
            {
                return Json(new
                {
                    success = false,
                    message = "Only the original requester may continue this Draft Purchase Request."
                }, JsonRequestBehavior.AllowGet);
            }

            var cartService = new ProcurementCartService(_db);
            var currentCart = await cartService.GetActiveWorkingNormalCartEntityAsync(userId);
            var currentCartCount = currentCart == null || currentCart.ProcurementCartItems == null
                ? 0
                : currentCart.ProcurementCartItems.Count;

            var draftInfo = await _db.Requests
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.CtrlNo,
                    ItemCount = x.RequestItems.Count()
                })
                .FirstOrDefaultAsync();

            if (draftInfo == null)
            {
                return Json(new
                {
                    success = false,
                    message = "The Draft Purchase Request no longer exists."
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                currentCartCount = currentCartCount,
                currentCartIsSameDraft = currentCart != null && currentCart.RequestId == id,
                draftItemCount = draftInfo.ItemCount,
                reference = draftInfo.CtrlNo ?? "Draft Purchase Request"
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ContinueDraft(Guid id, bool confirmReplace)
        {
            var userId = User.Identity.GetUserId();
            var access = await Access(userId, "requests");

            if (!access.IsAllowed || !access.AllowEdit || !await CanEditDraftRequestAsync(id, access))
            {
                return Json(new
                {
                    success = false,
                    message = "Only the original requester may continue this Draft Purchase Request."
                });
            }

            if (!confirmReplace)
            {
                return Json(new
                {
                    success = false,
                    message = "Please confirm that this Draft Purchase Request should become the current My Cart."
                });
            }

            try
            {
                var cartService = new ProcurementCartService(_db);
                var draftCart = await cartService.StartDraftCartAsync(
                    id,
                    userId,
                    User.Identity.Name,
                    access);

                return Json(new
                {
                    success = true,
                    mode = "CART",
                    cartCount = draftCart.ProcurementCartItems == null
                        ? 0
                        : draftCart.ProcurementCartItems.Count,
                    redirectUrl = Url.Action("Cart", "Procurement")
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RequestCreate([DataSourceRequest] DataSourceRequest request, RequestVM model)
        {
            try
            {
                _menuId = (TempData["requests"] != null ? TempData["requests"].ToString() : null);
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
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                var isDraftEdit = await CanEditDraftRequestAsync(model.Id, access);
                if (!isDraftEdit)
                {
                    ModelState.AddModelError("Status", "Only an authorized Draft Purchase Request can be edited directly.");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await new PurchaseRequestLifecycleService(_db).EnsureEditableAsync(model.Id, allowAdminEdit: false);
                    model = await _requestService.UpdateAsync(model, user, date, allowAdminEdit: false);
                }


            }
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

                [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public async Task<ActionResult> StartAdminEdit(Guid id)
        {
            _menuId = "requests_posting";
            var access = await Access(User.Identity.GetUserId(), _menuId, "requests");
            if (!access.IsAllowed || (!access.IsAdmin && !access.AllowEdit && !access.AllowPost))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Admin Edit access denied." }, JsonRequestBehavior.AllowGet);
                }
                TempData["Error"] = "Admin Edit access denied.";
                return RedirectToAction("Posting");
            }

            if (!await CanAdminEditRequestAsync(id, access))
            {
                var msg = "Admin edit is only permitted for Submitted or Unposted Purchase Requests.";
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = msg }, JsonRequestBehavior.AllowGet);
                }
                TempData["Error"] = msg;
                return RedirectToAction("Posting");
            }

            try
            {
                var lifecycle = new PurchaseRequestLifecycleService(_db);
                await lifecycle.EnsureEditableAsync(id, allowAdminEdit: true);

                var cartService = new ProcurementCartService(_db);
                var adminCart = await cartService.StartAdminEditCartAsync(id, User.Identity.GetUserId(), User.Identity.Name, access);


                var redirectUrl = Url.Action("Cart", "Procurement", new { mode = CartModes.AdminEdit, requestId = id, cartId = adminCart.Id });
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, redirectUrl = redirectUrl }, JsonRequestBehavior.AllowGet);
                }
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
                TempData["Error"] = ex.Message;
                return RedirectToAction("Posting");
            }
        }

        private async Task<PurchaseRequestReviewViewModel> LoadPurchaseRequestReviewModelAsync(Guid id)
        {
            var entity = await _db.Requests
                .AsNoTracking()
                .Include(x => x.RequestItems.Select(i => i.RequestSubItems))
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }

            var status = await GetRequestStatusAsync(entity);

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

            return model;
        }

        [HttpGet]
        public async Task<ActionResult> Review(Guid id, bool? viewOnly = false)
        {
            if (viewOnly == true)
            {
                return await ViewRequest(id);
            }

            var access = await Access(User.Identity.GetUserId(), "requests_posting");
            if (!access.IsAllowed || !access.AllowPost)
            {
                return new HttpStatusCodeResult(403, "Purchase Request review access denied.");
            }

            var model = await LoadPurchaseRequestReviewModelAsync(id);
            if (model == null || (model.Status != PrStatuses.Submitted && model.Status != PrStatuses.Unposted))
            {
                return HttpNotFound("Only a Submitted or Unposted Purchase Request can be reviewed.");
            }

            model.IsViewOnly = false;
            ViewBag.IsViewOnly = false;
            return PartialView("_Review", model);
        }

        [HttpGet]
        [ActionName("View")]
        public async Task<ActionResult> ViewRequest(Guid id)
        {
            if (!await CanAccessRequestRecordAsync(id))
            {
                return new HttpStatusCodeResult(403, "You are not authorized to view this Purchase Request.");
            }

            var model = await LoadPurchaseRequestReviewModelAsync(id);
            if (model == null)
            {
                return HttpNotFound("Purchase Request not found.");
            }

            var postingAccess = await Access(User.Identity.GetUserId(), "requests_posting", "requests");
            ViewBag.AllowUnpost = postingAccess.IsAllowed && postingAccess.AllowUnpost;
            model.IsViewOnly = true;
            ViewBag.IsViewOnly = true;
            return PartialView("_Review", model);
        }

        [HttpGet]
        public async Task<ActionResult> GetReturnComment(Guid id)
        {
            if (!await CanAccessRequestRecordAsync(id))
            {
                return Json(new { success = false, message = "You are not authorized to view this Purchase Request." }, JsonRequestBehavior.AllowGet);
            }

            var history = await _db.DocumentStatusHistories
                .AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == id && x.ToStatus == PrStatuses.Returned)
                .OrderByDescending(x => x.ChangedDt).ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    Remarks = x.Remarks,
                    ReturnedBy = x.ChangedBy,
                    ReturnedDate = x.ChangedDt
                })
                .FirstOrDefaultAsync();

            if (history == null)
            {
                return Json(new { success = false, message = "No review comment found." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                remarks = history.Remarks,
                returnedBy = history.ReturnedBy,
                returnedDate = history.ReturnedDate.ToString("MMM dd, yyyy hh:mm tt")
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RecallSubmission(Guid id)
        {
            var access = await Access(User.Identity.GetUserId(), "requests");
            if (!access.IsAllowed || !access.AllowEdit)
            {
                return Json(new { success = false, message = "Recall Submission access denied." });
            }

            try
            {
                var service = new PurchaseRequestLifecycleService(_db);
                await service.RecallSubmissionAsync(id, User.Identity.Name, DateTime.Now);

                return Json(new
                {
                    success = true,
                    message = "Purchase Request submission recalled. The request is now a Draft and may be continued."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostPurchaseRequest(PostPurchaseRequestViewModel model)
        {
            var access = await Access(User.Identity.GetUserId(), "requests_posting");
            if (!access.IsAllowed || !access.AllowPost)
                return Json(new { success = false, message = "Posting access denied." });

            if (model.RequestId == Guid.Empty)
                return Json(new { success = false, message = "Invalid Request ID." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (errors.Any())
                    return Json(new { success = false, message = string.Join(" ", errors) });
            }


            if (!model.PrDate.HasValue)
                return Json(new { success = false, message = "PR Date is required." });

            try
            {
                var service = new PurchaseRequestLifecycleService(_db);
                var pr = await service.PostAsync(model.RequestId, model.PrNumber, model.PrDate.Value, User.Identity.Name, DateTime.Now);
                return Json(new { success = true, prNumber = pr.PrNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

            if (!ModelState.IsValid || model.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(model.ReviewComment))
            {
                return Json(new { success = false, message = "A review comment is required." });
            }

            try
            {
                var service = new PurchaseRequestLifecycleService(_db);
                await service.ReturnForRevisionAsync(model.RequestId, model.ReviewComment, User.Identity.Name, DateTime.Now);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

                if (!String.Equals(entity.InsertedBy, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
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

[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RequestDestroy([DataSourceRequest]DataSourceRequest request, RequestVM model)
        {
            try
            {
                _menuId = (TempData["requests"] != null ? TempData["requests"].ToString() : null);
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!await CanEditDraftRequestAsync(model.Id, access))
                {
                    ModelState.AddModelError("DeleteError", "Only an authorized Draft Purchase Request can be deleted.");
                }
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await new PurchaseRequestLifecycleService(_db).EnsureEditableAsync(model.Id);
                    model = await _requestService.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
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
            ViewBag.CanRevise = await CanEditDraftRequestAsync(requestId, access);
            ViewData["requestId"] = requestId;
            return PartialView();
        }

        public async Task<ActionResult> _RequestItemAddEdit(Guid prId, Guid? requestItemId, string setLotNo)
        {
            var access = await Access(User.Identity.GetUserId(), "requests", "requests_posting");
            if (!await CanEditDraftRequestAsync(prId, access))
            {
                return new HttpStatusCodeResult(403, "Only an authorized Draft Purchase Request can be edited here.");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> _RequestItemSave(RequestItemVM model)
        {
            try
            {
                _menuId = (TempData["requests"] != null ? TempData["requests"].ToString() : null);
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests", "requests_posting");
                Access access = await accessTask;

                var entity = await _requestService.RequestItem.GetByIdAsync(model.Id);
                var requestId = entity == null ? model.PrId : entity.PrId;
                var isDraftEdit = requestId.HasValue && await CanEditDraftRequestAsync(requestId.Value, access);


                if (!isDraftEdit)
                {
                    ModelState.AddModelError("Status", "Only an authorized Draft Purchase Request item can be edited directly.");
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
                    if (requestId.HasValue) { await new PurchaseRequestLifecycleService(_db).EnsureEditableAsync(requestId.Value, allowAdminEdit: false); }
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (entity == null)
                    {
                        model = await _requestService.RequestItem.CreateAsync(model, user, date, allowAdminEdit: false);
                    }
                    else
                    {
                        model = await _requestService.RequestItem.UpdateAsync(model, user, date, allowAdminEdit: false);
                    }
                }
            }
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
                       .Select(ms => new
                       {
                           Key = ms.Key,
                           Message = ms.Value.Errors.Select(e =>
                           {
                               var errorMessage = e.ErrorMessage;
                               if (string.IsNullOrWhiteSpace(errorMessage))
                               {
                                   errorMessage = "An error occurred while saving the item. Please verify your inputs.";
                               }
                               return errorMessage;
                           }).ToList()
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> _RequestItemDestroy([DataSourceRequest]DataSourceRequest request, RequestItemVM model)
        {
            try
            {
                _menuId = (TempData["requests"] != null ? TempData["requests"].ToString() : null);
                TempData.Keep("requests");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests", "requests_posting");
                Access access = await accessTask;
                var isDraftEdit = model.PrId.HasValue && await CanEditDraftRequestAsync(model.PrId.Value, access);


                if (!isDraftEdit)
                {
                    ModelState.AddModelError("DeleteError", "Only an authorized Draft Purchase Request item can be deleted directly.");
                }
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (model.PrId.HasValue) { await new PurchaseRequestLifecycleService(_db).EnsureEditableAsync(model.PrId.Value, allowAdminEdit: false); }
                    model = await _requestService.RequestItem.DeleteAsync(model, user, date, allowAdminEdit: false);
                }
            }
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
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
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UnpostPurchaseRequest(Guid requestId)
        {
            try
            {
                var access = await Access(User.Identity.GetUserId(), "requests_posting", "requests");
                if (!access.IsAllowed || !access.AllowUnpost)
                {
                    return Json(new { success = false, message = "Unpost access denied." });
                }

                var service = new PurchaseRequestLifecycleService(_db);
                var pr = await service.UnpostAsync(requestId, User.Identity.Name, DateTime.Now);
                var prNo = !string.IsNullOrWhiteSpace(pr.PrNo) ? pr.PrNo.Trim() : (!string.IsNullOrWhiteSpace(pr.CtrlNo) ? pr.CtrlNo.Trim() : "");

                return Json(new
                {
                    success = true,
                    message = string.Format("Purchase Request {0} has been unposted successfully.", prNo)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [NonAction]
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnpostRequest(Guid requestId)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests_posting", "requests");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var service = new PurchaseRequestLifecycleService(_db);
                    var pr = await service.UnpostAsync(requestId, user, date);
                    var prNo = !string.IsNullOrWhiteSpace(pr.PrNo) ? pr.PrNo.Trim() : (!string.IsNullOrWhiteSpace(pr.CtrlNo) ? pr.CtrlNo.Trim() : "");

                    return Json(new
                    {
                        success = true,
                        message = string.Format("Purchase Request {0} has been unposted and returned to the review stage.", prNo),
                        Errors = ""
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            var firstError = errorList.FirstOrDefault() ?? "The operation was blocked.";
            if (errorList.Count() > 0)
            {
                return Json(new { success = false, message = firstError, Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { success = true, message = "Purchase Request unposted.", Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> SubmitRequest(Guid requestId)
        {
            try
            {
                _menuId = (TempData["requests"] != null ? TempData["requests"].ToString() : null);
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
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
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

        [NonAction]
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> UnsubmitRequest(Guid requestId)
        {
            try
            {
                _menuId = (TempData["requests"] != null ? TempData["requests"].ToString() : null);
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
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
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
        // 
        //#region UNIT GROUP
        //public ActionResult _UnitGroup(Guid requestId)
        //{
        //    ViewData["requestId"] = requestId;
        //    return PartialView();
        //}
        // 
        //public ActionResult _UnitGroupRead([DataSourceRequest] DataSourceRequest request, Guid? requestId)
        //{
        //    var data = _unitGroupService.GetByPrId(requestId);
        // 
        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}
        // 
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
        // 
        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;
        // 
        //            model = await _unitGroupService.UpdateAsync(model, user, date);
        //        }
        //    }
        // catch (ValidationException validationException)
        // {
        // var errors = validationException.GetErrorsForModelState();
        // if (errors != null && errors.Any())
        // {
        // foreach (var error in errors)
        // {
        // ModelState.AddModelError(error.Item1, error.Item2);
        // }
        // }
        // else
        // {
        // ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
        // }
        // }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }
        // 
        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}
        // 
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
        // 
        //            model = await _unitGroupService.DeleteAsync(model, user, date);
        //        }
        //    }
        // catch (ValidationException validationException)
        // {
        // var errors = validationException.GetErrorsForModelState();
        // if (errors != null && errors.Any())
        // {
        // foreach (var error in errors)
        // {
        // ModelState.AddModelError(error.Item1, error.Item2);
        // }
        // }
        // else
        // {
        // ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
        // }
        // }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("DeleteError", e.Message);
        //    }
        // 
        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}
        // 
        //#endregion
        // 
        //#region UNIT GROUP DESCRIPTION
        //public ActionResult _UnitGroupDescription(Guid unitGroupId)
        //{
        //    ViewData["unitGroupId"] = unitGroupId;
        //    return PartialView();
        //}
        // 
        //public ActionResult _UnitGroupDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        //{
        //    var data = _unitGroupDescriptionService.GetByUnitGroupId(unitGroupId);
        // 
        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}
        // 
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
        // 
        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;
        // 
        //            model = await _unitGroupDescriptionService.UpdateAsync(model, user, date);
        //        }
        //    }
        // catch (ValidationException validationException)
        // {
        // var errors = validationException.GetErrorsForModelState();
        // if (errors != null && errors.Any())
        // {
        // foreach (var error in errors)
        // {
        // ModelState.AddModelError(error.Item1, error.Item2);
        // }
        // }
        // else
        // {
        // ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
        // }
        // }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }
        // 
        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}
        // 
        //#endregion
        // 
        //#region UNIT GROUP DESCRIPTION ITEMS        
        //public ActionResult _UnitGroupDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        //{
        //    var data = _unitGroupDescriptionItemService.GetByUnitGroupDescriptionId(unitGroupDescriptionId);
        // 
        //    return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        //}
        // 
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
        // 
        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;
        // 
        //            model = await _unitGroupDescriptionItemService.UpdateAsync(model, user, date);
        //        }
        //    }
        // catch (ValidationException validationException)
        // {
        // var errors = validationException.GetErrorsForModelState();
        // if (errors != null && errors.Any())
        // {
        // foreach (var error in errors)
        // {
        // ModelState.AddModelError(error.Item1, error.Item2);
        // }
        // }
        // else
        // {
        // ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
        // }
        // }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("UpdateError", e.Message);
        //    }
        // 
        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}
        //#endregion
        // 
        #region PRINTOUTS
        public async Task<ActionResult> PurchaseRequestRpt(string ctrlNo)
        {
            if (string.IsNullOrWhiteSpace(ctrlNo))
            {
                return new HttpStatusCodeResult(400, "Purchase Request control number is required.");
            }

            var requestId = await _db.Requests
                .AsNoTracking()
                .Where(x => x.CtrlNo == ctrlNo)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            if (!requestId.HasValue)
            {
                return HttpNotFound("Purchase Request not found.");
            }

            if (!await CanAccessRequestRecordAsync(requestId.Value))
            {
                return new HttpStatusCodeResult(403, "You are not authorized to print this Purchase Request.");
            }

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
                ApprovedBy = approved != null ? approved.Description : null,
                ApprovedDesig = approved != null ? approved.Desc2 : null,
                Availability = availability != null ? availability.Description : null,
                AvaialbilityDesig = availability != null ? availability.Desc2 : null
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> _PpmpItemSelectionSave(Guid? prId, string selectedIds)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "requests");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("UpdateError", "Access Denied!");
                }
                else if (!prId.HasValue || !await CanEditDraftRequestAsync(prId.Value, access))
                {
                    ModelState.AddModelError("UpdateError", "Only an authorized Draft Purchase Request can add PPMP items here.");
                }



                else
                {
                    ModelState.Clear();
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _requestService.RequestItem.CreateFromPpmpItemAsync(prId, selectedIds, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                var errors = validationException.GetErrorsForModelState();
                if (errors != null && errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(error.Item1, error.Item2);
                    }
                }
                else
                {
                    ModelState.AddModelError("", validationException.InnerException != null ? validationException.InnerException.Message : validationException.Message);
                }
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

        private async Task<bool> CanEditDraftRequestAsync(Guid requestId, Access access)
        {
            var entity = await _db.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == requestId);
            if (entity == null)
            {
                return false;
            }

            var status = await GetRequestStatusAsync(entity);
            if (status != PrStatuses.Draft)
            {
                return false;
            }

            return access.IsAllowed &&
                string.Equals(entity.InsertedBy, User.Identity.Name, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> CanAdminEditRequestAsync(Guid requestId, Access access)
        {
            var entity = await _db.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == requestId);
            if (entity == null)
            {
                return false;
            }

            var status = await GetRequestStatusAsync(entity);
            if (status != PrStatuses.Submitted && status != PrStatuses.Unposted)
            {
                return false;
            }

            if (access.IsAdmin)
            {
                return true;
            }

            var postingAccess = await Access(User.Identity.GetUserId(), "requests_posting");
            return postingAccess.IsAllowed && postingAccess.AllowEdit;
        }

        private async Task<bool> CanAccessRequestRecordAsync(Guid requestId)
        {
            var userId = User.Identity.GetUserId();
            var mainAccess = await Access(userId, "requests");
            var postingAccess = await Access(userId, "requests_posting", "requests");

            if (!mainAccess.IsAllowed && !postingAccess.IsAllowed)
            {
                return false;
            }

            if (mainAccess.IsAdmin || postingAccess.IsAdmin)
            {
                return true;
            }

            var scopedRequests = await _requestService.GetAllAsync(userId);
            return await scopedRequests.AnyAsync(x => x.Id == requestId);
        }

        private string NormalizePrStatus(string status)
        {
            switch (status != null ? status.ToUpperInvariant() : null)
            {
                case "SUBMITTED":
                    return PrStatuses.Submitted;
                case "RETURNED":
                    return PrStatuses.Returned;
                case "REVISING":
                    return PrStatuses.Revising;
                case "POSTED":
                    return PrStatuses.Posted;
                case "UNPOSTED":
                    return PrStatuses.Unposted;
                case "CANCELLED":
                    return PrStatuses.Cancelled;
                case "DRAFT":
                    return PrStatuses.Draft;
                default:
                    return !string.IsNullOrWhiteSpace(status) ? status : PrStatuses.Draft;
            }
        }
    }
}
