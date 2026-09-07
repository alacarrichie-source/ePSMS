using System.Data.Entity;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AIRs_;
using iLgs.Services.Codes;
using iLgs.Services.PurchaseOrder;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static iLgs.Models.Enums;
using iLgs.Ai.Models;
using iLgs.Ai.Services;
using iLgs.Ai.Services.Air;
using iLgs.Ai.Services.PurchaseOrder;

namespace iLgs.Controllers
{
    [AppAuthorize("AIRS")]
    public class AIRsController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly IAirService _airService;
        private readonly ICodextnService _codextnService;
        private readonly IOrderService _orderService;
        private readonly IAirUploadService _uploadService;
        //private readonly string[] _menuId = { "airs_inspection", "airs_acceptance" };
        private string _menuId = string.Empty;
        private readonly IAirInspectionService _airInspectionService;
        private readonly IAirAcceptanceService _airAcceptanceService;
        private readonly IAIRInventoryPostingService _airInventoryPostingService;
        private readonly IDocumentStorageService _docService;

        public AIRsController()
        {
            //_db = new AppManEntities();
            _airService = new AirService(_db);
            _codextnService = new CodextnService(_db);
            _orderService = new OrderService(_db);
            _uploadService = new AirUploadService(_db);
            _airInspectionService = new AirInspectionService(_db, new DocumentHistoryService(_db));
            _airAcceptanceService = new AirAcceptanceService(_db, new DocumentHistoryService(_db));
            _airInventoryPostingService = new AIRInventoryPostingService(_db, new DocumentHistoryService(_db));
            _docService = new DocumentStorageService();
        }

        //public AIRsController(AppManEntities db, IAirService airService, IAirItemService airItemService, ICodextnService codextnService, IOrderService orderService,
        //    IOrderItemUnitGroupService orderItemUnitGroupService, IOrderItemUnitGroupDescriptionService orderItemUnitGroupDescriptionService,
        //    IOrderItemUnitGroupDescriptionItemService orderItemUnitGroupDescriptionItemService, IServiceAgent serviceAgent,
        //    IAirUploadService airUploadService)
        //{
        //    _db = db;
        //    _airService = airService;
        //    _airService.AirItem = airItemService;
        //    _codextnService = codextnService;
        //    _orderService = orderService;            
        //    _sa = serviceAgent;
        //    _orderService.UnitGroup = orderItemUnitGroupService;
        //    _orderService.UnitGroup.UnitGroupDescription = orderItemUnitGroupDescriptionService;
        //    _orderService.UnitGroup.UnitGroupDescription.UnitGroupDescriptionItem = orderItemUnitGroupDescriptionItemService;
        //    _uploadService = airUploadService;
        //}

        public async Task<ActionResult> Admin()
        {
            _menuId = "airs_admin";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["airs"] = _menuId;
            TempData["AllowIndexAccess"] = true;
            ViewBag.AirGroup = (int)AirGroup.ADMIN;
            return View("Index");
        }

        public async Task<ActionResult> Custodian()
        {
            _menuId = "airs_custodian";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["airs"] = _menuId;
            TempData["AllowIndexAccess"] = true;
            ViewBag.AirGroup = (int)AirGroup.SERIAL;
            return View("Index");
        }

        public async Task<ActionResult> Acceptance(Guid? id, Guid? snapshotId)
        {
            _menuId = "airs_acceptance";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            if (id.HasValue)
            {
                var model = await _airInspectionService.GetSubmittedWizardViewModelAsync(id.Value, snapshotId);
                if (model == null)
                {
                    ViewBag.Error = "Inspection report or submission snapshot not found.";
                    return View("Error");
                }

                model.CanWithdraw = false;
                ViewBag.Title = "View Submitted Inspection - " + (!string.IsNullOrEmpty(model.AirNo) ? model.AirNo : (model.CtrlNo ?? "AIR"));
                ViewBag.CurrentStep = 1;
                ViewBag.IsSubmittedView = true;
                ViewBag.IsAcceptanceContext = true;
                ViewBag.ReturnUrl = Url.Action("Acceptance", "AIRs");
                ViewBag.ReturnLabel = "Back to Acceptance List";
                ViewBag.DraftId = (Guid?)null;
                ViewBag.DraftNo = (string)null;
                ViewBag.WizardStateJson = JsonConvert.SerializeObject(model);

                return View("Wizard", model);
            }

            TempData["airs"] = _menuId;
            TempData["AllowIndexAccess"] = true;
            ViewBag.AirGroup = (int)AirGroup.ACCEPTANCE;
            return View("Index");
        }

