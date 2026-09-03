using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using iLgs.Ai.Models;
using iLgs.Ai.Services.PurchaseOrder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iLgs.Controllers
{
    [Authorize]
    public class PurchaseOrdersController : BaseController
    {
        private readonly IPurchaseOrderAiService _poService;
        private readonly IPurchaseRequestAiService _prService;
        private readonly ISupplierAiService _supplierService;
        private readonly IPdfReportService _pdfService;
        private readonly IDocumentStorageService _docService;

        public PurchaseOrdersController()
        {
            _poService = new PurchaseOrderAiService(_db);
            _prService = new PurchaseRequestAiService(_db);
            _supplierService = new SupplierAiService(_db);
            _pdfService = new PdfReportService();
            _docService = new DocumentStorageService();
        }

        // ==========================================
        // 1. MAIN PURCHASE ORDER GRID & ACTIONS
        // ==========================================

        public ActionResult Index()
        {
            ViewBag.Title = "Purchase Orders Management";
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> MyWizardDrafts_Read(
            [DataSourceRequest] DataSourceRequest request)
        {
            var user = User.Identity.Name ?? "Admin";
            var drafts = await _poService.GetActiveWizardDraftsAsync(user);
            return Json(drafts.ToDataSourceResult(request));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DiscardWizardDraft(Guid id)
        {
            var user = User.Identity.Name ?? "Admin";
            var discarded = await _poService.DiscardWizardDraftAsync(id, user);
            return Json(new
            {
                success = discarded,
                message = discarded
                    ? "The wizard draft was discarded."
                    : "The wizard draft was not found or is no longer active."
            });
        }

        [HttpPost]
        public ActionResult PurchaseOrders_Read([DataSourceRequest] DataSourceRequest request)
        {
            var query = _poService.GetPurchaseOrdersGrid();
            var result = query.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<ActionResult> GetPODetailsModal(Guid id)
        {
            var model = await _poService.GetPODetailsAsync(id);
            if (model == null) return HttpNotFound("Purchase Order not found.");
            return PartialView("_PODetailsModal", model);
        }

        [HttpGet]
        public async Task<ActionResult> Print(Guid id)
        {
            var model = await _poService.GetPODetailsAsync(id);
            if (model == null) return HttpNotFound("Purchase Order not found.");
            return View("Print", model);
        }

        [HttpGet]
        public async Task<ActionResult> DownloadPdf(Guid id)
        {
            var model = await _poService.GetPODetailsAsync(id);
            if (model == null) return HttpNotFound("Purchase Order not found.");

            var pdfBytes = await _pdfService.GeneratePurchaseOrderForm101APdfAsync(model);
            var fileName = $"PO_Form_101A_{model.PONumber}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostPO(Guid id, string bacResolutionNo)
        {
            try
            {
                var success = await _poService.PostPOAsync(id, User.Identity.Name ?? "Admin", bacResolutionNo);
                if (!success) return Json(new { success = false, message = "Unable to post PO." });
                return Json(new { success = true, message = "Purchase Order posted successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelPO(Guid id, string reason)
        {
            try
            {
                var success = await _poService.CancelPOAsync(id, User.Identity.Name ?? "Admin", reason);
                if (!success) return Json(new { success = false, message = "Unable to cancel PO." });
                return Json(new { success = true, message = "Purchase Order cancelled." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ==========================================
        // 2. 4-STEP PO CREATION WIZARD
        // ==========================================

        // UPDATED: Added logic to load existing draft state
        public async Task<ActionResult> Wizard(Guid? draftId = null)
        {
            ViewBag.Title = "Create Purchase Order Wizard";
            var model = new POWizardViewModel();
            ViewBag.SelectedPrIds = new List<Guid>();
            ViewBag.WizardStateJson = null;
            ViewBag.CurrentStep = 1;
            ViewBag.DraftId = null;

            // If a draftId is provided, we load the previously selected PRs
            // This populates the checkboxes in Step 1 automatically
            if (draftId.HasValue)
            {
                var draft = await _poService.GetWizardDraftAsync(
                    draftId.Value,
                    User.Identity.Name ?? "Admin");
                if (draft != null)
                {
                    ViewBag.SelectedPrIds = draft.PrIds;
                    ViewBag.DraftId = draft.DraftId;
                    ViewBag.WizardStateJson = draft.StateJson;
                    ViewBag.CurrentStep = draft.CurrentStep;
                }
            }

            return View("Wizard", model);
        }

        [HttpPost]
        public ActionResult GetApprovedPRs_Read([DataSourceRequest] DataSourceRequest request)
        {
            var query = _prService.GetApprovedPRsGrid();
            var result = query.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetSelectedPRLineItems(string[] prIds)
        {
            if (prIds == null || prIds.Length == 0)
            {
                return Json(new { success = false, message = "No Purchase Requests selected." });
            }

            var items = await _prService.GetPRItemsForAllocationAsync(prIds);
            return Json(new { success = true, items = items });
        }

        // Used by both manual save and debounced auto-save.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SavePODraft(
            Guid? draftId,
            string wizardStateJson,
            List<Guid> prIds,
            int currentStep)
        {
            if (prIds == null || !prIds.Any())
            {
                return Json(new { success = false, message = "Please select at least one PR." });
            }

            try
            {
                if (String.IsNullOrWhiteSpace(wizardStateJson) ||
                    wizardStateJson.Length > 2 * 1024 * 1024)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The wizard draft is empty or too large."
                    });
                }

                try
                {
                    JObject.Parse(wizardStateJson);
                }
                catch (JsonException)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The wizard draft format is invalid."
                    });
                }

                currentStep = Math.Max(1, Math.Min(4, currentStep));
                var user = User.Identity.Name ?? "Admin";
                var savedDraftId = await _poService.SaveWizardProgressAsync(
                    draftId,
                    wizardStateJson,
                    prIds,
                    currentStep,
                    user);

                return Json(new
                {
                    success = true,
                    message = "Progress saved successfully.",
                    draftId = savedDraftId
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "The wizard draft could not be saved."
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetSuppliersDropdown()
        {
            var suppliers = await _supplierService.GetActiveSuppliersAsync();
            return Json(suppliers, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadPOCopy(string groupId, HttpPostedFileBase file)
        {
            try
            {
                var doc = _docService.SaveUploadedFile(file, "POCopies", "POCopy");
                return Json(new { success = true, document = doc, groupId = groupId });
            }
            catch (InvalidOperationException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAdditionalDoc(string groupId, HttpPostedFileBase file)
        {
            try
            {
                var doc = _docService.SaveUploadedFile(file, "SupportingDocs", "Additional");
                return Json(new { success = true, document = doc, groupId = groupId });
            }
            catch (InvalidOperationException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // Backward-compatible alias. Saving a draft must not create Order records.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<ActionResult> SaveDraft(
            Guid? draftId,
            string wizardStateJson,
            List<Guid> prIds,
            int currentStep)
        {
            return SavePODraft(draftId, wizardStateJson, prIds, currentStep);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostPOs(Guid? draftId, string poGroupsJson)
        {
            var poGroups = DeserializeGroups(poGroupsJson);
            if (poGroups == null || !poGroups.Any()) return Json(new { success = false, message = "No groups." });

            try
            {
                var validationMessage = await ValidateWizardGroupsAsync(poGroups, true);
                if (validationMessage != null) return Json(new { success = false, message = validationMessage });
                var user = User.Identity.Name ?? "Admin";
                var created = await _poService.CreatePOsFromWizardAsync(poGroups, user, isDraft: false);
                if (draftId.HasValue)
                {
                    var completedDraft = await _db.PurchaseOrderWizardProgresses
                        .FirstOrDefaultAsync(d =>
                            d.Id == draftId.Value &&
                            d.CreatedBy == user &&
                            !d.IsCompleted);

                    if (completedDraft != null)
                    {
                        completedDraft.IsCompleted = true;
                        completedDraft.UpdatedAt = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }
                }
                var poNumbers = string.Join(", ", created.Select(p => p.PoNo));

                return Json(new
                {
                    success = true,
                    message = $"Purchase Order(s) {poNumbers} posted!",
                    redirectUrl = Url.Action("Index", "PurchaseOrders")
                });
            }
            catch (Exception) { return Json(new { success = false, message = "The Purchase Orders could not be posted. Please review the data and try again." }); }
        }

        private static List<POGroupDraftViewModel> DeserializeGroups(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;
            try { return JsonConvert.DeserializeObject<List<POGroupDraftViewModel>>(json); }
            catch (JsonException) { return null; }
        }

        private async Task<string> ValidateWizardGroupsAsync(List<POGroupDraftViewModel> groups, bool posting)
        {
            if (groups.Count > 50) return "Too many PO groups were submitted.";
            var allocatedQuantities = new Dictionary<Guid, int>();
            var poNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                var name = String.IsNullOrWhiteSpace(group.GroupName) ? "PO group" : group.GroupName;
                if (group.Items == null || !group.Items.Any()) return name + " has no line items.";
                if (group.Items.Any(i => i == null || i.Quantity <= 0 || i.UnitCost < 0 || String.IsNullOrWhiteSpace(i.Description)))
                    return name + " contains an invalid line item.";
                if (!posting) continue;
                if (String.IsNullOrWhiteSpace(group.PONumber))
                    return "Enter the PO number for " + name + ".";
                if (!System.Text.RegularExpressions.Regex.IsMatch(group.PONumber.Trim(), @"^\d{4}-\d{2}-\d{4}$"))
                    return "Enter the PO number for " + name + " in YYYY-MM-9999 format.";
                if (!poNumbers.Add(group.PONumber.Trim()))
                    return "The PO number " + group.PONumber.Trim() + " is used by more than one PO group.";
                if (await _db.Orders.AnyAsync(o => o.PoNo == group.PONumber.Trim()))
                    return "The PO number " + group.PONumber.Trim() + " already exists.";
                if (!group.SupplierId.HasValue)
                    return "Select a valid supplier for " + name + ".";
                var supplier = await _db.Suppliers.FindAsync(group.SupplierId.Value);
                if (supplier == null)
                    return "Select a valid supplier for " + name + ".";
                group.SupplierName = supplier.Name;
                group.SupBusiness = supplier.BusinessName;
                group.SupAddress = supplier.Address;
                group.SupTIN = supplier.TIN;
                group.SupEmail = supplier.Email;
                group.SupZipCode = supplier.ZipCode;
                group.SupContactNo = supplier.ContactNos;
                if (String.IsNullOrWhiteSpace(group.PlaceOfDelivery) || String.IsNullOrWhiteSpace(group.TermDelivery) ||
                    String.IsNullOrWhiteSpace(group.PaymentTerms) || String.IsNullOrWhiteSpace(group.ModeOfProcurement))
                    return "Complete the delivery and procurement terms for " + name + ".";
                if (group.POCopyDoc == null || String.IsNullOrWhiteSpace(group.POCopyDoc.FilePath))
                    return "Upload the signed PO copy for " + name + ".";
                foreach (var item in group.Items)
                {
                    if (item.Allocations == null || !item.Allocations.Any()) return "Source PR allocations are missing for an item in " + name + ".";
                    if (item.Allocations.Any(a => !a.RequestItemId.HasValue || a.Quantity <= 0) || item.Allocations.Sum(a => a.Quantity) != item.Quantity)
                        return "A source PR allocation is invalid for an item in " + name + ".";
                    foreach (var allocation in item.Allocations)
                    {
                        var requestItem = await _db.RequestItems.FindAsync(allocation.RequestItemId.Value);
                        if (requestItem == null)
                            return "A source Purchase Request item is no longer available.";
                        var request = await _db.Requests.FindAsync(requestItem.PrId);
                        if (request == null || request.PostedDt == null) return "A source Purchase Request is no longer approved.";
                        if ((requestItem.UnitCost ?? 0) != item.UnitCost)
                            return "A submitted unit cost no longer matches its approved Purchase Request.";
                        var requestItemId = allocation.RequestItemId.Value;
                        allocatedQuantities[requestItemId] = (allocatedQuantities.ContainsKey(requestItemId) ? allocatedQuantities[requestItemId] : 0) + allocation.Quantity;
                        if (allocatedQuantities[requestItemId] > (requestItem.Qty ?? 0))
                            return "A Purchase Request item quantity has been over-allocated.";
                    }
                }
            }
            return null;
        }

        // ==========================================
        // 3. DOCUMENT DOWNLOAD & PREVIEW
        // ==========================================

        [HttpGet]
        public ActionResult DownloadDocument(string path, string name)
        {
            var fileBytes = _docService.GetFileBytes(path);
            if (fileBytes == null) return HttpNotFound();
            return File(fileBytes, "application/octet-stream", name);
        }

        [HttpGet]
        public ActionResult PreviewDocument(string path)
        {
            var fileBytes = _docService.GetFileBytes(path);
            if (fileBytes == null) return HttpNotFound();
            var extension = Path.GetExtension(path ?? String.Empty).ToLowerInvariant();
            var contentType = extension == ".pdf" ? "application/pdf" :
                (extension == ".jpg" || extension == ".jpeg") ? "image/jpeg" :
                extension == ".png" ? "image/png" : "application/octet-stream";
            return File(fileBytes, contentType);
        }
    }
}
