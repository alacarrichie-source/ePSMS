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
using iLgs.Ai.Service;
using iLgs.Ai.Service.PurchaseOrder;
using System.Net;
using iLgs.Services.Items;
using iLgs.Services.PurchaseOrder;
using iLgs.Models;
using Microsoft.AspNet.Identity;

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
                var access = await Access(User.Identity.GetUserId(), "orders");
                if (!access.AllowPost) return Json(new { success = false, message = "Post access denied." });
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UnpostPO(Guid id)
        {
            try {
                var access = await Access(User.Identity.GetUserId(), "orders");
                if (!access.AllowUnpost) return Json(new { success = false, message = "Unpost access denied." });
                await new PurchaseOrderLifecycleService(_db).UnpostAsync(id, User.Identity.Name, DateTime.Now);
                return Json(new { success = true, message = "Purchase Order successfully unposted. You may now edit and repost the PO.", editUrl = Url.Action("Wizard", new { orderId = id }) });
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveExistingPO(Guid id, string poGroupsJson, bool post = false)
        {
            try {
                var access = await Access(User.Identity.GetUserId(), "orders");
                if (!access.AllowEdit || (post && !access.AllowPost))
                    return Json(new { success = false, message = "Edit/post access denied." });
                var groups = DeserializeGroups(poGroupsJson);
                if (groups == null || groups.Count != 1 || groups[0] == null)
                    return Json(new { success = false, message = "Submit exactly one existing Purchase Order." });
                var po = await _poService.SaveExistingPOAsync(id, groups[0], User.Identity.Name, post);
                return Json(new { success = true, id = po.Id, message = post ? "The existing Purchase Order was reposted." : "Changes to the existing Purchase Order were saved.", redirectUrl = Url.Action("Index") });
            } catch (InvalidOperationException ex) { return Json(new { success = false, message = ex.Message }); }
              catch (Exception) { return Json(new { success = false, message = "The Purchase Order could not be saved. No changes were committed." }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeletePO(Guid id)
        {
            try {
                var access = await Access(User.Identity.GetUserId(), "orders");
                if (!access.AllowDelete) return Json(new { success = false, message = "Delete access denied." });
                using (var transaction = _db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable)) {
                    var lifecycle = new PurchaseOrderLifecycleService(_db);
                    await lifecycle.LockAsync(id);
                    await lifecycle.EnsureEditableAsync(id);
                    await new OrderService(_db).DeleteAsync(new OrderVM { Id = id }, User.Identity.Name, DateTime.Now);
                    transaction.Commit();
                }
                return Json(new { success = true, message = "Purchase Order deleted." });
            } catch (Exception ex) { return Json(new { success = false, message = ex.GetBaseException().Message }); }
        }

        [HttpGet]
        public async Task<ActionResult> ExistingDocument(Guid orderId, Guid documentId)
        {
            var upload = await _db.Uploads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId && x.ImageId == orderId);
            if (upload == null) return HttpNotFound();
            var directory = (upload.VirtualDirectory ?? "").Replace('\\', '/');
            if (directory.StartsWith("~/App_Data/Uploads/", StringComparison.OrdinalIgnoreCase)) {
                var bytes = _docService.GetFileBytes(directory.TrimEnd('/') + "/" + upload.FileName);
                return bytes == null ? (ActionResult)HttpNotFound() : File(bytes, MimeMapping.GetMimeMapping(upload.FileName));
            }
            var kind = directory.TrimEnd('/').Split('/').Last();
            if (kind != "ORDERS" && kind != "CAFOA") return HttpNotFound();
            return await new OrderUploadService(_db).Create(kind).GetUploadedFileAsync(documentId);
        }

        // ==========================================
        // 2. 4-STEP PO CREATION WIZARD
        // ==========================================

        // UPDATED: Added logic to load existing draft state
        public async Task<ActionResult> Wizard(Guid? draftId = null, Guid? orderId = null)
        {
            ViewBag.Title = "Create Purchase Order Wizard";
            var model = new POWizardViewModel();
            ViewBag.SelectedPrIds = new List<Guid>();
            ViewBag.WizardStateJson = null;
            ViewBag.CurrentStep = 1;
            ViewBag.DraftId = null;
            ViewBag.DraftNo = null;

            ViewBag.EditOrderId = null;
            ViewBag.ExistingPOJson = null;
            if (orderId.HasValue)
            {
                if (draftId.HasValue) return new HttpStatusCodeResult(400, "Choose an existing PO or a creation draft, not both.");
                var access = await Access(User.Identity.GetUserId(), "orders");
                if (!access.AllowEdit) return new HttpStatusCodeResult(403, "Edit access denied.");
                try {
                    var existing = await _poService.GetExistingPOAsync(orderId.Value);
                    foreach (var doc in existing.AdditionalDocs.Concat(existing.POCopyDoc == null ? new PODocumentViewModel[0] : new[] { existing.POCopyDoc }))
                        if (doc.ExistingUploadId.HasValue) doc.PreviewUrl = Url.Action("ExistingDocument", new { orderId = orderId.Value, documentId = doc.ExistingUploadId.Value });
                    ViewBag.EditOrderId = orderId.Value;
                    ViewBag.ExistingPOJson = JsonConvert.SerializeObject(existing);
                    ViewBag.CurrentStep = 3;
                    ViewBag.Title = "Edit Purchase Order " + existing.PONumber;
                    return View("Wizard", model);
                } catch (InvalidOperationException ex) { return new HttpStatusCodeResult(409, ex.Message); }
            }

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
                    ViewBag.DraftNo = draft.DraftNo;
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
                    var state = JObject.Parse(wizardStateJson);
                    if (state["existingOrderId"] != null && state["existingOrderId"].Type != JTokenType.Null)
                        return Json(new { success = false, message = "Existing POs must use SaveExistingPO, not a creation draft." });
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

                var savedDraftNo = await _db.PurchaseOrderWizardProgresses
                    .Where(d => d.Id == savedDraftId)
                    .Select(d => d.DraftNo)
                    .SingleAsync();

                return Json(new
                {
                    success = true,
                    message = "Progress saved successfully.",
                    draftId = savedDraftId,
                    draftNo = savedDraftNo
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
        public async Task<ActionResult> UploadPOCopy(string groupId, HttpPostedFileBase file, Guid? orderId = null)
        {
            try
            {
                if (orderId.HasValue) await new PurchaseOrderLifecycleService(_db).EnsureEditableAsync(orderId.Value);
                var doc = _docService.SaveUploadedFile(file, "POCopies", "POCopy");
                return Json(new { success = true, document = doc, groupId = groupId });
            }
            catch (InvalidOperationException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadAdditionalDoc(string groupId, HttpPostedFileBase file, Guid? orderId = null)
        {
            try
            {
                if (orderId.HasValue) await new PurchaseOrderLifecycleService(_db).EnsureEditableAsync(orderId.Value);
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
                var user = User.Identity.Name ?? "Admin";
                if (poGroups.Any(g => g.ExistingOrderId.HasValue))
                    return Json(new { success = false, message = "Use the existing PO save/repost action." });
                var groupIds = poGroups.Select(g => { Guid value; return Guid.TryParse(g.GroupId, out value) ? value : Guid.Empty; }).ToList();
                if (await _db.Orders.AnyAsync(o => groupIds.Contains(o.Id)))
                    return Json(new { success = false, message = "An existing PO cannot be submitted as a new PO." });
                if (!draftId.HasValue)
                    return Json(new { success = false, message = "Save the wizard draft before posting." });

                var completedDraft = await _db.PurchaseOrderWizardProgresses
                    .FirstOrDefaultAsync(d =>
                        d.Id == draftId.Value &&
                        d.CreatedBy == user &&
                        !d.IsCompleted);

                if (completedDraft == null || String.IsNullOrWhiteSpace(completedDraft.DraftNo))
                    return Json(new { success = false, message = "The wizard draft number could not be found." });

                // The persisted server-generated draft number is authoritative.
                // Never accept a client-supplied control number during posting.
                foreach (var group in poGroups)
                    group.CtrlNo = completedDraft.DraftNo;

                var validationMessage = await ValidateWizardGroupsAsync(poGroups, true);
                if (validationMessage != null) return Json(new { success = false, message = validationMessage });

                var created = await _poService.CreatePOsFromWizardAsync(poGroups, user, isDraft: false);
                completedDraft.IsCompleted = true;
                completedDraft.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                var poNumbers = string.Join(", ", created.Select(p => p.PoNo));

                return Json(new
                {
                    success = true,
                    message = $"Purchase Order(s) {poNumbers} posted!",
                    redirectUrl = Url.Action("Index", "PurchaseOrders")
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "The Purchase Orders could not be posted. Please review the data and try again."
                });
            }
        }

        private static List<POGroupDraftViewModel> DeserializeGroups(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;
            try { return JsonConvert.DeserializeObject<List<POGroupDraftViewModel>>(json); }
            catch (JsonException) { return null; }
        }

        private Task<string> ValidateWizardGroupsAsync(List<POGroupDraftViewModel> groups, bool posting)
        {
            return _poService.ValidateWizardGroupsAsync(groups, posting);
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

        // ================================================================
        // 1. PRINT FROM WIZARD PAYLOAD
        // ================================================================

        /// <summary>
        /// Step 4 preview/print.
        /// Receives the JSON PO Groups payload directly from the Wizard.
        /// Nothing is re-read from SQL Server.
        /// </summary>
        [HttpPost]
        public ActionResult PrintWizardPO(List<POGroupReportPayload> poGroups)
        {
            if (poGroups == null || poGroups.Count == 0)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest,
                    "No Purchase Order payload was supplied.");
            }

            try
            {
                var reportService =
                    new PurchaseOrderCrystalReportService();

                string reportPath = Server.MapPath(
                    "~/Reports/PurchaseOrder.rpt");

                byte[] pdf = reportService.ExportWizardPayloadPdf(
                    poGroups,
                    reportPath);

                // No filename => browser displays the PDF inline.
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.InternalServerError,
                    GetFullExceptionMessage(ex));
            }
        }


        // ================================================================
        // 2. PRINT AN ACTUAL / SAVED PURCHASE ORDER
        // ================================================================

        /// <summary>
        /// Prints an already-saved Purchase Order.
        ///
        /// Flow:
        /// database -> BuildActualPOPayload() -> same payload structure
        /// -> same DataSet -> same PurchaseOrder.rpt.
        /// </summary>
        [HttpGet]
        public ActionResult PrintPO(Guid id)
        {
            try
            {
                POGroupReportPayload payload = BuildActualPOPayload(id);

                if (payload == null)
                {
                    return HttpNotFound(
                        "The Purchase Order could not be found.");
                }

                var reportService =
                    new PurchaseOrderCrystalReportService();

                string reportPath = Server.MapPath(
                    "~/Reports/PurchaseOrder.rpt");

                byte[] pdf = reportService.ExportActualDataPdf(
                    payload,
                    reportPath);

                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.InternalServerError,
                    GetFullExceptionMessage(ex));
            }
        }


        // ================================================================
        // ACTUAL DATABASE DATA -> SHARED REPORT PAYLOAD
        // ================================================================

        /// <summary>
        /// This is the ONLY database-specific part of the reporting flow.
        ///
        /// Map your saved Order and its related entities here.
        ///
        /// IMPORTANT:
        /// The property/navigation names below are based on the PO model
        /// discussed for your wizard. Rename any property that differs in
        /// your actual EF model.
        /// </summary>
        private POGroupReportPayload BuildActualPOPayload(Guid poId)
        {
            var itemCodeService = new ItemCodeService(_db);
            var po = _db.Orders
                  .AsNoTracking()
                  .Include(x => x.OrderItems.Select(i => i.ItemCode.ItemType))
                  .Include(x => x.OrderItems.Select(i => i.AllField))
                  .Include(x => x.OrderItems.Select(i => i.OrderItemRequests))
                  .Include(x => x.OrderItems.Select(i => i.OrderSubItems))
                  .SingleOrDefault(x => x.Id == poId);

            if (po == null)
                return null;

            var payload = new POGroupReportPayload
            {
                // A saved PO is one report group.
                GroupId = po.Id.ToString(),
                GroupName = "Purchase Order",

                PONumber = po.PoNo,
                PRNumber = po.PrNo,
                DepartmentName = po.Department,

                SupplierId = po.SupplierId,

                // Change these to your Supplier navigation/property names.
                SupplierName = po.SupName,
                CtrlNo = po.CtrlNo,
                SupBusiness = po.SupBusiness,
                SupAddress = po.SupAddress,
                SupTIN = po.SupTIN,
                SupEmail = po.SupEmail,
                SupZipCode = po.SupZipCode,
                SupContactNo = po.SupContactNo,

                PODate = po.PoDate,
                //DeliveryPeriodDays = 
                //    po.DeliveryDate == null
                //        ? null
                //        : po.term.ToString(),

                DeliveryDate = po.DeliveryDate,
                PlaceOfDelivery = po.DeliveryPlace,
                TermDelivery = po.TermDelivery,
                PaymentTerms = po.TermPayment,
                ModeOfProcurement = po.PoMode,

                SignedBySuppName = po.SignedBySuppName,
                SignedBySuppDate = po.SignedBySuppDate,
                SignedByAuthName = po.SignedByAuthName,
                SignedByAuthDesignation =
                    po.SignedByAuthDesignation,
                ResoNo = po.ResoNo,
                CertifiedCorrectBy = po.CertifiedCorrectBy,
                CertifiedCorrectDate = po.CertifiedCorrectDate,

                SourcePRs = new List<SourcePRReportPayload>(),
                Items = new List<POItemReportPayload>(),
                AdditionalDocs =
                    new List<PODocumentReportPayload>()
            };


            // ============================================================
            // ITEMS
            // ============================================================

            foreach (var item in po.OrderItems.OrderBy(x => x.ItemNo))
            {
                var allFields =
                    PurchaseOrderAiService.GetAllFieldDictionary(
                        item.AllField,
                        itemCodeService.GetPartialView(item.ItemCodeId));
                var reportItem = new POItemReportPayload
                {
                    Id = item.Id,
                    ItemNo = item.ItemNo.ToString(),
                    ItemCode = item.ItemCode != null
                        ? item.ItemCode.Code
                        : item.PpmpCode,
                    ItemCodeId = item.ItemCodeId,
                    PpmpCode = item.PpmpCode,
                    StockNo = item.PsNo,

                    Description = item.Description,
                    Quantity = item.Qty ?? 0,
                    Unit = item.Unit,
                    UnitCost = item.UnitCost ?? 0,

                    Category = item.ItemCode != null && item.ItemCode.ItemType != null
                        ? item.ItemCode.ItemType.Category
                        : String.Empty,
                    //Account = item.ItemCode.ItemType.Account,
                    //SubAccount = item.SubAccount,
                    //GSOCategory = item.GSOCategory,
                    TechnicalDescription =
                        item.OtherDesc,

                    AllFields = allFields,

                    AdditionalSpecs =
                        allFields.Count == 0
                            ? null
                            : new AdditionalSpecsReportPayload
                            {
                                Id = item.AllField.Id,
                                Multipliers = allFields.ContainsKey("Multipliers")
                                    ? allFields["Multipliers"]
                                    : null,
                                Brand = allFields.ContainsKey("Brand")
                                    ? allFields["Brand"]
                                    : null,
                                Model_ = allFields.ContainsKey("Model_")
                                    ? allFields["Model_"]
                                    : null,
                                Dimension = allFields.ContainsKey("Dimension")
                                    ? allFields["Dimension"]
                                    : null,
                                Size = allFields.ContainsKey("Size")
                                    ? allFields["Size"]
                                    : null,
                                Weight = allFields.ContainsKey("Weight")
                                    ? allFields["Weight"]
                                    : null,
                                Materials = allFields.ContainsKey("Materials")
                                    ? allFields["Materials"]
                                    : null,
                                Capacity = allFields.ContainsKey("Capacity")
                                    ? allFields["Capacity"]
                                    : null,
                                Color = allFields.ContainsKey("Color")
                                    ? allFields["Color"]
                                    : null
                            },

                    SetLotItems =
                        new List<SetLotItemReportPayload>(),

                    Allocations =
                        new List<AllocationReportPayload>()
                };


                // ========================================================
                // SET / LOT COMPONENTS
                // ========================================================
                

                foreach (var sub in item.OrderSubItems
                                           .OrderBy(x => x.ItemNo))
                {
                    reportItem.SetLotItems.Add(
                        new SetLotItemReportPayload
                        {
                            //RequestSubItemId =
                            //    sub.RequestSubItemId,
                            ItemNo = sub.ItemNo,
                            ItemName = sub.Description,
                            Unit = sub.Unit,
                            Qty = sub.QtyPerSet,
                            EstimatedCost =
                                sub.EstimatedTotalCost ?? 0
                        });
                }


                // ========================================================
                // SOURCE PR ALLOCATIONS
                // ========================================================

                //foreach (var allocation in item.OrderItemRequests)
                //{
                //    reportItem.Allocations.Add(
                //        new AllocationReportPayload
                //        {
                //            // Prefer the actual Request/PR ID.
                //            PRId = allocation.RequestId,

                //            RequestItemId =
                //                allocation.RequestItemId,

                //            PRNumber =
                //                allocation.PRNumber,

                //            Quantity =
                //                allocation.Qty
                //        });
                //}

                payload.Items.Add(reportItem);
            }


            // ============================================================
            // SOURCE PRs
            // ============================================================
            //
            // BEST OPTION:
            // Build SourcePRs from the actual Request navigation/entity so
            // DepartmentId and DepartmentName are authoritative.
            //
            // Example:
            //
            // var sourceRequestIds = payload.Items
            //     .SelectMany(x => x.Allocations)
            //     .Where(x => x.PRId != null)
            //     .Select(x => (Guid)x.PRId)
            //     .Distinct()
            //     .ToList();
            //
            // var requests = db.Requests
            //     .AsNoTracking()
            //     .Where(x => sourceRequestIds.Contains(x.Id))
            //     .ToList();
            //
            // payload.SourcePRs = requests
            //     .Select(x => new SourcePRReportPayload
            //     {
            //         PRId = x.Id,
            //         PRNumber = x.PrNo,
            //         DepartmentId = x.DepartmentId,
            //         DepartmentName = x.DepartmentName
            //     })
            //     .ToList();


            // ============================================================
            // OPTIONAL DOCUMENTS
            // ============================================================
            //
            // Map your actual saved PO document entities if they are
            // required by the report.
            //
            // payload.POCopyDoc = ...
            // payload.AdditionalDocs = ...


            // ============================================================
            // SUMMARY VALUES
            // ============================================================
            //
            // You can leave these blank. The Crystal service automatically
            // derives them from SourcePRs.
            //
            // payload.PRNumber = ...
            // payload.DepartmentName = ...

            return payload;            

            // Remove this line after you insert your actual EF mapping above.
            //throw new NotImplementedException(
            //    "Map your saved Order entity to POGroupReportPayload " +
            //    "inside BuildActualPOPayload().");
        }


        // ================================================================
        // ERROR HELPER
        // ================================================================

        private static string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();

            while (ex != null)
            {
                if (!string.IsNullOrWhiteSpace(ex.Message))
                    messages.Add(ex.Message);

                ex = ex.InnerException;
            }

            return string.Join(" -> ", messages);
        }

    }
}
