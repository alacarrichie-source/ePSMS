using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using iLgs.Services.Ai.PurchaseOrder;
using iLgs.Ai.Models;

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

        // GET: PurchaseOrders/Index
        public ActionResult Index()
        {
            ViewBag.Title = "Purchase Orders Management";
            return View();
        }

        // POST: PurchaseOrders/PurchaseOrders_Read
        [HttpPost]
        public ActionResult PurchaseOrders_Read([DataSourceRequest] DataSourceRequest request)
        {
            var query = _poService.GetPurchaseOrdersGrid();
            var result = query.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // GET: PurchaseOrders/GetPODetailsModal/5
        [HttpGet]
        public async Task<ActionResult> GetPODetailsModal(Guid id)
        {
            var model = await _poService.GetPODetailsAsync(id);
            if (model == null) return HttpNotFound("Purchase Order not found.");

            return PartialView("_PODetailsModal", model);
        }

        // GET: PurchaseOrders/Print/5 (GAM Appendix 61 Form 101-A)
        [HttpGet]
        public async Task<ActionResult> Print(Guid id)
        {
            var model = await _poService.GetPODetailsAsync(id);
            if (model == null) return HttpNotFound("Purchase Order not found.");

            return View("Print", model);
        }

        // GET: PurchaseOrders/DownloadPdf/5
        [HttpGet]
        public async Task<ActionResult> DownloadPdf(Guid id)
        {
            var model = await _poService.GetPODetailsAsync(id);
            if (model == null) return HttpNotFound("Purchase Order not found.");

            var pdfBytes = await _pdfService.GeneratePurchaseOrderForm101APdfAsync(model);
            var fileName = $"PO_Form_101A_{model.PONumber}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        // POST: PurchaseOrders/PostPO
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: PurchaseOrders/CancelPO
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // 2. 4-STEP PO CREATION WIZARD
        // ==========================================

        // GET: PurchaseOrders/Wizard
        public ActionResult Wizard()
        {
            ViewBag.Title = "Create Purchase Order Wizard";
            var model = new POWizardViewModel();
            return View("Wizard", model);
        }

        // POST: PurchaseOrders/GetApprovedPRs_Read
        [HttpPost]
        public ActionResult GetApprovedPRs_Read([DataSourceRequest] DataSourceRequest request)
        {
            var query = _prService.GetApprovedPRsGrid();
            var result = query.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // POST: PurchaseOrders/GetSelectedPRLineItems
        [HttpPost]
        public async Task<ActionResult> GetSelectedPRLineItems(string[] prIds)
        {
            if (prIds == null || prIds.Length == 0)
            {
                return Json(new { success = false, message = "No Purchase Requests selected." });
            }

            var items = await _prService.GetPRItemsForAllocationAsync(prIds);
            return Json(new { success = true, items = items });
        }

        // GET: PurchaseOrders/GetSuppliersDropdown
        [HttpGet]
        public async Task<ActionResult> GetSuppliersDropdown()
        {
            var suppliers = await _supplierService.GetActiveSuppliersAsync();
            return Json(suppliers, JsonRequestBehavior.AllowGet);
        }

        // POST: PurchaseOrders/UploadPOCopy
        [HttpPost]
        public ActionResult UploadPOCopy(string groupId, HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return Json(new { success = false, message = "No file uploaded." });
            }

            var doc = _docService.SaveUploadedFile(file, "POCopies", "POCopy");
            return Json(new { success = true, document = doc, groupId = groupId });
        }

        // POST: PurchaseOrders/UploadAdditionalDoc
        [HttpPost]
        public ActionResult UploadAdditionalDoc(string groupId, HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return Json(new { success = false, message = "No file uploaded." });
            }

            var doc = _docService.SaveUploadedFile(file, "SupportingDocs", "Additional");
            return Json(new { success = true, document = doc, groupId = groupId });
        }

        // POST: PurchaseOrders/SaveDraft
        [HttpPost]
        public async Task<ActionResult> SaveDraft(List<POGroupDraftViewModel> poGroups)
        {
            if (poGroups == null || !poGroups.Any())
            {
                return Json(new { success = false, message = "No PO groups to save." });
            }

            try
            {
                var user = User.Identity.Name ?? "Admin";
                var created = await _poService.CreatePOsFromWizardAsync(poGroups, user, isDraft: true);
                var poNumbers = string.Join(", ", created.Select(p => p.PoNo));

                return Json(new
                {
                    success = true,
                    message = $"Draft Purchase Order(s) {poNumbers} saved successfully.",
                    redirectUrl = Url.Action("Index", "PurchaseOrders")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: PurchaseOrders/PostPOs
        [HttpPost]
        public async Task<ActionResult> PostPOs(List<POGroupDraftViewModel> poGroups)
        {
            if (poGroups == null || !poGroups.Any())
            {
                return Json(new { success = false, message = "No PO groups to post." });
            }

            // Validation: Ensure supplier selected and items assigned
            foreach (var grp in poGroups)
            {
                if (grp.SupplierId == null)
                {
                    return Json(new { success = false, message = $"Please select a Supplier for {grp.GroupName}." });
                }
                if (grp.Items == null || !grp.Items.Any())
                {
                    return Json(new { success = false, message = $"{grp.GroupName} has no assigned items." });
                }
            }

            try
            {
                var user = User.Identity.Name ?? "Admin";
                var created = await _poService.CreatePOsFromWizardAsync(poGroups, user, isDraft: false);
                var poNumbers = string.Join(", ", created.Select(p => p.PoNo));

                return Json(new
                {
                    success = true,
                    message = $"Purchase Order(s) {poNumbers} generated and posted successfully!",
                    redirectUrl = Url.Action("Index", "PurchaseOrders")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // 3. DOCUMENT DOWNLOAD & PREVIEW
        // ==========================================

        // GET: PurchaseOrders/DownloadDocument/5
        [HttpGet]
        public ActionResult DownloadDocument(string path, string name)
        {
            var fileBytes = _docService.GetFileBytes(path);
            if (fileBytes == null) return HttpNotFound("File not found.");

            return File(fileBytes, "application/octet-stream", name);
        }

        // GET: PurchaseOrders/PreviewDocument/5
        [HttpGet]
        public ActionResult PreviewDocument(string path)
        {
            var fileBytes = _docService.GetFileBytes(path);
            if (fileBytes == null) return HttpNotFound("File not found.");

            return File(fileBytes, "application/pdf");
        }
    }
}