        public async Task<ActionResult> Inspection(Guid? id, Guid? snapshotId)
        {
            _menuId = "airs_inspection";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            if (id.HasValue)
            {
                var model = await _airInspectionService.GetSubmittedWizardViewModelAsync(id.Value, snapshotId);
                if (model == null)
                {
                    ViewBag.Error = "Inspection report or submission snapshot not found.";
                    return View("Error");
                }

                ViewBag.Title = "View Submitted Inspection - " + (!string.IsNullOrEmpty(model.AirNo) ? model.AirNo : (model.CtrlNo ?? "AIR"));
                ViewBag.CurrentStep = 1;
                ViewBag.IsSubmittedView = true;
                ViewBag.IsAcceptanceContext = false;
                ViewBag.ReturnUrl = Url.Action("Inspection", "AIRs");
                ViewBag.ReturnLabel = "Back to Inspection List";
                ViewBag.DraftId = (Guid?)null;
                ViewBag.DraftNo = (string)null;
                ViewBag.WizardStateJson = JsonConvert.SerializeObject(model);

                return View("Wizard", model);
            }

            TempData["airs"] = _menuId;
            TempData["AllowIndexAccess"] = true;
            ViewBag.AirGroup = (int)AirGroup.INSPECTION;
            return View("Index");
        }

        public async Task<ActionResult> View(Guid id, Guid? snapshotId)
        {
            return await Inspection(id, snapshotId);
        }

        public async Task<ActionResult> ViewSubmittedInspection(Guid airId, Guid? snapshotId)
        {
            return await Inspection(airId, snapshotId);
        }

