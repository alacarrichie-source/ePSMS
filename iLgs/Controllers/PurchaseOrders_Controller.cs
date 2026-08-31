using iLgs.Models;
using iLgs.Services;
using iLgs.Services.PurchaseOrder;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using iLgs.Core.Services;
using iLgs.Core.ViewModels;
//using iLgs.Infrastructure.Hubs;

namespace iLgs.Controllers
{
    [System.Web.Mvc.Authorize]
    public class PurchaseOrders_Controller : BaseController
    {
        private readonly Core.Services.IPurchaseOrderService _poService;
        private readonly IPurchaseRequestService _prService;
        private readonly ISupplierService _supplierService;
        private readonly IHubContext _hubContext;

        public PurchaseOrders_Controller()
        {
            _poService = new Core.Services.PurchaseOrderService(_db);
            _prService = new PurchaseRequestService(_db);
            _supplierService = new SupplierService(_db);
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<PORealtimeHub>();
        }

        // GET: /PurchaseOrders/Index
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Suppliers = _supplierService.GetActiveSuppliers();
            return View();
        }

        // POST: /PurchaseOrders/ReadPurchaseOrders (Kendo Grid DataSource)
        [HttpPost]
        public async Task<ActionResult> ReadPurchaseOrders([DataSourceRequest] DataSourceRequest request)
        {
            var query = _poService.GetPurchaseOrdersQueryable();
            var result = await query.ToDataSourceResultAsync(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // POST: /PurchaseOrders/GetPOItems (Sub-Grid Hierarchy)
        [HttpPost]
        public async Task<ActionResult> GetPOItems(string poId, [DataSourceRequest] DataSourceRequest request)
        {
            var items = await _poService.GetItemsByPOIdAsync(poId);
            return Json(await items.ToDataSourceResultAsync(request));
        }

        // GET: /PurchaseOrders/GetAvailablePRs (Step 1 PR Selector Grid)
        [HttpGet]
        public async Task<ActionResult> GetAvailablePRs()
        {
            var approvedPRs = await _prService.GetApprovedForPOAsync();
            return Json(new { success = true, data = approvedPRs }, JsonRequestBehavior.AllowGet);
        }

        // POST: /PurchaseOrders/GetPRItemsForAllocation (Step 2 Item Breakdown)
        [HttpPost]
        public async Task<ActionResult> GetPRItemsForAllocation(List<string> prIds)
        {
            if (prIds == null || !prIds.Any())
                return Json(new { success = false, message = "No Purchase Requests selected." });

            var items = await _prService.GetItemsByPRIdsAsync(prIds);
            return Json(new { success = true, data = items });
        }

        // GET: /PurchaseOrders/GetPODetails
        [HttpGet]
        public async Task<ActionResult> GetPODetails(string id)
        {
            var po = await _poService.GetByIdAsync(id);
            if (po == null) return HttpNotFound();
            return PartialView("_POViewerPrintModal", po);
        }

        // POST: /PurchaseOrders/SaveDraftPurchaseOrders (Wizard Step 4 - Draft)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveDraftPurchaseOrders(CreatePOWizardInputModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Validation errors occurred.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            
            var createdPOs = await _poService.CreatePurchaseOrdersAsync(model, isDraft: true, createdBy: User.Identity.Name);

            foreach (var po in createdPOs)
            {
                _hubContext.Clients.All.broadcastPOStatusChange(new
                {
                    Id = po.Id,
                    PONumber = po.PONumber,
                    SupplierName = po.SupplierName,
                    Status = "DRAFT",
                    TotalAmount = po.TotalAmount,
                    CreatedBy = User.Identity.Name
                });
            }

            return Json(new { success = true, count = createdPOs.Count, poIds = createdPOs.Select(p => p.Id) });
        }

        // POST: /PurchaseOrders/PostAndGeneratePurchaseOrders (Wizard Step 4 - Post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostAndGeneratePurchaseOrders(CreatePOWizardInputModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Please complete all statutory compliance validations." });

            var createdPOs = await _poService.CreatePurchaseOrdersAsync(model, isDraft: false, createdBy: User.Identity.Name);

            // SignalR notification to all active procurement officers
            foreach (var po in createdPOs)
            {
                _hubContext.Clients.All.broadcastPOStatusChange(new
                {
                    Id = po.Id,
                    PONumber = po.PONumber,
                    SupplierName = po.SupplierName,
                    Status = "POSTED",
                    TotalAmount = po.TotalAmount,
                    CreatedBy = User.Identity.Name
                });
            }

            return Json(new { success = true, count = createdPOs.Count, poIds = createdPOs.Select(p => p.Id) });
        }

        // POST: /PurchaseOrders/PostSinglePO
        [HttpPost]
        public async Task<ActionResult> PostSinglePO(string poId)
        {
            var updated = await _poService.PostPurchaseOrderAsync(poId, User.Identity.Name);

            _hubContext.Clients.All.broadcastPOStatusChange(new
            {
                Id = updated.Id,
                PONumber = updated.PONumber,
                Status = "POSTED",
                UpdatedBy = User.Identity.Name
            });

            return Json(new { success = true, po = updated });
        }

        // GET: /PurchaseOrders/ExportPDF
        [HttpGet]
        public async Task<ActionResult> ExportPDF(string id)
        {
            var pdfBytes = await _poService.GenerateOfficialPOReportPdfAsync(id);
            return File(pdfBytes, "application/pdf", $"PO_{id}_COA_Official.pdf");
        }
    }
}