        public async Task<ActionResult> ViewAcceptance(Guid id)
        {
            var userId = User.Identity.GetUserId();
            var accessAcceptance = await Access(userId, "airs_acceptance");
            var accessInspection = await Access(userId, "airs_inspection");
            if (!accessAcceptance.IsAllowed && !accessInspection.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            try
            {
                var model = await _airAcceptanceService.GetReadOnlyAcceptanceDetailsAsync(id);
                if (model == null)
                {
                    ViewBag.Error = "Acceptance record not found.";
                    return View("Error");
                }

                ViewBag.Title = "View Acceptance - " + (!string.IsNullOrEmpty(model.AirNo) ? model.AirNo : (model.CtrlNo ?? "AIR"));
                ViewBag.IsReadOnly = true;
                ViewBag.IsAcceptanceView = true;
                ViewBag.ReturnUrl = (Request.UrlReferrer != null && Request.UrlReferrer.ToString().ToLower().Contains("inspection"))
                    ? Url.Action("Inspection", "AIRs")
                    : Url.Action("Acceptance", "AIRs");
                return View("AcceptanceWizard", model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        // GET: AIRs
        public ActionResult Index()
        {
            ViewBag.Title = "Acceptance & Inspection Reports";
            return View();
        }

        // GET: AIRs/AIRs_Read
        public async Task<ActionResult> AIRs_Read([DataSourceRequest] DataSourceRequest request, string statusFilter, bool acceptanceQueue = false)
        {
            var data = await _airInspectionService.GetAirListAsync(statusFilter, acceptanceQueue);
            return Json(data.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        // GET: AIRs/MyDrafts_Read
        public async Task<ActionResult> MyDrafts_Read([DataSourceRequest] DataSourceRequest request)
        {
            var user = User.Identity.Name;
            var data = await _airInspectionService.GetMyDraftsAsync(user);
            return Json(data.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        // GET: AIRs/Wizard
        public async Task<ActionResult> Wizard(Guid? id, Guid? orderId)
        {
            AIRWizardViewModel model;

            if (id.HasValue)
            {
                var draft = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.Id == id.Value);
                if (draft != null)
                {
                    if (draft.Status != AirWizardStatuses.Draft || draft.IsCompleted)
                    {
                        ViewBag.Error = "This inspection draft is no longer active (Status: " + draft.Status + ").";
                        return View("Error");
                    }
                    model = await _airInspectionService.GetDraftForEditAsync(id.Value);
                }
                else
                {
                    var air = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == id.Value);
                    if (air != null)
                    {
                        if (air.IsInspected == true || air.OverallStatus == AirStatuses.Withdrawn || air.OverallStatus == AirStatuses.SubmittedForAcceptance || air.PostedDt != null)
                        {
                            return RedirectToAction("Inspection", new { id = air.Id });
                        }
                    }
                    model = await _airInspectionService.GetAirInspectionForEditAsync(id.Value);
                }
            }
            else if (orderId.HasValue)
            {
                model = await _airInspectionService.GetPOInspectionDetailsAsync(orderId.Value);
                model.CurrentStep = 2;
            }
            else
            {
                model = new AIRWizardViewModel();
            }

            ViewBag.Title = model.IsRevisionMode ? "Revise Inspection Report" : "AIR Inspection Wizard";
            ViewBag.CurrentStep = model.CurrentStep;
            ViewBag.DraftId = model.DraftId;
            ViewBag.DraftNo = model.DraftNo;
            ViewBag.WizardStateJson = JsonConvert.SerializeObject(model);

            return View("Wizard", model);
        }

        // GET: AIRs/EligiblePOs_Read
        public async Task<ActionResult> EligiblePOs_Read([DataSourceRequest] DataSourceRequest request)
        {
            var data = await _airInspectionService.GetEligiblePurchaseOrdersAsync();
            return Json(data.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
        }

        // GET: AIRs/GetPOInspectionDetails
        [HttpGet]
        public async Task<ActionResult> GetPOInspectionDetails(Guid orderId, Guid? airId)
        {
            try
            {
                var details = await _airInspectionService.GetPOInspectionDetailsAsync(orderId, airId);
                return Json(new { success = true, data = details }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: AIRs/SaveDraft
        [HttpPost]
        public async Task<ActionResult> SaveDraft(AIRWizardViewModel model)
        {
            try
            {
                ValidateHeaderOrFormAntiForgeryToken();

                if (model != null && (model.IsSubmittedView || model.IsReadOnly))
                {
                    return Json(new { success = false, message = "Submitted inspection records are read-only and cannot be modified as drafts." });
                }

                if (model != null && model.AirId.HasValue && !model.SourceAIRId.HasValue)
                {
                    var isSubmitted = await _db.AIRs.AnyAsync(a => a.Id == model.AirId.Value && (a.IsInspected == true || a.OverallStatus == AirStatuses.SubmittedForAcceptance));
                    if (isSubmitted)
                    {
                        return Json(new { success = false, message = "This inspection report has already been submitted and cannot be modified as a draft." });
                    }
                }

                var id = await _airInspectionService.SaveInspectionDraftAsync(model, User.Identity.Name);
                return Json(new { success = true, draftId = id, airId = (Guid?)null, draftNo = model.DraftNo, message = "Inspection draft saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/SubmitForAcceptance
        [HttpPost]
        public async Task<ActionResult> SubmitForAcceptance(AIRWizardViewModel model)
        {
            try
            {
                ValidateHeaderOrFormAntiForgeryToken();
                var id = await _airInspectionService.SubmitForAcceptanceAsync(model, User.Identity.Name);
                return Json(new { success = true, airId = id, message = "Inspection submitted for acceptance." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void ValidateHeaderOrFormAntiForgeryToken()
        {
            var cookie = Request.Cookies[System.Web.Helpers.AntiForgeryConfig.CookieName];
            var cookieToken = cookie != null ? cookie.Value : null;
            var formToken = Request.Headers["X-RequestVerificationToken"] ?? Request.Form["__RequestVerificationToken"];
            if (string.IsNullOrEmpty(formToken))
            {
                throw new HttpAntiForgeryException("A request verification token is required.");
            }
            System.Web.Helpers.AntiForgery.Validate(cookieToken, formToken);
        }

        // POST: AIRs/WithdrawSubmission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> WithdrawSubmission(AIRActionRequestViewModel model)
        {
            try
            {
                var newDraftId = await _airInspectionService.WithdrawSubmissionAsync(model.AirId, model.Reason, User.Identity.Name);
                return Json(new { success = true, draftId = newDraftId, message = "Submission withdrawn and revision draft created." });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errors = new System.Collections.Generic.List<string>();
                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        errors.Add(string.Format("{0}.{1}: {2}", eve.Entry.Entity.GetType().Name, ve.PropertyName, ve.ErrorMessage));
                    }
                }
                return Json(new { success = false, message = "Validation failed: " + string.Join("; ", errors) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/RequestWithdrawal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RequestWithdrawal(AIRActionRequestViewModel model)
        {
            try
            {
                await _airInspectionService.RequestWithdrawalAsync(model.AirId, model.Reason, User.Identity.Name);
                return Json(new { success = true, message = "Withdrawal request sent to Acceptance Officer." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/DiscardDraft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DiscardDraft(Guid id)
        {
            try
            {
                await _airInspectionService.DiscardDraftAsync(id, User.Identity.Name);
                return Json(new { success = true, message = "Draft discarded." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/ChangeDraftPO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeDraftPO(Guid draftId, Guid newOrderId)
        {
            try
            {
                var model = await _airInspectionService.ChangeDraftPOAsync(draftId, newOrderId, User.Identity.Name);
                return Json(new { success = true, data = model, message = "Purchase Order updated for draft." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: AIRs/GetItemHistory
        public async Task<ActionResult> GetItemHistory(Guid orderItemId)
        {
            var history = await _airInspectionService.GetItemInspectionHistoryAsync(orderItemId);
            return PartialView("_ItemHistoryModal", history);
        }

        // ==========================================
        // ACCEPTANCE ACTIONS
        // ==========================================

        // POST: AIRs/StartAcceptance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StartAcceptance(Guid id)
        {
            try
            {
                await _airAcceptanceService.StartAcceptanceAsync(id, User.Identity.Name);
                return Json(new { success = true, message = "Acceptance process started." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: AIRs/AcceptanceWizard
        public async Task<ActionResult> AcceptanceWizard(Guid id)
        {
            var model = await _airAcceptanceService.GetAcceptanceDetailsAsync(id);
            ViewBag.Title = "AIR Acceptance Wizard";
            return View("AcceptanceWizard", model);
        }

        // POST: AIRs/ReturnForRevision
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReturnForRevision(AIRActionRequestViewModel model)
        {
            try
            {
                await _airAcceptanceService.ReturnForRevisionAsync(model.AirId, model.Reason, User.Identity.Name);
                return Json(new { success = true, message = "AIR returned for revision." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/DeclineWithdrawal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeclineWithdrawal(AIRActionRequestViewModel model)
        {
            try
            {
                await _airAcceptanceService.DeclineWithdrawalAsync(model.AirId, model.Reason, User.Identity.Name);
                return Json(new { success = true, message = "Withdrawal request declined." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/PostAcceptance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostAcceptance(AIRAcceptanceWizardViewModel model)
        {
            var access = await Access(User.Identity.GetUserId(), "airs_acceptance");
            if (!access.IsAllowed)
            {
                return Json(new { success = false, message = "Access Denied: You do not have permission to post AIR Acceptance." });
            }

            try
            {
                await _airAcceptanceService.PostAcceptanceAsync(model, User.Identity.Name);
                return Json(new { success = true, message = "AIR accepted and posted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/UnpostAcceptance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostToInventory(Guid id)
        {
            try
            {
                var access = await Access(User.Identity.GetUserId(), "airs_acceptance");
                if (!access.AllowPost) return Json(new { success = false, message = "Access Denied: posting permission is required." });
                var result = await _airInventoryPostingService.PostCompletedAirAsync(id, ControllerContext.HttpContext.User.Identity.Name);
                return Json(new { success = true, alreadyPosted = result.AlreadyPosted, receiptCount = result.ReceiptCount, assetCount = result.AssetCount });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UnpostAcceptance(AIRActionRequestViewModel model)
        {
            var access = await Access(User.Identity.GetUserId(), "airs_acceptance");
            if (!access.IsAllowed)
            {
                return Json(new { success = false, message = "Access Denied: You do not have permission to unpost AIR Acceptance." });
            }

            if (model == null || model.AirId == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid request: AIR ID is required." });
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                return Json(new { success = false, message = "Reason for unposting is required." });
            }

            try
            {
                await _airAcceptanceService.UnpostAcceptanceAsync(model.AirId, model.Reason, User.Identity.Name);
                return Json(new { success = true, message = "AIR Acceptance unposted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/DeleteAcceptance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteAcceptance(AIRActionRequestViewModel model)
        {
            var access = await Access(User.Identity.GetUserId(), "airs_acceptance");
            if (!access.IsAllowed)
            {
                return Json(new { success = false, message = "Access Denied: You do not have permission to delete AIR Acceptance." });
            }

            if (model == null || model.AirId == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid request: AIR ID is required." });
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                return Json(new { success = false, message = "Reason for deletion is required." });
            }

            try
            {
                await _airAcceptanceService.DeleteAcceptanceAsync(model.AirId, model.Reason, User.Identity.Name);
                return Json(new { success = true, message = "AIR Acceptance deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: AIRs/UploadAttachment
        [HttpPost]
        public ActionResult UploadAttachment(HttpPostedFileBase file, string category)
        {
            try
            {
                var doc = _docService.SaveUploadedFile(file, "AIR", category ?? "SupportingDoc");
                return Json(new { success = true, document = doc });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public async Task<ActionResult> AIRRead([DataSourceRequest] DataSourceRequest request, int? airGroup)
        {
            var userId = User.Identity.GetUserId();
            var data = await _airService.GetAllAsync(userId, (AirGroup)airGroup);

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
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }

                if (await _airService.GetByAirNoAsync(model.AIRNo) != null)
                {
                    ModelState.AddModelError("AIR No.", "AIR No. already exists!");
                }
                else
                {
                    var order = await _orderService.GetByIdAsync((Guid)model.OrderId);
                    if (order == null)
                    {
                        ModelState.AddModelError("PO No.", "Invalid PO No.!");
                    }
                    //else
                    //{
                    //    if (order.PoDate > model.AIRDate)
                    //    {
                    //        ModelState.AddModelError("AIR Date", "AIR date must be greather than or equal to P.O. date!");
                    //    }
                    //    if (order.PoDate > model.InvoiceDate)
                    //    {
                    //        ModelState.AddModelError("Invoice Date", "Invoice Date date must be greather than or equal to P.O. date!");
                    //    }
                    //}
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.CreateAsync(model, user, date);
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
        public async Task<ActionResult> AIRUpdate([DataSourceRequest] DataSourceRequest request, AIR_VM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (await _airService.GetAnyAirNoAsync(model.Id, model.AIRNo))
                {
                    ModelState.AddModelError("AIR No", "AIR No. already exists!");
                }
                else
                {
                    var order = await _orderService.GetByIdAsync((Guid)model.OrderId);
                    if (order == null)
                    {
                        ModelState.AddModelError("PO No", "Invalid PO No.!");
                    }
                    //else
                    //{
                    //    if (order.PoDate > model.AIRDate)
                    //    {
                    //        ModelState.AddModelError("AIR Date", "AIR date must be greather than or equal to P.O. date!");
                    //    }

                    //    if (order.PoDate > model.InvoiceDate)
                    //    {
                    //        ModelState.AddModelError("Invoice Date", "Invoice Date date must be greather than or equal to P.O. date!");
                    //    }
                    //}
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> AIRDestroy([DataSourceRequest]DataSourceRequest request, AIR_VM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model = await _airService.DeleteAsync(model, user, date);
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

        public async Task<ActionResult> _AIRAddEdit(Guid? airId, int airGroup)
        {
            var model = new AIR_VM();
            if (airId != null)
            {
                model = await _airService.GetVmByIdAsync((Guid)airId);
                model.Mode = "E";
            }
            else
            {
                model.Mode = "A";
                model.Id = Guid.NewGuid();
                //model.AIRDate = DateTime.Now;
            }
            model.AirGroup = airGroup;
            ViewData["airId"] = airId;
            ViewBag.AirGroup = airGroup;
            return PartialView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRSave(AIR_VM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;

                var entity = await _airService.GetByIdAsync(model.Id);
                if (entity == null)
                {
                    if (!access.AllowAdd)
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }
                else
                {
                    if (!access.AllowEdit)
                    {
                        ModelState.AddModelError("Access Error", "Access Denied!");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (entity == null)
                    {
                        model = await _airService.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _airService.UpdateAsync(model, user, date);
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

            return Json(new { Errors = "", Model = model }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _AIRInvoiceRead([DataSourceRequest] DataSourceRequest request, Guid? airId)
        {
            var data = _airService.AirInvoice.GetVmByAirId(airId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRInvoiceCreate([DataSourceRequest] DataSourceRequest request, AIRInvoiceVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirInvoice.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRInvoiceUpdate([DataSourceRequest] DataSourceRequest request, AIRInvoiceVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirInvoice.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRInvoiceDestroy([DataSourceRequest]DataSourceRequest request, AIRInvoiceVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirInvoice.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("GridError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("GridError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult _AIRItem(Guid airId, int airGroup)
        {
            ViewData["airId"] = airId;
            ViewBag.AirGroup = airGroup;
            return PartialView();
        }

        public async Task<ActionResult> _AIRItemAddEdit(Guid airId, Guid? airItemId)
        {
            var data = await _airService.AirItem.GetByIdAsync(airItemId);
            if (data == null)
            {
                data = new AIRItemVM()
                {
                    Id = Guid.NewGuid(),
                    AirId = airId,
                    Mode = "A"
                };
            }
            else
            {
                data.Mode = "E";
            }
            ViewData["airItemId"] = airItemId;
            return PartialView(data);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRItemSave(AIRItemVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await _airService.IsPostedAsync((Guid)model.AirId))
                {
                    ModelState.AddModelError("AIR No.", "AIR Number already Posted, cannot update!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    if (model.Mode == "A")
                    {
                        model = await _airService.AirItem.CreateAsync(model, user, date);
                    }
                    else
                    {
                        model = await _airService.AirItem.UpdateAsync(model, user, date);
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

        public ActionResult _AIRItemRead([DataSourceRequest] DataSourceRequest request, Guid? airId)
        {
            var data = _airService.AirItem.GetByAirId(airId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRItemCreate([DataSourceRequest] DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRItemUpdate([DataSourceRequest] DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRItemDestroy([DataSourceRequest]DataSourceRequest request, AIRItemVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.DeleteAsync(model, user, date);
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

        #region ITEMEXTN VEHICLES
        public ActionResult _AIRItemExtnVehicleRead([DataSourceRequest] DataSourceRequest request, Guid? airItemId)
        {
            var data = _airService.AirItem.AirItemExtn.AirItemExtnVehicle.GetByAirItemId(airItemId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRItemExtnVehicleCreate([DataSourceRequest] DataSourceRequest request, AIRItemExtnVehicle model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;
                    model = await _airService.AirItem.AirItemExtn.AirItemExtnVehicle.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRItemExtnVehicleUpdate([DataSourceRequest] DataSourceRequest request, AIRItemExtnVehicle model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.AirItemExtn.AirItemExtnVehicle.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRItemExtnVehicleDestroy([DataSourceRequest]DataSourceRequest request, AIRItemExtnVehicle model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.AirItemExtn.AirItemExtnVehicle.DeleteAsync(model, user, date);
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
        #endregion  


        #region ITEMEXTN OTHERS
        public ActionResult _AIRItemExtnOtherRead([DataSourceRequest] DataSourceRequest request, Guid? airItemId)
        {
            var data = _airService.AirItem.AirItemExtn.AirItemExtnOther.GetByAirItemId(airItemId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _AIRItemExtnOtherCreate([DataSourceRequest] DataSourceRequest request, AIRItemExtnOther model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.AirItemExtn.AirItemExtnOther.CreateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRItemExtnOtherUpdate([DataSourceRequest] DataSourceRequest request, AIRItemExtnOther model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.AirItemExtn.AirItemExtnOther.UpdateAsync(model, user, date);
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
        public async Task<ActionResult> _AIRItemExtnOtherDestroy([DataSourceRequest]DataSourceRequest request, AIRItemExtnOther model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("GridError", "Delete Access Denied!");
                }
                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _airService.AirItem.AirItemExtn.AirItemExtnOther.DeleteAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("GridError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("GridError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> GenerateSerials(Guid airItemId, string type)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
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

                    await _airService.AirItem.AirItemExtn.AirItemExtnOther.GenerateSerialAsync(airItemId, user, date);
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
        public async Task<ActionResult> GenerateItemExtnSerial(Guid airItemExtnId, string type)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
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

                    await _airService.AirItem.AirItemExtn.AirItemExtnOther.GenerateSerialItemExtnAsync(airItemExtnId, user, date);
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
        #endregion  

        public async Task<ActionResult> AIRRpt(string ctrlNo)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (access == null)
                {
                    throw new Exception("Access Denied!");
                }
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                return View("Error");
            }

            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Air.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = rpt.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;
            crConnectionInfo.IntegratedSecurity = false;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = rpt.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cCtrlNo", ctrlNo);
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }

        public async Task<ActionResult> RisByAirRpt(string ctrlNo)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (access == null)
                {
                    throw new Exception("Access Denied!");
                }
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
                return View("Error");
            }

            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/RisByAir.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = rpt.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;
            crConnectionInfo.IntegratedSecurity = false;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = rpt.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cCtrlNo", ctrlNo);
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> PostAIRs(Guid airId)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _airService.PostAsync(airId, user, date);
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

        [HttpPost]
        public async Task<ActionResult> UnpostAIRs(Guid airId)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }
                else if (await _airService.GetByIdAsync(airId) == null)
                {
                    ModelState.AddModelError("AIR", "Invalid AIR Id");
                }
                else if (!(await _airService.IsPostedAsync(airId)))
                {
                    ModelState.AddModelError("AIR No.", "AIR Number not yet posted, cannot unpost!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _airService.UnpostAsync(airId, user, date);
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

        //public ActionResult _OrderItemExtnBatchRead([DataSourceRequest] DataSourceRequest request, Guid? orderItemId, string psType)
        //{
        //    var data = _orderItemExtnService.GetBatchInfo(orderItemId, psType);
        //    var result = new JsonNetResult
        //    {
        //        Data = data.ToDataSourceResult(request),
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //        Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        //    };

        //    return result;
        //}

        [HttpPost]
        public ActionResult GetItemExtnTemplate(Guid? id)
        {
            string itemExtnName = _airService.AirItem.GetItemExtnName(id);

            return Json(new { Errors = "", ItemExtnName = itemExtnName }, JsonRequestBehavior.AllowGet);

        }

        #region UNIT GROUP
        public ActionResult _UnitGroup(Guid orderId, int airGroup)
        {
            ViewData["orderId"] = orderId;
            ViewBag.AirGroup = airGroup;
            return PartialView();
        }

        public ActionResult _UnitGroupRead([DataSourceRequest] DataSourceRequest request, Guid? orderId)
        {
            var data = _orderService.UnitGroup.GetByOrderId(orderId);
            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _UnitGroupUpdate([DataSourceRequest] DataSourceRequest request, OrderItemUnitGroupVM model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _orderService.UnitGroup.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdteError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        #endregion

        #region UNIT GROUP DESCRIPTION
        public ActionResult _UnitGroupDescription(Guid unitGroupId)
        {
            ViewData["unitGroupId"] = unitGroupId;
            return PartialView();
        }

        public ActionResult _UnitGroupDescriptionRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupId)
        {
            var data = _orderService.UnitGroup.UnitGroupDescription.GetByUnitGroupId(unitGroupId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> _UnitGroupDescriptionUpdate([DataSourceRequest] DataSourceRequest request, OrderItemUnitGroupDescriptionVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
        //        Access access = await accessTask;
        //        if (!access.AllowEdit)
        //        {
        //            ModelState.AddModelError("UpdateError", "Update Access Denied!");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            model = await _orderService.UnitGroup.UnitGroupDescription.UpdateAsync(model, user, date);
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
        #endregion

        #region UNIT GROUP DESCRIPTION ITEMS        
        public ActionResult _UnitGroupDescriptionItemRead([DataSourceRequest] DataSourceRequest request, Guid? unitGroupDescriptionId)
        {
            var data = _orderService.UnitGroup.UnitGroupDescription.UnitGroupDescriptionItem.GetByUnitGroupDescriptionId(unitGroupDescriptionId);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        #endregion

        #region UPLOADS
        public ActionResult _Images(Guid? imageId, string postedBy)
        {
            ViewData["imageId"] = imageId;
            ViewData["postedBy"] = postedBy;
            return PartialView();
        }


        public ActionResult _ImagesAdd(Guid? imageId)
        {
            var model = new Models.Upload()
            {
                ImageId = imageId,
                Description = "AIR"
            };
            ViewData["imageId"] = imageId;
            ViewData["fileSize"] = model.FileSize;
            return PartialView(model);
        }

        public ActionResult _ImagesRead([DataSourceRequest] DataSourceRequest request, Guid imageId)
        {
            var data = _uploadService.GetAllByImageId(imageId);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> _ImagesDestroy([DataSourceRequest]DataSourceRequest request, Models.Upload model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadService.DeleteAsync(model, user, date);
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

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _ImagesUpdate([DataSourceRequest] DataSourceRequest request, Models.Upload model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("UpdateError", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadService.UpdateAsync(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("UpdateError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("UpdateError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("UpdateError", e.Message);
            }


            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public async Task<ActionResult> _ImagesUpload(IEnumerable<HttpPostedFileBase> files, Models.Upload model)
        {
            try
            {
                _menuId = TempData["airs"]?.ToString();
                TempData.Keep("airs");
                Task<Access> accessTask = Access(User.Identity.GetUserId(), _menuId);
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("AddError", "Upload Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _uploadService.UploadAsync(files, model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError("AddError", error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("AddError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("AddError", e.Message);
            }

            var errorList = ModelState.Values.SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();

            if (errorList.Any())
            {
                var errorMessage = string.Join("\n", errorList);
                return Content(errorMessage);
            }

            return Content("");
        }

        public ActionResult DownloadFile(string fileName)
        {
            try
            {
                // Call the service to get the file bytes
                byte[] fileBytes = _uploadService.DownloadFile(fileName);

                // Return the file as a download
                return File(fileBytes, MimeMapping.GetMimeMapping(fileName), fileName);
            }
            catch (FileNotFoundException ex)
            {
                // Handle file not found case
                return HttpNotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                return new HttpStatusCodeResult(500, "Error downloading file: " + ex.Message);
            }
        }

        public async Task<ActionResult> PreviewUpload(Guid id)
        {
            var fileResult = await _uploadService.GetUploadedFileAsync(id);
            if (fileResult != null)
            {
                return fileResult; // Return the file result directly
            }
            else
            {
                return HttpNotFound("File not found"); // Handle not found case
            }
        }
        #endregion
    }
}



