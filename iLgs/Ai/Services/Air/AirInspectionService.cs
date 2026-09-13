using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Ai.Models;
using iLgs.Models;
using iLgs.Services.AIRs_;

namespace iLgs.Ai.Services.Air
{
    public interface IAirInspectionService
    {
        Task<List<AIRGridItemViewModel>> GetAirListAsync(string statusFilter, bool acceptanceQueue);
        Task<List<AIRWizardDraftListItemViewModel>> GetMyDraftsAsync(string user);
        Task<List<POInspectionCandidateViewModel>> GetEligiblePurchaseOrdersAsync();
        Task<AIRWizardViewModel> GetPOInspectionDetailsAsync(Guid orderId, Guid? airId = null);
        Task<AIRWizardViewModel> GetAirInspectionForEditAsync(Guid airId);
        Task<AIRWizardViewModel> GetDraftForEditAsync(Guid draftId);
        Task<Guid> SaveInspectionDraftAsync(AIRWizardViewModel model, string user);
        Task<Guid> SubmitForAcceptanceAsync(AIRWizardViewModel model, string user);
        Task<Guid> WithdrawSubmissionAsync(Guid airId, string reason, string user);
        Task RequestWithdrawalAsync(Guid airId, string reason, string user);
        Task<AIRItemHistoryViewModel> GetItemInspectionHistoryAsync(Guid orderItemId);
        Task DiscardDraftAsync(Guid draftId, string user);
        bool CanWithdrawAir(AIR air);
        bool CanDiscardAir(AIR air, out string reason);
        Task<AIRWizardViewModel> ChangeDraftPOAsync(Guid draftId, Guid newOrderId, string user);
        Task<AIRSubmissionViewViewModel> GetSubmissionViewAsync(Guid airId, Guid? snapshotId = null);
        Task<AIRWizardViewModel> GetSubmittedWizardViewModelAsync(Guid airId, Guid? snapshotId = null);
    }

    public class AirInspectionService : IAirInspectionService
    {
        private readonly AppManEntities _db;
        private readonly IDocumentHistoryService _historyService;
        private readonly IAirItemAbstractService _airItemSharedService;

        public AirInspectionService(AppManEntities db, IDocumentHistoryService historyService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _airItemSharedService = new AirItemAbstractService(_db);
        }

        public async Task<List<AIRGridItemViewModel>> GetAirListAsync(string statusFilter, bool acceptanceQueue)
        {
            var query = _db.AIRs.AsNoTracking().Include(a => a.Order).Include(a => a.AIRItems);

            var list = await query.OrderByDescending(a => a.InsertedDt ?? a.AIRDate).ToListAsync();
            var inventoryPostedIds = await GetInventoryPostedAirIdsAsync();
            var result = new List<AIRGridItemViewModel>();

            foreach (var a in list)
            {
                var overallStatus = ResolveOverallStatus(a);
                var inventoryPosted = inventoryPostedIds.Contains(a.Id);

                // Exclude drafts, withdrawn, and deleted records from Acceptance queue
                if (acceptanceQueue &&
                    (string.Equals(overallStatus, AirStatuses.Draft, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(overallStatus, AirStatuses.Withdrawn, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(overallStatus, AirStatuses.ReturnedForRevision, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(overallStatus, statusFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var vm = new AIRGridItemViewModel
                {
                    Id = a.Id,
                    AirNo = a.AIRNo ?? a.CtrlNo ?? "Draft",
                    CtrlNo = a.CtrlNo,
                    AirDate = a.AIRDate,
                    OrderId = a.OrderId,
                    PONumber = a.Order?.PoNo ?? "N/A",
                    PODate = a.Order?.PoDate,
                    SupplierName = a.Order?.SupName ?? "N/A",
                    Department = a.Order?.Department ?? "N/A",
                    InvoiceNo = a.InvoiceNo ?? "N/A",
                    DrNo = a.DrNo,
                    ItemsCount = a.AIRItems.Count,
                    TotalInspectedQty = a.AIRItems.Sum(i => i.Qty ?? 0),
                    OverallStatus = overallStatus,
                    InspectionStatus = a.PostedDt != null ? AirInspectionStatuses.Posted : (a.IsInspected == true ? AirInspectionStatuses.Submitted : AirInspectionStatuses.Draft),
                    AcceptanceStatus = a.PostedDt != null ? AirAcceptanceStatuses.Accepted : (string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase) ? AirAcceptanceStatuses.Unposted : (string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase) ? AirAcceptanceStatuses.Deleted : (a.AcceptedDate != null ? AirAcceptanceStatuses.Accepted : (a.AcceptanceStartedDt != null ? AirAcceptanceStatuses.InProgress : AirAcceptanceStatuses.Pending)))),
                    RevisionComments = a.RevisionComments,
                    WithdrawalReason = a.WithdrawalReason,
                    WithdrawalRequested = a.WithdrawalRequested == true,

                    CanContinue = a.IsInspected != true && a.PostedDt == null,
                    CanWithdraw = CanWithdrawAir(a),
                    CanRequestWithdrawal = false,
                    CanRevise = false,
                    CanStartAcceptance = acceptanceQueue && overallStatus == AirStatuses.SubmittedForAcceptance && a.IsInspected == true && a.AcceptanceStartedDt == null && a.PostedDt == null,
                    CanContinueAcceptance = acceptanceQueue && (overallStatus == AirStatuses.AcceptanceInProgress || overallStatus == AirStatuses.Unposted || string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase)) && a.PostedDt == null,
                    CanPost = false,
                    InventoryPosted = inventoryPosted,
                    CanPostToInventory = acceptanceQueue && overallStatus == AirStatuses.Posted && a.AcceptanceStatus == AirAcceptanceStatuses.Accepted && !inventoryPosted,
                    CanPrint = a.PostedDt != null,
                    CanViewAcceptance = a.AcceptanceStartedDt != null || a.AcceptedDate != null || a.PostedDt != null || string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase) || string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase),
                    CanUnpost = acceptanceQueue && a.PostedDt != null && !string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase),
                    CanDelete = acceptanceQueue && (string.Equals(overallStatus, AirStatuses.Unposted, StringComparison.OrdinalIgnoreCase) || string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase))
                };

                result.Add(vm);
            }

            return result;
        }

        private async Task<HashSet<Guid>> GetInventoryPostedAirIdsAsync()
        {
            int columnCount = await _db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM sys.columns " +
                "WHERE object_id = OBJECT_ID('dbo.PsCardItems') " +
                "AND name = 'AIRItemId'")
                .SingleAsync();

            if (columnCount == 0)
            {
                var historicalIds = await _db.DocumentStatusHistories.AsNoTracking()
                    .Where(h =>
                        h.DocumentType == DocumentTypes.AcceptanceInspectionReport &&
                        h.Action == "AIR_INVENTORY_POSTED")
                    .Select(h => h.DocumentId)
                    .ToListAsync();

                return new HashSet<Guid>(historicalIds);
            }

            var currentIds = await _db.Database.SqlQuery<Guid>(
                "SELECT DISTINCT ai.AirId " +
                "FROM dbo.PsCardItems pci " +
                "INNER JOIN dbo.AIRItems ai ON ai.Id = pci.AIRItemId " +
                "WHERE ai.AirId IS NOT NULL")
                .ToListAsync();

            return new HashSet<Guid>(currentIds);
        }

        public async Task<List<AIRWizardDraftListItemViewModel>> GetMyDraftsAsync(string user)
        {
            var progressList = await _db.AIRWizardProgresses.AsNoTracking()
                .Where(p => p.Status == AirWizardStatuses.Draft && !p.IsCompleted && (p.UserId == user || p.CreatedBy == user))
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();

            var orderIds = progressList.Where(p => p.OrderId.HasValue).Select(p => p.OrderId.Value).Distinct().ToList();
            var orderDict = await _db.Orders.AsNoTracking()
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.PoNo, o.SupName })
                .ToDictionaryAsync(o => o.Id);

            var sourceAirIds = progressList.Where(p => p.SourceAIRId.HasValue).Select(p => p.SourceAIRId.Value).Distinct().ToList();
            var sourceAirDict = await _db.AIRs.AsNoTracking()
                .Where(a => sourceAirIds.Contains(a.Id))
                .Select(a => new { a.Id, a.AIRNo, a.CtrlNo })
                .ToDictionaryAsync(a => a.Id, a => !string.IsNullOrEmpty(a.AIRNo) ? a.AIRNo : (a.CtrlNo ?? "AIR"));

            var result = new List<AIRWizardDraftListItemViewModel>();

            foreach (var p in progressList)
            {
                string poNo = "N/A";
                string supName = "N/A";
                if (p.OrderId.HasValue && orderDict.ContainsKey(p.OrderId.Value))
                {
                    poNo = orderDict[p.OrderId.Value].PoNo ?? "N/A";
                    supName = orderDict[p.OrderId.Value].SupName ?? "N/A";
                }
                else if (!string.IsNullOrEmpty(p.WizardStateJson))
                {
                    try
                    {
                        var jsonModel = JsonConvert.DeserializeObject<AIRWizardViewModel>(p.WizardStateJson);
                        if (jsonModel != null)
                        {
                            if (!string.IsNullOrEmpty(jsonModel.PONumber)) poNo = jsonModel.PONumber;
                            if (!string.IsNullOrEmpty(jsonModel.SupplierName)) supName = jsonModel.SupplierName;
                        }
                    }
                    catch { }
                }

                var isRev = p.SourceAIRId.HasValue || p.RevisionNo > 0;
                string srcAirNo = null;
                if (p.SourceAIRId.HasValue && sourceAirDict.ContainsKey(p.SourceAIRId.Value))
                {
                    srcAirNo = sourceAirDict[p.SourceAIRId.Value];
                }

                result.Add(new AIRWizardDraftListItemViewModel
                {
                    Id = p.Id,
                    DraftNo = p.DraftNo ?? p.Id.ToString().Substring(0, 8),
                    PONumber = poNo,
                    SupplierName = supName,
                    CurrentStep = p.CurrentStep > 0 ? p.CurrentStep : (p.LastStep > 0 ? p.LastStep : 1),
                    CreatedAt = p.CreatedAt,
                    LastUpdatedAt = p.UpdatedAt ?? p.CreatedAt,
                    Status = p.Status,
                    RevisionNo = p.RevisionNo,
                    SourceAIRId = p.SourceAIRId,
                    SourceAIRNo = srcAirNo,
                    IsRevision = isRev,
                    DraftType = isRev ? "Revision" : "New Inspection"
                });
            }

            var existingDraftAirIds = progressList.Where(p => p.AIRId.HasValue).Select(p => p.AIRId.Value).ToList();
            var legacyDrafts = await _db.AIRs.AsNoTracking()
                .Where(a => a.IsInspected != true && a.PostedDt == null && a.OverallStatus != AirStatuses.Cancelled && (a.InsertedBy == user || a.UpdatedBy == user))
                .Where(a => !existingDraftAirIds.Contains(a.Id))
                .OrderByDescending(a => a.UpdatedDt ?? a.InsertedDt)
                .Select(a => new AIRWizardDraftListItemViewModel
                {
                    Id = a.Id,
                    DraftNo = a.CtrlNo ?? a.AIRNo ?? "LEGACY-DRAFT",
                    PONumber = a.Order != null ? a.Order.PoNo : "N/A",
                    SupplierName = a.Order != null ? a.Order.SupName : "N/A",
                    CurrentStep = 1,
                    CreatedAt = a.InsertedDt ?? DateTime.Today,
                    LastUpdatedAt = a.UpdatedDt ?? a.InsertedDt ?? DateTime.Today,
                    Status = AirWizardStatuses.Draft,
                    RevisionNo = 0,
                    SourceAIRId = (Guid?)null,
                    SourceAIRNo = (string)null,
                    IsRevision = false,
                    DraftType = "New Inspection"
                })
                .ToListAsync();

            result.AddRange(legacyDrafts);

            return result.OrderByDescending(r => r.LastUpdatedAt).ToList();
        }

        public async Task<List<POInspectionCandidateViewModel>> GetEligiblePurchaseOrdersAsync()
        {
            var orders = await _db.Orders.AsNoTracking()
                .Include(o => o.OrderItems.Select(oi => oi.OrderItemRequests))
                .Where(o => o.OrderItems.Any())
                .OrderByDescending(o => o.PoDate ?? o.InsertedDt)
                .ToListAsync();

            var orderIds = orders.Select(o => o.Id).ToList();

            var postedAirItems = await _db.AIRItems.AsNoTracking()
                .Where(ai => ai.AIR.PostedDt != null && ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItem.OrderId.HasValue && orderIds.Contains(ai.OrderItemRequest.OrderItem.OrderId.Value))
                .Select(ai => new
                {
                    OrderId = ai.OrderItemRequest.OrderItem.OrderId.Value,
                    Qty = ai.Qty ?? 0
                })
                .ToListAsync();

            var postedTotals = postedAirItems
                .GroupBy(x => x.OrderId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var result = new List<POInspectionCandidateViewModel>();

            foreach (var o in orders)
            {
                decimal totalOrdered = o.OrderItems.Sum(oi => 
                    oi.OrderItemRequests != null && oi.OrderItemRequests.Any() 
                        ? oi.OrderItemRequests.Sum(oir => (decimal?)oir.QtyApplied ?? 0) 
                        : (oi.Qty ?? 0));

                if (totalOrdered == 0)
                {
                    totalOrdered = o.OrderItems.Sum(oi => oi.Qty ?? 0);
                }

                decimal postedInspected = 0;
                if (postedTotals.ContainsKey(o.Id))
                {
                    postedInspected = postedTotals[o.Id];
                }

                decimal remaining = totalOrdered - postedInspected;
                if (remaining < 0) remaining = 0;

                string statusText;
                int progressPercent;

                if (postedInspected == 0)
                {
                    statusText = "Not Inspected";
                    progressPercent = 0;
                }
                else if (remaining > 0)
                {
                    statusText = "Partially Inspected";
                    progressPercent = totalOrdered > 0 ? (int)Math.Min(100, Math.Round((postedInspected / totalOrdered) * 100)) : 0;
                }
                else
                {
                    statusText = "Fully Inspected";
                    progressPercent = 100;
                }

                result.Add(new POInspectionCandidateViewModel
                {
                    OrderId = o.Id,
                    PONumber = o.PoNo,
                    PODate = o.PoDate,
                    PRNumber = o.PrNo,
                    Department = o.Department,
                    SupplierName = o.SupName,
                    Purpose = o.Department,
                    DeliveryPlace = o.DeliveryPlace,
                    ItemCount = o.OrderItems.Count,
                    TotalAmount = o.OrderItems.Sum(oi => oi.Amount ?? 0),
                    TotalOrderedQty = totalOrdered,
                    TotalPostedInspectedQty = postedInspected,
                    RemainingInspectableQty = remaining,
                    InspectionStatusText = statusText,
                    InspectionProgressPercent = progressPercent
                });
            }

            return result;
        }

        public async Task<AIRWizardViewModel> GetPOInspectionDetailsAsync(Guid orderId, Guid? airId = null)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems.Select(oi => oi.ItemCode.ItemType))
                .Include(o => o.OrderItems.Select(oi => oi.OrderItemRequests.Select(oir => oir.RequestItem.Request)))
                .Include(o => o.OrderItems.Select(oi => oi.OrderSubItems))
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) throw new InvalidOperationException("Purchase Order not found.");

            var vm = new AIRWizardViewModel
            {
                OrderId = order.Id,
                PONumber = order.PoNo,
                PODate = order.PoDate,
                PRNumber = order.PrNo,
                DepartmentName = order.Department,
                SupplierId = order.SupplierId,
                SupplierName = order.SupName,
                DeliveryPlace = order.DeliveryPlace,
                POTotalAmount = order.OrderItems.Sum(oi => oi.Amount ?? 0),
                AirDate = DateTime.Today,
                InspectionDate = null,
                InspectionLocation = order.DeliveryPlace ?? "GSO Warehouse"
            };

            AIR existingAir = null;
            if (airId.HasValue)
            {
                existingAir = await _db.AIRs
                    .Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns))
                    .Include(a => a.AIRItems.Select(ai => ai.AIRItemAllocations))
                    .Include(a => a.AIRInvoices)
                    .Include(a => a.AIRDocuments)
                    .FirstOrDefaultAsync(a => a.Id == airId.Value);

                if (existingAir != null)
                {
                    vm.AirId = existingAir.Id;
                    vm.AirNo = existingAir.AIRNo;
                    vm.CtrlNo = existingAir.CtrlNo;
                    vm.AirDate = existingAir.AIRDate ?? DateTime.Today;
                    vm.InspectionDate = existingAir.InspectedDate;
                    vm.InspectorName = existingAir.InspectorName ?? existingAir.Officer;
                    vm.InspectorDesignation = existingAir.InspectorDesignation;
                    vm.InspectionCommittee = existingAir.InspectionCommittee;
                    vm.InspectionLocation = existingAir.InspectionLocation ?? order.DeliveryPlace ?? "GSO Warehouse";
                    vm.GeneralRemarks = existingAir.Remarks;
                    vm.RevisionComments = existingAir.RevisionComments;
                    vm.Disposition = existingAir.Disposition ?? (existingAir.InvDist == "D" ? "For Distribution" : (existingAir.InvDist == "I" ? "Inventory" : existingAir.InvDist));

                    // Invoices from AIRInvoices table
                    if (existingAir.AIRInvoices != null && existingAir.AIRInvoices.Any())
                    {
                        vm.Invoices = existingAir.AIRInvoices
                            .OrderBy(inv => inv.InsertedDt ?? inv.InvoiceDate)
                            .Select(inv => {
                                var docPrefix = "InvoiceDoc:" + inv.Id.ToString("D");
                                var doc = existingAir.AIRDocuments != null 
                                    ? existingAir.AIRDocuments.FirstOrDefault(d => d.DocumentType == docPrefix) 
                                    : null;
                                return new AIRInvoiceViewModel
                                {
                                    Id = inv.Id,
                                    AirId = inv.AirId,
                                    SalesInvoiceNo = inv.InvoiceNo,
                                    InvoiceDate = inv.InvoiceDate,
                                    InvoiceTotalAmount = inv.Amount ?? 0m,
                                    SupportingDocument = doc != null ? new AIRDocumentViewModel
                                    {
                                        Id = doc.Id,
                                        AirId = doc.AirId,
                                        DocumentType = doc.DocumentType,
                                        FileName = doc.FileName,
                                        FilePath = doc.FilePath,
                                        FileSize = doc.FileSize,
                                        UploadedBy = doc.UploadedBy,
                                        UploadedDt = doc.UploadedDt
                                    } : null
                                };
                            }).ToList();
                    }

                    // General supplier delivery documents
                    if (existingAir.AIRDocuments != null)
                    {
                        vm.SupportingDocuments = existingAir.AIRDocuments
                            .Where(d => !d.DocumentType.StartsWith("InvoiceDoc:"))
                            .OrderBy(d => d.UploadedDt)
                            .Select(doc => new AIRDocumentViewModel
                            {
                                Id = doc.Id,
                                AirId = doc.AirId,
                                DocumentType = doc.DocumentType,
                                FileName = doc.FileName,
                                FilePath = doc.FilePath,
                                FileSize = doc.FileSize,
                                UploadedBy = doc.UploadedBy,
                                UploadedDt = doc.UploadedDt
                            }).ToList();
                    }
                }
            }

            foreach (var oi in order.OrderItems.OrderBy(i => i.ItemNo))
            {
                var oirList = oi.OrderItemRequests != null && oi.OrderItemRequests.Any() 
                    ? oi.OrderItemRequests.ToList() 
                    : await _db.OrderItemRequests.Include(r => r.RequestItem.Request).Where(r => r.OrderItemId == oi.Id).ToListAsync();
                var oirIds = oirList.Select(r => r.Id).ToList();
                var oir = oirList.FirstOrDefault();
                Guid oirId = oir != null ? oir.Id : Guid.Empty;

                var prSources = oirList
                    .Where(r => r.RequestItem != null && r.RequestItem.Request != null)
                    .Select(r => new AIRItemSourceAllocationViewModel
                    {
                        PRNumber = r.RequestItem.Request.PrNo,
                        Department = r.RequestItem.Request.Department,
                        Qty = r.QtyApplied ?? 0
                    })
                    .ToList();

                string prNumber = prSources.Any()
                    ? string.Join(", ", prSources.Select(s => s.PRNumber).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
                    : (order.PrNo ?? string.Empty);

                string department = prSources.Any()
                    ? string.Join(", ", prSources.Select(s => s.Department).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct())
                    : (order.Department ?? string.Empty);

                decimal orderedQty = oirList.Sum(r => (decimal?)r.QtyApplied ?? 0);
                if (orderedQty == 0) orderedQty = oi.Qty ?? 0;

                AIRItem existingItem = null;
                if (existingAir != null)
                {
                    existingItem = existingAir.AIRItems.FirstOrDefault(ai => 
                        (ai.OrderItemRequestId.HasValue && oirIds.Contains(ai.OrderItemRequestId.Value)) ||
                        (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == oi.Id) ||
                        (ai.AIRItemAllocations != null && ai.AIRItemAllocations.Any(al => oirIds.Contains(al.OrderItemRequestId))));
                }

                // Query posted quantities per OrderItemRequestId from AIRItemAllocations and historical AIRItems
                var postedAllocMap = oirIds.Any()
                    ? (await _db.AIRItemAllocations
                        .Where(a => oirIds.Contains(a.OrderItemRequestId) &&
                                    a.AIRItem.AIR.PostedDt != null &&
                                    (airId == null || a.AIRItem.AirId != airId.Value))
                        .GroupBy(a => a.OrderItemRequestId)
                        .Select(g => new { OrderItemRequestId = g.Key, TotalInspected = g.Sum(x => (decimal?)x.QtyInspected) ?? 0 })
                        .ToListAsync())
                        .ToDictionary(x => x.OrderItemRequestId, x => x.TotalInspected)
                    : new Dictionary<Guid, decimal>();

                var historicalPostedMap = oirIds.Any()
                    ? (await _db.AIRItems
                        .Where(ai => ai.AIR.PostedDt != null &&
                                     (airId == null || ai.AirId != airId.Value) &&
                                     !ai.AIRItemAllocations.Any() &&
                                     ai.OrderItemRequestId.HasValue &&
                                     oirIds.Contains(ai.OrderItemRequestId.Value))
                        .GroupBy(ai => ai.OrderItemRequestId.Value)
                        .Select(g => new { OrderItemRequestId = g.Key, TotalQty = g.Sum(x => (decimal?)x.Qty) ?? 0 })
                        .ToListAsync())
                        .ToDictionary(x => x.OrderItemRequestId, x => x.TotalQty)
                    : new Dictionary<Guid, decimal>();

                var allocationsList = new List<AIRItemAllocationViewModel>();
                foreach (var oirItem in oirList)
                {
                    decimal prevForOir = 0;
                    if (postedAllocMap.ContainsKey(oirItem.Id)) prevForOir += postedAllocMap[oirItem.Id];
                    if (historicalPostedMap.ContainsKey(oirItem.Id)) prevForOir += historicalPostedMap[oirItem.Id];

                    decimal allocatedForOir = (decimal?)oirItem.QtyApplied ?? 0;
                    if (allocatedForOir == 0 && oirList.Count == 1)
                    {
                        allocatedForOir = oi.Qty ?? 0;
                    }

                    AIRItemAllocation existingAlloc = null;
                    if (existingItem != null && existingItem.AIRItemAllocations != null && existingItem.AIRItemAllocations.Any())
                    {
                        existingAlloc = existingItem.AIRItemAllocations.FirstOrDefault(a => a.OrderItemRequestId == oirItem.Id);
                    }

                    decimal currentInspectForOir = 0;
                    Guid? allocId = null;
                    string allocRemarks = null;

                    if (existingAlloc != null)
                    {
                        currentInspectForOir = existingAlloc.QtyInspected;
                        allocId = existingAlloc.Id;
                        allocRemarks = existingAlloc.Remarks;
                    }
                    else if (existingItem != null)
                    {
                        if (oirList.Count == 1 || (existingItem.OrderItemRequestId.HasValue && existingItem.OrderItemRequestId.Value == oirItem.Id))
                        {
                            currentInspectForOir = existingItem.Qty ?? 0;
                            allocRemarks = existingItem.Remarks;
                        }
                    }

                    allocationsList.Add(new AIRItemAllocationViewModel
                    {
                        Id = allocId,
                        OrderItemRequestId = oirItem.Id,
                        RequestId = oirItem.RequestItem != null ? oirItem.RequestItem.PrId : (Guid?)null,
                        RequestItemId = oirItem.RequestItemId,
                        DepartmentId = oirItem.RequestItem != null && oirItem.RequestItem.Request != null ? oirItem.RequestItem.Request.DeptId : (Guid?)null,
                        QtyAllocated = allocatedForOir,
                        QtyInspected = currentInspectForOir,
                        PreviousInspected = prevForOir,
                        PRNumber = oirItem.RequestItem != null && oirItem.RequestItem.Request != null ? oirItem.RequestItem.Request.PrNo : (order.PrNo ?? string.Empty),
                        Department = oirItem.RequestItem != null && oirItem.RequestItem.Request != null ? oirItem.RequestItem.Request.Department : (order.Department ?? string.Empty),
                        Remarks = allocRemarks
                    });
                }

                if (!allocationsList.Any())
                {
                    var defaultOir = await _db.OrderItemRequests.FirstOrDefaultAsync(r => r.OrderItemId == oi.Id);
                    Guid defOirId = defaultOir != null ? defaultOir.Id : Guid.NewGuid();
                    decimal defQty = defaultOir != null && defaultOir.QtyApplied.HasValue ? defaultOir.QtyApplied.Value : (oi.Qty ?? 0);
                    allocationsList.Add(new AIRItemAllocationViewModel
                    {
                        OrderItemRequestId = defOirId,
                        RequestId = defaultOir != null && defaultOir.RequestItem != null ? defaultOir.RequestItem.PrId : (Guid?)null,
                        RequestItemId = defaultOir != null ? defaultOir.RequestItemId : (Guid?)null,
                        DepartmentId = defaultOir != null && defaultOir.RequestItem != null && defaultOir.RequestItem.Request != null ? defaultOir.RequestItem.Request.DeptId : (Guid?)null,
                        QtyAllocated = defQty,
                        QtyInspected = existingItem != null ? (existingItem.Qty ?? 0) : 0,
                        PreviousInspected = 0,
                        PRNumber = order.PrNo ?? string.Empty,
                        Department = order.Department ?? string.Empty,
                        Remarks = existingItem != null ? existingItem.Remarks : null
                    });
                }

                Guid primaryOirId = allocationsList.First().OrderItemRequestId;
                decimal totalOrderedQty = allocationsList.Sum(a => a.QtyAllocated);
                decimal totalPrevInspected = allocationsList.Sum(a => a.PreviousInspected);
                decimal totalInspectNow = allocationsList.Sum(a => a.QtyInspected);
                string itemRemarks = existingItem != null ? existingItem.Remarks : string.Empty;

                // Determine extension type dynamically using _airItemSharedService
                string category = oi.ItemCode?.ItemType?.Code;
                string itemExtnName = _airItemSharedService.GetItemExtnNameByCategory(category);

                bool isSet = oi.OrderSubItems.Any() || (existingItem != null && _db.AIRSubItems.Any(s => s.AirItemId == existingItem.Id));
                bool parentRequiresInventory = false;
                if (!string.IsNullOrWhiteSpace(itemExtnName))
                {
                    if (isSet)
                    {
                        string pDesc = (oi.Description ?? oi.ItemName ?? "").ToLower();
                        if (pDesc.Contains("set") || pDesc.Contains("lot") || pDesc.Contains("kit") || pDesc.Contains("package") || pDesc.Contains("desktop") || pDesc.Contains("bundle"))
                        {
                            parentRequiresInventory = false; // Set bundle accountability resides in components
                        }
                        else
                        {
                            parentRequiresInventory = true;
                        }
                    }
                    else
                    {
                        parentRequiresInventory = true;
                    }
                }
                else
                {
                    itemExtnName = "ItemExtnOther";
                }

                var lineItem = new AIRLineItemViewModel
                {
                    AirItemId = existingItem != null ? (Guid?)existingItem.Id : null,
                    OrderItemId = oi.Id,
                    OrderItemRequestId = primaryOirId,
                    ItemNo = oi.ItemNo,
                    PRNumber = prNumber,
                    Department = department,
                    Sources = prSources,
                    Allocations = allocationsList,
                    PPMPCode = oi.PpmpCode,
                    Description = oi.Description ?? oi.ItemName,
                    Unit = oi.Unit,
                    UnitCost = oi.UnitCost ?? 0,
                    OrderedQty = totalOrderedQty,
                    PreviousInspectedQty = totalPrevInspected,
                    InspectNowQty = totalInspectNow,
                    Remarks = itemRemarks,
                    IsSetLot = isSet,
                    CategoryCode = category,
                    ItemExtnName = itemExtnName,
                    ParentRequiresInventory = parentRequiresInventory,
                    Disposition = existingItem != null ? (existingItem.Disposition 
                        ?? (existingItem.InvDist == "D" ? "For Distribution" : (existingItem.InvDist == "I" ? "Inventory" : existingItem.InvDist)))
                        : vm.Disposition
                };

                // Populate existing extension records if any (parent extensions have AIRItemId populated)
                if (existingItem != null && existingItem.AIRItemExtns != null && existingItem.AIRItemExtns.Any(e => e.AIRItemId == existingItem.Id))
                {
                    var orderedExtns = existingItem.AIRItemExtns.Where(e => e.AIRItemId == existingItem.Id).OrderBy(e => e.ContentNo).ToList();
                    int extnCursor = 0;

                    if (lineItem.Allocations != null && lineItem.Allocations.Any())
                    {
                        foreach (var alloc in lineItem.Allocations)
                        {
                            int allocUnitsCount = (int)Math.Floor(alloc.QtyInspected > 0 ? alloc.QtyInspected : (lineItem.Allocations.Count == 1 ? lineItem.InspectNowQty : 0));
                            for (int k = 0; k < allocUnitsCount && extnCursor < orderedExtns.Count; k++)
                            {
                                var extn = orderedExtns[extnCursor++];
                                var detail = new AIRItemInventoryDetailViewModel
                                {
                                    Id = extn.Id,
                                    AirItemId = extn.AIRItemId,
                                    ContentNo = extn.ContentNo ?? (lineItem.InventoryDetails.Count + 1),
                                    TContentNo = extn.TContentNo,
                                    SetLotNo = extn.SetLotNo,
                                    SetLotQtyNo = extn.SetLotQtyNo,
                                    OrderItemRequestId = alloc.OrderItemRequestId,
                                    RequestId = alloc.RequestId,
                                    RequestItemId = alloc.RequestItemId,
                                    PRNumber = alloc.PRNumber,
                                    DepartmentId = alloc.DepartmentId,
                                    DepartmentName = alloc.Department
                                };
                                PopulateDetailFromExtension(detail, extn);
                                detail.IsCompleted = IsDetailCompleted(detail, itemExtnName);
                                lineItem.InventoryDetails.Add(detail);
                            }
                        }
                    }

                    // Remaining unassigned existing extensions
                    while (extnCursor < orderedExtns.Count)
                    {
                        var extn = orderedExtns[extnCursor++];
                        var firstAlloc = lineItem.Allocations != null ? lineItem.Allocations.FirstOrDefault() : null;
                        var detail = new AIRItemInventoryDetailViewModel
                        {
                            Id = extn.Id,
                            AirItemId = extn.AIRItemId,
                            ContentNo = extn.ContentNo ?? (lineItem.InventoryDetails.Count + 1),
                            TContentNo = extn.TContentNo,
                            SetLotNo = extn.SetLotNo,
                            SetLotQtyNo = extn.SetLotQtyNo,
                            OrderItemRequestId = firstAlloc != null ? firstAlloc.OrderItemRequestId : (Guid?)null,
                            RequestId = firstAlloc != null ? firstAlloc.RequestId : (Guid?)null,
                            RequestItemId = firstAlloc != null ? firstAlloc.RequestItemId : (Guid?)null,
                            PRNumber = firstAlloc != null ? firstAlloc.PRNumber : lineItem.PRNumber,
                            DepartmentId = firstAlloc != null ? firstAlloc.DepartmentId : (Guid?)null,
                            DepartmentName = firstAlloc != null ? firstAlloc.Department : lineItem.Department
                        };
                        PopulateDetailFromExtension(detail, extn);
                        detail.IsCompleted = IsDetailCompleted(detail, itemExtnName);
                        lineItem.InventoryDetails.Add(detail);
                    }
                }

                // Reconcile units for lineItem if Disposition == "Inventory" and ParentRequiresInventory
                if (lineItem.Disposition == "Inventory" && lineItem.ParentRequiresInventory && lineItem.InspectNowQty > 0)
                {
                    if (lineItem.Allocations != null && lineItem.Allocations.Any())
                    {
                        foreach (var alloc in lineItem.Allocations)
                        {
                            int targetForAlloc = (int)Math.Floor(alloc.QtyInspected);
                            int existingCount = lineItem.InventoryDetails.Count(d => d.OrderItemRequestId == alloc.OrderItemRequestId);
                            for (int q = existingCount + 1; q <= targetForAlloc; q++)
                            {
                                int globalNo = lineItem.InventoryDetails.Count + 1;
                                lineItem.InventoryDetails.Add(new AIRItemInventoryDetailViewModel
                                {
                                    OrderItemRequestId = alloc.OrderItemRequestId,
                                    RequestId = alloc.RequestId,
                                    RequestItemId = alloc.RequestItemId,
                                    PRNumber = alloc.PRNumber,
                                    DepartmentId = alloc.DepartmentId,
                                    DepartmentName = alloc.Department,
                                    ContentNo = globalNo,
                                    TContentNo = globalNo,
                                    IsCompleted = false,
                                    Condition = "Good"
                                });
                            }
                        }
                    }
                    else
                    {
                        int targetCount = (int)Math.Floor(lineItem.InspectNowQty);
                        for (int q = lineItem.InventoryDetails.Count + 1; q <= targetCount; q++)
                        {
                            lineItem.InventoryDetails.Add(new AIRItemInventoryDetailViewModel
                            {
                                PRNumber = lineItem.PRNumber,
                                DepartmentName = lineItem.Department,
                                ContentNo = q,
                                TContentNo = q,
                                IsCompleted = false,
                                Condition = "Good"
                            });
                        }
                    }
                }
                // Sub-items if Set/Lot
                if (isSet)
                {
                    var existingSubItems = existingItem != null 
                        ? await _db.AIRSubItems.Where(s => s.AirItemId == existingItem.Id).ToListAsync() 
                        : new List<AIRSubItem>();
                    var subItemIds = existingSubItems.Select(s => s.Id).ToList();
                    var subExtns = subItemIds.Any()
                        ? await _db.AIRItemExtns.Where(e => e.AIRSubItemId.HasValue && subItemIds.Contains(e.AIRSubItemId.Value)).ToListAsync()
                        : new List<AIRItemExtn>();

                    foreach (var sub in oi.OrderSubItems.OrderBy(s => s.SortOrder))
                    {
                        decimal qtyPerParent = sub.QtyPerSet > 0 ? sub.QtyPerSet : 1;
                        decimal subExpected = orderedQty * qtyPerParent;

                        // Resolve sub-item category dynamically: check keywords first, then catalog match
                        string subCategory = null;
                        if (!string.IsNullOrWhiteSpace(sub.Description))
                        {
                            string dLow = sub.Description.ToLower();
                            if (dLow.Contains("cpu") || dLow.Contains("processor") || dLow.Contains("monitor") || 
                                dLow.Contains("display") || dLow.Contains("hard drive") || dLow.Contains("ssd") || 
                                dLow.Contains("printer") || dLow.Contains("scanner") || dLow.Contains("ups") || 
                                dLow.Contains("server") || dLow.Contains("laptop") || dLow.Contains("tablet") ||
                                dLow.Contains("machine") || dLow.Contains("equipment"))
                            {
                                subCategory = "E";
                            }
                            else if (dLow.Contains("vehicle") || dLow.Contains("truck") || dLow.Contains("car") || dLow.Contains("motorcycle"))
                            {
                                subCategory = "T";
                            }
                            else if (dLow.Contains("keyboard") || dLow.Contains("mouse") || dLow.Contains("pad") || 
                                     dLow.Contains("cable") || dLow.Contains("wire") || dLow.Contains("paper") || 
                                     dLow.Contains("toner") || dLow.Contains("cartridge") || dLow.Contains("supply") ||
                                     dLow.Contains("supplies") || dLow.Contains("consumable"))
                            {
                                subCategory = "X";
                            }
                            else
                            {
                                var matchedItemCode = await _db.ItemCodes.Include(ic => ic.ItemType)
                                    .FirstOrDefaultAsync(ic => ic.Description == sub.Description || ic.Description.StartsWith(sub.Description));
                                if (matchedItemCode != null && matchedItemCode.ItemType != null)
                                {
                                    subCategory = matchedItemCode.ItemType.Code;
                                }
                            }
                        }

                        string subExtnName = _airItemSharedService.GetItemExtnNameByCategory(subCategory);
                        bool subRequiresInventory = !string.IsNullOrWhiteSpace(subExtnName) && (lineItem.Disposition == "Inventory");

                        var existingSub = existingSubItems.FirstOrDefault(s => s.OrderSubItemId == sub.Id);
                        decimal subInspectNow = existingSub != null ? existingSub.InspectedQty : (lineItem.InspectNowQty * qtyPerParent);

                        var subVm = new AIRSubItemViewModel
                        {
                            AirSubItemId = existingSub?.Id,
                            OrderSubItemId = sub.Id,
                            OrderSubItemRequestId = sub.OrderSubItemRequests?.FirstOrDefault()?.Id,
                            SubItemNo = !string.IsNullOrWhiteSpace(sub.ItemNoIndex ?? sub.ItemNo) ? (sub.ItemNoIndex ?? sub.ItemNo).Trim().TrimStart('#').Trim() : null,
                            Description = sub.Description,
                            Unit = sub.Unit,
                            ExpectedQty = subExpected,
                            PreviousInspectedQty = 0,
                            InspectNowQty = subInspectNow,
                            Remarks = existingSub?.Remarks,
                            QtyPerParent = qtyPerParent,
                            CategoryCode = subCategory,
                            ItemExtnName = subExtnName,
                            RequiresInventory = subRequiresInventory,
                            SourceType = SubItemSourceTypes.Normalize(existingSub?.SourceType, true),
                            IsRequiredForBundle = true // ORDERED sub-items are ALWAYS required for bundle
                        };

                        if (existingSub != null)
                        {
                            var extnsForSub = subExtns.Where(e => e.AIRSubItemId == existingSub.Id).OrderBy(e => e.ContentNo).ToList();
                            int subExtnCursor = 0;

                            if (lineItem.Allocations != null && lineItem.Allocations.Any())
                            {
                                foreach (var alloc in lineItem.Allocations)
                                {
                                    int allocSubCount = (int)Math.Floor(alloc.QtyInspected * qtyPerParent);
                                    for (int k = 0; k < allocSubCount && subExtnCursor < extnsForSub.Count; k++)
                                    {
                                        var extn = extnsForSub[subExtnCursor++];
                                        var detail = new AIRItemInventoryDetailViewModel
                                        {
                                            Id = extn.Id,
                                            AirItemId = null,
                                            AirSubItemId = extn.AIRSubItemId,
                                            ContentNo = extn.ContentNo ?? (subVm.InventoryDetails.Count + 1),
                                            TContentNo = extn.TContentNo,
                                            SetLotNo = extn.SetLotNo,
                                            SetLotQtyNo = extn.SetLotQtyNo,
                                            OrderItemRequestId = alloc.OrderItemRequestId,
                                            RequestId = alloc.RequestId,
                                            RequestItemId = alloc.RequestItemId,
                                            PRNumber = alloc.PRNumber,
                                            DepartmentId = alloc.DepartmentId,
                                            DepartmentName = alloc.Department
                                        };
                                        PopulateDetailFromExtension(detail, extn);
                                        detail.IsCompleted = IsDetailCompleted(detail, subExtnName);
                                        subVm.InventoryDetails.Add(detail);
                                    }
                                }
                            }

                            while (subExtnCursor < extnsForSub.Count)
                            {
                                var extn = extnsForSub[subExtnCursor++];
                                var firstAlloc = lineItem.Allocations != null ? lineItem.Allocations.FirstOrDefault() : null;
                                var detail = new AIRItemInventoryDetailViewModel
                                {
                                    Id = extn.Id,
                                    AirItemId = null,
                                    AirSubItemId = extn.AIRSubItemId,
                                    ContentNo = extn.ContentNo ?? (subVm.InventoryDetails.Count + 1),
                                    TContentNo = extn.TContentNo,
                                    SetLotNo = extn.SetLotNo,
                                    SetLotQtyNo = extn.SetLotQtyNo,
                                    OrderItemRequestId = firstAlloc != null ? firstAlloc.OrderItemRequestId : (Guid?)null,
                                    RequestId = firstAlloc != null ? firstAlloc.RequestId : (Guid?)null,
                                    RequestItemId = firstAlloc != null ? firstAlloc.RequestItemId : (Guid?)null,
                                    PRNumber = firstAlloc != null ? firstAlloc.PRNumber : lineItem.PRNumber,
                                    DepartmentId = firstAlloc != null ? firstAlloc.DepartmentId : (Guid?)null,
                                    DepartmentName = firstAlloc != null ? firstAlloc.Department : lineItem.Department
                                };
                                PopulateDetailFromExtension(detail, extn);
                                detail.IsCompleted = IsDetailCompleted(detail, subExtnName);
                                subVm.InventoryDetails.Add(detail);
                            }
                        }

                        // Reconcile sub-item units if inventory required
                        if (subRequiresInventory && subVm.InspectNowQty > 0)
                        {
                            if (lineItem.Allocations != null && lineItem.Allocations.Any())
                            {
                                foreach (var alloc in lineItem.Allocations)
                                {
                                    int allocSubTarget = (int)Math.Floor(alloc.QtyInspected * qtyPerParent);
                                    int existingSubCount = subVm.InventoryDetails.Count(d => d.OrderItemRequestId == alloc.OrderItemRequestId);
                                    for (int q = existingSubCount + 1; q <= allocSubTarget; q++)
                                    {
                                        int globalNo = subVm.InventoryDetails.Count + 1;
                                        subVm.InventoryDetails.Add(new AIRItemInventoryDetailViewModel
                                        {
                                            OrderItemRequestId = alloc.OrderItemRequestId,
                                            RequestId = alloc.RequestId,
                                            RequestItemId = alloc.RequestItemId,
                                            PRNumber = alloc.PRNumber,
                                            DepartmentId = alloc.DepartmentId,
                                            DepartmentName = alloc.Department,
                                            AirSubItemId = subVm.AirSubItemId,
                                            ContentNo = globalNo,
                                            TContentNo = globalNo,
                                            IsCompleted = false,
                                            Condition = "Good"
                                        });
                                    }
                                }
                            }
                            else
                            {
                                int targetCount = (int)Math.Floor(subVm.InspectNowQty);
                                for (int q = subVm.InventoryDetails.Count + 1; q <= targetCount; q++)
                                {
                                    subVm.InventoryDetails.Add(new AIRItemInventoryDetailViewModel
                                    {
                                        PRNumber = lineItem.PRNumber,
                                        DepartmentName = lineItem.Department,
                                        AirSubItemId = subVm.AirSubItemId,
                                        ContentNo = q,
                                        TContentNo = q,
                                        IsCompleted = false,
                                        Condition = "Good"
                                    });
                                }
                            }
                        }
                        lineItem.SubItems.Add(subVm);
                    }

                    // Also load any inspector-added components for this item
                    var orderedSubIds = oi.OrderSubItems.Select(s => s.Id).ToList();
                    var inspectorAddedSubs = existingSubItems.Where(s => !s.OrderSubItemId.HasValue || !orderedSubIds.Contains(s.OrderSubItemId.Value)).ToList();
                    foreach (var manualSub in inspectorAddedSubs)
                    {
                        string manualCategory = manualSub.CategoryCode;
                        string manualExtnName = manualSub.ItemExtnName ?? _airItemSharedService.GetItemExtnNameByCategory(manualCategory);
                        bool manualRequiresInv = !string.IsNullOrWhiteSpace(manualExtnName) && (lineItem.Disposition == "Inventory");

                        var manualVm = new AIRSubItemViewModel
                        {
                            AirSubItemId = manualSub.Id,
                            OrderSubItemId = Guid.Empty,
                            OrderSubItemRequestId = null,
                            SubItemNo = !string.IsNullOrWhiteSpace(manualSub.SubItemNo) ? manualSub.SubItemNo.Trim().TrimStart('#').Trim() : null,
                            Description = manualSub.Description,
                            Unit = manualSub.Unit,
                            ExpectedQty = manualSub.ExpectedQty > 0 ? manualSub.ExpectedQty : manualSub.InspectedQty,
                            PreviousInspectedQty = 0,
                            InspectNowQty = manualSub.InspectedQty,
                            Remarks = manualSub.Remarks,
                            QtyPerParent = manualSub.QtyPerParent ?? 1,
                            CategoryCode = manualCategory,
                            ItemExtnName = manualExtnName,
                            RequiresInventory = manualRequiresInv,
                            SourceType = SubItemSourceTypes.Normalize(manualSub.SourceType, false),
                            IsRequiredForBundle = SubItemSourceTypes.ResolveBundleRequired(manualSub.SourceType, manualSub.IsRequiredForBundle, false)
                        };

                        var extnsForManual = subExtns.Where(e => e.AIRSubItemId == manualSub.Id).OrderBy(e => e.ContentNo).ToList();
                        foreach (var extn in extnsForManual)
                        {
                            var detail = new AIRItemInventoryDetailViewModel
                            {
                                Id = extn.Id,
                                AirItemId = null,
                                AirSubItemId = extn.AIRSubItemId,
                                ContentNo = extn.ContentNo ?? (manualVm.InventoryDetails.Count + 1),
                                TContentNo = extn.TContentNo,
                                SetLotNo = extn.SetLotNo,
                                SetLotQtyNo = extn.SetLotQtyNo
                            };
                            PopulateDetailFromExtension(detail, extn);
                            detail.IsCompleted = IsDetailCompleted(detail, manualExtnName);
                            manualVm.InventoryDetails.Add(detail);
                        }

                        // Reconcile manual sub-item units strictly according to InspectNowQty
                        if (manualRequiresInv && manualVm.InspectNowQty > 0)
                        {
                            int targetCount = (int)Math.Floor(manualVm.InspectNowQty);
                            var firstAlloc = lineItem.Allocations != null ? lineItem.Allocations.FirstOrDefault() : null;
                            for (int q = manualVm.InventoryDetails.Count + 1; q <= targetCount; q++)
                            {
                                manualVm.InventoryDetails.Add(new AIRItemInventoryDetailViewModel
                                {
                                    OrderItemRequestId = firstAlloc != null ? firstAlloc.OrderItemRequestId : (Guid?)null,
                                    RequestId = firstAlloc != null ? firstAlloc.RequestId : (Guid?)null,
                                    RequestItemId = firstAlloc != null ? firstAlloc.RequestItemId : (Guid?)null,
                                    PRNumber = firstAlloc != null ? firstAlloc.PRNumber : lineItem.PRNumber,
                                    DepartmentId = firstAlloc != null ? firstAlloc.DepartmentId : (Guid?)null,
                                    DepartmentName = firstAlloc != null ? firstAlloc.Department : lineItem.Department,
                                    AirSubItemId = manualVm.AirSubItemId,
                                    ContentNo = q,
                                    TContentNo = q,
                                    IsCompleted = false,
                                    Condition = "Good"
                                });
                            }
                        }

                        lineItem.SubItems.Add(manualVm);
                    }

                    if (lineItem.SubItems.Any())
                    {
                        lineItem.IsSetLot = true;
                    }
                }

                vm.Items.Add(lineItem);
            }

            return vm;
        }

        public async Task<AIRWizardViewModel> GetAirInspectionForEditAsync(Guid airId)
        {
            var air = await _db.AIRs
                .Include(a => a.Order)
                .Include(a => a.AIRItems)
                .Include(a => a.AIRInvoices)
                .FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR not found.");

            var vm = await GetPOInspectionDetailsAsync(air.OrderId.Value, airId);
            vm.AirId = air.Id;
            vm.AirNo = air.AIRNo ?? air.CtrlNo;
            vm.CtrlNo = air.CtrlNo;
            vm.AirDate = air.AIRDate;
            vm.InspectionDate = air.InspectedDate ?? DateTime.Today;
            vm.InspectorName = air.Officer ?? air.InspectorName;
            vm.InspectorDesignation = air.InspectorDesignation;
            vm.InspectionCommittee = air.InspectionCommittee;
            vm.InspectionLocation = air.InspectionLocation;
            vm.GeneralRemarks = air.Remarks;
            vm.Disposition = air.Disposition;
            vm.OverallStatus = ResolveOverallStatus(air);
            vm.InspectionStatus = air.PostedDt != null ? AirInspectionStatuses.Posted : (air.IsInspected == true ? AirInspectionStatuses.Submitted : AirInspectionStatuses.Draft);

            if (air.AIRInvoices != null && air.AIRInvoices.Any())
            {
                vm.Invoices = air.AIRInvoices.Select(inv => new AIRInvoiceViewModel
                {
                    Id = inv.Id,
                    SalesInvoiceNo = inv.InvoiceNo,
                    InvoiceDate = inv.InvoiceDate,
                    InvoiceTotalAmount = inv.Amount ?? 0
                }).ToList();
            }

            return vm;
        }

        public async Task<AIRWizardViewModel> GetDraftForEditAsync(Guid draftId)
        {
            var progress = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.Id == draftId);
            if (progress != null)
            {
                if (progress.Status != AirWizardStatuses.Draft || progress.IsCompleted)
                {
                    throw new InvalidOperationException("This inspection draft is no longer active (Status: " + progress.Status + ").");
                }
            }
            else
            {
                var legacyAir = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == draftId);
                if (legacyAir != null)
                {
                    if (legacyAir.PostedDt != null || (legacyAir.IsInspected == true && string.Equals(legacyAir.OverallStatus, AirStatuses.SubmittedForAcceptance, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("Cannot edit a submitted inspection report. Only active drafts can be edited.");
                    }
                    return await GetAirInspectionForEditAsync(legacyAir.Id);
                }
                throw new InvalidOperationException("Inspection draft not found.");
            }

            AIRWizardViewModel vm = null;
            if (!string.IsNullOrWhiteSpace(progress.WizardStateJson))
            {
                try
                {
                    vm = JsonConvert.DeserializeObject<AIRWizardViewModel>(progress.WizardStateJson);
                }
                catch
                {
                    vm = null;
                }
            }

            if (vm == null)
            {
                vm = new AIRWizardViewModel();
            }

            vm.DraftId = progress.Id;
            vm.DraftNo = progress.DraftNo;
            vm.CurrentStep = progress.CurrentStep > 0 ? progress.CurrentStep : (progress.LastStep > 0 ? progress.LastStep : 1);
            vm.OrderId = progress.OrderId ?? vm.OrderId;
            vm.SourceAIRId = progress.SourceAIRId;
            vm.RevisionNo = progress.RevisionNo;
            vm.WizardProgressStatus = progress.Status;

            if (vm.OrderId.HasValue)
            {
                var targetAirIdForPO = progress.SourceAIRId ?? vm.AirId;
                var freshDetails = await GetPOInspectionDetailsAsync(vm.OrderId.Value, targetAirIdForPO);
                if (vm.Items != null && vm.Items.Any() && freshDetails.Items != null && freshDetails.Items.Any())
                {
                    var userItems = vm.Items.ToDictionary(i => i.OrderItemId);
                    foreach (var freshItem in freshDetails.Items)
                    {
                        AIRLineItemViewModel existing;
                        if (userItems.TryGetValue(freshItem.OrderItemId, out existing))
                        {
                            freshItem.InspectNowQty = existing.InspectNowQty;
                            freshItem.Remarks = existing.Remarks;
                            if (!string.IsNullOrEmpty(existing.Disposition))
                            {
                                freshItem.Disposition = existing.Disposition;
                            }
                            if (existing.InventoryDetails != null && existing.InventoryDetails.Any())
                            {
                                freshItem.InventoryDetails = existing.InventoryDetails;
                            }
                            if (existing.Allocations != null && existing.Allocations.Any() && freshItem.Allocations != null)
                            {
                                var userAllocMap = existing.Allocations.ToDictionary(a => a.OrderItemRequestId);
                                foreach (var freshAlloc in freshItem.Allocations)
                                {
                                    AIRItemAllocationViewModel existingAlloc;
                                    if (userAllocMap.TryGetValue(freshAlloc.OrderItemRequestId, out existingAlloc))
                                    {
                                        freshAlloc.QtyInspected = existingAlloc.QtyInspected;
                                        freshAlloc.Remarks = existingAlloc.Remarks;
                                    }
                                }
                            }
                            if (existing.SubItems != null && existing.SubItems.Any() && freshItem.SubItems != null)
                            {
                                var userSubMap = existing.SubItems
                                    .Where(s => s.OrderSubItemId != Guid.Empty)
                                    .GroupBy(s => s.OrderSubItemId)
                                    .ToDictionary(g => g.Key, g => g.First());

                                foreach (var freshSub in freshItem.SubItems)
                                {
                                    AIRSubItemViewModel existingSub;
                                    if (userSubMap.TryGetValue(freshSub.OrderSubItemId, out existingSub))
                                    {
                                        freshSub.InspectNowQty = existingSub.InspectNowQty;
                                        freshSub.Remarks = existingSub.Remarks;
                                        freshSub.SourceType = SubItemSourceTypes.Normalize(existingSub.SourceType, true);
                                        freshSub.IsRequiredForBundle = true; // ORDERED sub-items are ALWAYS required for bundle
                                        if (existingSub.InventoryDetails != null && existingSub.InventoryDetails.Any())
                                        {
                                            freshSub.InventoryDetails = existingSub.InventoryDetails;
                                        }
                                    }
                                }

                                // Restore inspector-added components that were not ordered
                                var orderedIds = freshItem.SubItems
                                    .Where(s => s.OrderSubItemId != Guid.Empty)
                                    .Select(s => s.OrderSubItemId)
                                    .ToList();

                                var inspectorAdded = existing.SubItems
                                    .Where(s => s.OrderSubItemId == Guid.Empty || !orderedIds.Contains(s.OrderSubItemId))
                                    .ToList();

                                foreach (var manualSub in inspectorAdded)
                                {
                                    if (!string.IsNullOrWhiteSpace(manualSub.SubItemNo))
                                    {
                                        manualSub.SubItemNo = manualSub.SubItemNo.Trim().TrimStart('#').Trim();
                                    }
                                    var match = freshItem.SubItems.FirstOrDefault(s =>
                                        (s.AirSubItemId.HasValue && manualSub.AirSubItemId.HasValue && s.AirSubItemId == manualSub.AirSubItemId) ||
                                        (s.OrderSubItemId == Guid.Empty && s.Description == manualSub.Description && s.SourceType == manualSub.SourceType));
                                    if (match == null)
                                    {
                                        manualSub.IsRequiredForBundle = SubItemSourceTypes.ResolveBundleRequired(manualSub.SourceType, manualSub.IsRequiredForBundle, false);
                                        freshItem.SubItems.Add(manualSub);
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(manualSub.SubItemNo))
                                        {
                                            match.SubItemNo = manualSub.SubItemNo.Trim().TrimStart('#').Trim();
                                        }
                                        match.InspectNowQty = manualSub.InspectNowQty;
                                        match.Remarks = manualSub.Remarks;
                                        match.SourceType = SubItemSourceTypes.Normalize(manualSub.SourceType, false);
                                        match.IsRequiredForBundle = SubItemSourceTypes.ResolveBundleRequired(manualSub.SourceType, manualSub.IsRequiredForBundle, false);
                                        if (manualSub.InventoryDetails != null && manualSub.InventoryDetails.Any())
                                        {
                                            match.InventoryDetails = manualSub.InventoryDetails;
                                        }
                                    }
                                }

                                if (freshItem.SubItems.Any())
                                {
                                    freshItem.IsSetLot = true;
                                }
                            }
                        }
                    }
                    vm.Items = freshDetails.Items;
                }
                else if (freshDetails.Items != null)
                {
                    vm.Items = freshDetails.Items;
                }

                vm.PONumber = freshDetails.PONumber;
                vm.PODate = freshDetails.PODate;
                vm.PRNumber = freshDetails.PRNumber;
                vm.DepartmentName = freshDetails.DepartmentName;
                vm.SupplierId = freshDetails.SupplierId;
                vm.SupplierName = freshDetails.SupplierName;
                vm.DeliveryPlace = freshDetails.DeliveryPlace;
                vm.Purpose = freshDetails.Purpose;
                vm.POTotalAmount = freshDetails.POTotalAmount;
                if (string.IsNullOrEmpty(vm.Disposition))
                {
                    vm.Disposition = freshDetails.Disposition;
                }
            }

            if (progress.SourceAIRId.HasValue && progress.SourceAIRId.Value != Guid.Empty)
            {
                vm.SourceAIRId = progress.SourceAIRId;
                vm.RevisionNo = progress.RevisionNo;
                vm.AirId = null;
                vm.DraftId = progress.Id;
                vm.DraftNo = progress.DraftNo;
            }

            return vm;
        }

        public async Task<Guid> SaveInspectionDraftAsync(AIRWizardViewModel model, string user)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            AIRWizardProgress progress = null;
            if (model.DraftId.HasValue)
            {
                progress = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.Id == model.DraftId.Value);
                if (progress == null)
                    throw new InvalidOperationException("Inspection draft not found.");
                if (progress.Status != AirWizardStatuses.Draft || progress.IsCompleted)
                    throw new InvalidOperationException("Only an active draft can be saved.");
                if (progress.UserId != user && progress.CreatedBy != user)
                    throw new InvalidOperationException("You cannot modify another user's inspection draft.");
            }

            // Fallback: If DraftId was not supplied or not yet saved in client, reuse an active draft for this order and user
            if (progress == null && model.OrderId.HasValue)
            {
                progress = await _db.AIRWizardProgresses
                    .FirstOrDefaultAsync(p => p.Status == AirWizardStatuses.Draft &&
                                              p.OrderId == model.OrderId.Value &&
                                              p.UserId == user &&
                                              p.SourceAIRId == model.SourceAIRId &&
                                              p.RevisionNo == model.RevisionNo &&
                                              !p.IsCompleted);
            }


            if (progress == null)
            {
                progress = new AIRWizardProgress
                {
                    Id = model.DraftId ?? Guid.NewGuid(),
                    DraftNo = await GenerateDraftNumberAsync(DateTime.Now),
                    CreatedBy = user,
                    UserId = user,
                    CreatedAt = DateTime.Now,
                    Status = AirWizardStatuses.Draft,
                    RevisionNo = model.RevisionNo,
                    SourceAIRId = model.SourceAIRId,
                    IsCompleted = false
                };
                _db.AIRWizardProgresses.Add(progress);
                model.DraftId = progress.Id;
                model.DraftNo = progress.DraftNo;
            }
            else
            {
                model.DraftId = progress.Id;
                model.DraftNo = progress.DraftNo;
            }

            progress.OrderId = model.OrderId;
            progress.CurrentStep = model.CurrentStep > 0 ? model.CurrentStep : 1;
            progress.LastStep = progress.CurrentStep;
            progress.UpdatedAt = DateTime.Now;

            if (model.Items != null)
            {
                foreach (var lineItm in model.Items)
                {
                    if (lineItm.SubItems != null)
                    {
                        foreach (var sub in lineItm.SubItems)
                        {
                            if (!string.IsNullOrWhiteSpace(sub.SubItemNo))
                            {
                                sub.SubItemNo = sub.SubItemNo.Trim().TrimStart('#').Trim();
                            }
                        }
                    }
                }
            }

            progress.WizardStateJson = JsonConvert.SerializeObject(model);

            await _db.SaveChangesAsync();

            return progress.Id;
        }

        public async Task<Guid> SubmitForAcceptanceAsync(AIRWizardViewModel model, string user)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            AIRWizardProgress submittingProgress = null;
            if (model.DraftId.HasValue)
            {
                submittingProgress = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.Id == model.DraftId.Value);
                if (submittingProgress == null)
                    throw new InvalidOperationException("Inspection draft not found.");
                if (submittingProgress.Status == AirWizardStatuses.Submitted || submittingProgress.IsCompleted)
                    throw new InvalidOperationException("This inspection draft has already been submitted.");
                if (submittingProgress.Status != AirWizardStatuses.Draft)
                    throw new InvalidOperationException(string.Format("Draft cannot be submitted because its status is '{0}'.", submittingProgress.Status));
                if (submittingProgress.UserId != user && submittingProgress.CreatedBy != user)
                    throw new InvalidOperationException("You cannot submit another user's inspection draft.");

                if (submittingProgress.SourceAIRId.HasValue && submittingProgress.SourceAIRId.Value != Guid.Empty)
                {
                    model.SourceAIRId = submittingProgress.SourceAIRId.Value;
                    model.RevisionNo = submittingProgress.RevisionNo;
                }
            }

            bool isRevision = (model.SourceAIRId.HasValue && model.SourceAIRId.Value != Guid.Empty) ||
                              (model.AirId.HasValue && model.AirId.Value != Guid.Empty);
            Guid? excludeAirId = isRevision ? (model.SourceAIRId ?? model.AirId) : model.AirId;

            if (model.SourceAIRId.HasValue && model.SourceAIRId.Value != Guid.Empty)
            {
                var sourceAirCheck = await _db.AIRs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == model.SourceAIRId.Value);
                if (sourceAirCheck == null)
                    throw new InvalidOperationException(string.Format("Source inspection report '{0}' not found for revision resubmission.", model.SourceAIRId.Value));
                if (sourceAirCheck.PostedDt != null)
                    throw new InvalidOperationException("Cannot resubmit revision: the source inspection report has already been posted and finalized.");
                if (sourceAirCheck.OverallStatus != AirStatuses.Withdrawn && sourceAirCheck.OverallStatus != AirStatuses.Draft)
                    throw new InvalidOperationException(string.Format("Cannot resubmit: source inspection report status is currently '{0}'. Only draft or withdrawn inspection reports can be submitted.", sourceAirCheck.OverallStatus ?? "Unknown"));

                model.AirId = sourceAirCheck.Id;
            }
            else if (model.AirId.HasValue)
            {
                var existingAir = await _db.AIRs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == model.AirId.Value);
                if (existingAir != null)
                {
                    if (existingAir.PostedDt.HasValue)
                        throw new InvalidOperationException("Cannot submit: the inspection report has already been posted and finalized.");
                    if (existingAir.IsInspected == true && string.Equals(existingAir.OverallStatus, AirStatuses.SubmittedForAcceptance, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("This inspection report has already been submitted for acceptance.");
                }
            }

            if (model.Items == null || !model.Items.Any(i => i.InspectNowQty > 0))
            {
                throw new InvalidOperationException("At least one item must have an Inspect Now quantity greater than 0.");
            }

            if (model.Invoices == null || !model.Invoices.Any())
            {
                throw new InvalidOperationException("At least one Sales Invoice record is required.");
            }

            foreach (var inv in model.Invoices)
            {
                if (string.IsNullOrWhiteSpace(inv.SalesInvoiceNo))
                    throw new InvalidOperationException("Sales Invoice Number is required for all invoice records.");
                if (inv.InvoiceDate == null)
                    throw new InvalidOperationException(string.Format("Invoice Date is required for Sales Invoice '{0}'.", inv.SalesInvoiceNo));
                if (inv.InvoiceTotalAmount <= 0)
                    throw new InvalidOperationException(string.Format("Invoice Total Amount must be greater than zero for Sales Invoice '{0}'.", inv.SalesInvoiceNo));
            }

            var duplicateInv = model.Invoices
                .GroupBy(i => (i.SalesInvoiceNo ?? "").Trim().ToUpperInvariant())
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateInv != null)
            {
                throw new InvalidOperationException(string.Format("Duplicate Sales Invoice Number '{0}' found. Each invoice number must be unique.", duplicateInv.Key));
            }

            // Server-side concurrency guard: recalculate remaining from DB per OrderItemRequest allocation
            foreach (var item in model.Items.Where(i => i.InspectNowQty > 0))
            {
                if (item.Allocations != null && item.Allocations.Any())
                {
                    foreach (var alloc in item.Allocations)
                    {
                        if (alloc.QtyInspected < 0)
                        {
                            throw new InvalidOperationException(string.Format("Invalid inspection quantity for PR {0}: quantity cannot be negative.", alloc.PRNumber));
                        }

                        if (alloc.QtyInspected > 0)
                        {
                            var postedFromAllocations = await _db.AIRItemAllocations
                                .Where(a => a.OrderItemRequestId == alloc.OrderItemRequestId &&
                                            a.AIRItem.AIR.PostedDt != null &&
                                            (excludeAirId == null || a.AIRItem.AirId != excludeAirId.Value))
                                .SumAsync(a => (decimal?)a.QtyInspected) ?? 0;

                            var postedFromHistorical = await _db.AIRItems
                                .Where(ai => ai.AIR.PostedDt != null &&
                                             (excludeAirId == null || ai.AirId != excludeAirId.Value) &&
                                             !ai.AIRItemAllocations.Any() &&
                                             ai.OrderItemRequestId == alloc.OrderItemRequestId)
                                .SumAsync(ai => (decimal?)ai.Qty) ?? 0;

                            var dbPostedForOIR = postedFromAllocations + postedFromHistorical;

                            var oirEntity = await _db.OrderItemRequests.FindAsync(alloc.OrderItemRequestId);
                            decimal qtyAllocated = oirEntity != null && oirEntity.QtyApplied.HasValue ? oirEntity.QtyApplied.Value : alloc.QtyAllocated;

                            decimal remainingForOIR = qtyAllocated - dbPostedForOIR;
                            if (remainingForOIR < 0) remainingForOIR = 0;

                            if (alloc.QtyInspected > remainingForOIR)
                            {
                                throw new InvalidOperationException(string.Format(
                                    "Concurrency conflict on item {0}, PR {1} (Department: {2}): inspected quantity ({3:N2}) exceeds remaining deliverable quantity ({4:N2}). Another inspection transaction may have been posted.",
                                    item.ItemNo, alloc.PRNumber, alloc.Department, alloc.QtyInspected, remainingForOIR));
                            }
                        }
                    }
                }
                else
                {
                    var oirList = await _db.OrderItemRequests.Where(r => r.OrderItemId == item.OrderItemId).ToListAsync();
                    var oirIds = oirList.Select(r => r.Id).ToList();

                    var dbPosted = await _db.AIRItems
                        .Where(ai => ai.AIR.PostedDt != null &&
                                     (excludeAirId == null || ai.AirId != excludeAirId.Value) &&
                                     ((ai.OrderItemRequestId.HasValue && oirIds.Contains(ai.OrderItemRequestId.Value)) ||
                                      (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == item.OrderItemId)))
                        .SumAsync(ai => (decimal?)ai.Qty) ?? 0;

                    decimal totalQty = oirList.Sum(r => (decimal?)r.QtyApplied ?? 0);
                    if (totalQty == 0)
                    {
                        var orderItem = await _db.OrderItems.FindAsync(item.OrderItemId);
                        if (orderItem == null) throw new InvalidOperationException(string.Format("Order item not found: {0}", item.ItemNo));
                        totalQty = orderItem.Qty ?? 0;
                    }

                    var remainingAllowed = totalQty - dbPosted;
                    if (item.InspectNowQty > remainingAllowed)
                    {
                        throw new InvalidOperationException(string.Format(
                            "Concurrency conflict on item {0}: inspected qty ({1}) exceeds remaining deliverable qty ({2}). Another inspection may have posted.",
                            item.ItemNo, item.InspectNowQty, remainingAllowed));
                    }
                }
            }

            // Validate completeness of parent and sub-item inventory details
            var incompleteErrors = new List<string>();
            foreach (var item in model.Items.Where(i => i.InspectNowQty > 0))
            {
                if (item.Disposition == "Inventory")
                {
                    if (item.ParentRequiresInventory)
                    {
                        int req = (int)Math.Floor(item.InspectNowQty);
                        int comp = item.InventoryDetails != null 
                            ? item.InventoryDetails.Count(d => IsDetailCompleted(d, item.ItemExtnName)) 
                            : 0;
                        if (comp < req)
                        {
                            incompleteErrors.Add(string.Format("Item #{0} ({1}): {2} of {3} parent inventory details completed.", item.ItemNo, item.Description, comp, req));
                        }
                    }

                    if (item.SubItems != null && item.SubItems.Any())
                    {
                        foreach (var sub in item.SubItems.Where(s => s.RequiresInventory && s.InspectNowQty > 0))
                        {
                            int subReq = (int)Math.Floor(sub.InspectNowQty);
                            int subComp = sub.InventoryDetails != null 
                                ? sub.InventoryDetails.Count(d => IsDetailCompleted(d, sub.ItemExtnName)) 
                                : 0;
                            if (subComp < subReq)
                            {
                                incompleteErrors.Add(string.Format("Item #{0} ({1}) - Sub-item '{2}': {3} of {4} inventory details completed.", item.ItemNo, item.Description, sub.Description, subComp, subReq));
                            }
                        }
                    }
                }
            }

            if (incompleteErrors.Any())
            {
                throw new InvalidOperationException("Inventory details are incomplete:\n" + string.Join("\n", incompleteErrors));
            }

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    AIR air = null;
                    var targetAirId = model.AirId ?? model.SourceAIRId;

                    if (targetAirId.HasValue)
                    {
                        air = await _db.AIRs
                            .Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns))
                            .Include(a => a.AIRItems.Select(ai => ai.AIRItemAllocations))
                            .Include(a => a.AIRInvoices)
                            .Include(a => a.AIRDocuments)
                            .FirstOrDefaultAsync(a => a.Id == targetAirId.Value);

                        if (air != null)
                        {
                            if (air.PostedDt != null)
                            {
                                throw new InvalidOperationException("Cannot submit: the inspection report has already been posted and finalized.");
                            }
                            if (air.OverallStatus == AirStatuses.SubmittedForAcceptance && air.IsInspected == true)
                            {
                                throw new InvalidOperationException("This inspection report has already been submitted for acceptance.");
                            }
                            model.AirId = air.Id;
                        }
                    }

                    if (air == null)
                    {
                        air = new AIR
                        {
                            Id = model.AirId ?? Guid.NewGuid(),
                            OrderId = model.OrderId,
                            CtrlNo = await GenerateAirNumberAsync(DateTime.Now),
                            AIRNo = await GenerateAirNumberAsync(DateTime.Now),
                            InsertedBy = user,
                            InsertedDt = DateTime.Now
                        };
                        _db.AIRs.Add(air);
                        model.AirId = air.Id;
                    }

                    air.AIRDate = model.AirDate ?? DateTime.Today;
                    air.InspectedDate = model.InspectionDate;
                    air.IsInspected = true;
                    air.OverallStatus = AirStatuses.SubmittedForAcceptance;
                    air.InspectionStatus = AirInspectionStatuses.Submitted;
                    air.Officer = model.InspectorName;
                    air.Remarks = model.GeneralRemarks;

                    if (string.IsNullOrWhiteSpace(air.AIRNo))
                    {
                        air.AIRNo = await GenerateAirNumberAsync(DateTime.Now);
                    }

                    air.InspectorName = model.InspectorName;
                    air.InspectorDesignation = model.InspectorDesignation;
                    air.InspectionCommittee = model.InspectionCommittee;
                    air.InspectionLocation = model.InspectionLocation;

                    air.Disposition = model.Disposition;
                    air.InvDist = model.Disposition == "For Distribution" ? "D" : (model.Disposition == "Inventory" ? "I" : model.Disposition);

                    if (isRevision)
                    {
                        // Reset current-state withdrawal/revision fields on active AIR row.
                        // Historical withdrawal details remain preserved in DocumentStatusHistories and immutable Snapshot V1.
                        air.WithdrawnBy = null;
                        air.WithdrawnDt = null;
                        air.WithdrawalReason = null;
                        air.WithdrawalRequested = false;
                        air.WithdrawalRequestedBy = null;
                        air.WithdrawalRequestedDt = null;
                        air.RevisionComments = null;
                        air.ReturnedBy = null;
                        air.ReturnedDt = null;
                    }

                    air.UpdatedBy = user;
                    air.UpdatedDt = DateTime.Now;

                    SyncAirItems(air, model, user);

                    await _db.SaveChangesAsync();

                    if (model.DraftId.HasValue)
                    {
                        var progress = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.Id == model.DraftId.Value);
                        if (progress != null)
                        {
                            progress.AIRId = air.Id;
                            progress.Status = AirWizardStatuses.Submitted;
                            progress.SubmittedDt = DateTime.Now;
                            progress.IsCompleted = true;
                            progress.CurrentStep = 4;
                            progress.LastStep = 4;
                            progress.UpdatedAt = DateTime.Now;
                            await _db.SaveChangesAsync();
                        }
                    }

                    // Generate and persist immutable submission snapshot for this exact AIR
                    var maxSnapshotVersion = await _db.AIRSubmissionSnapshots
                        .Where(s => s.AIRId == air.Id)
                        .MaxAsync(s => (int?)s.VersionNo) ?? 0;

                    int versionNo = Math.Max(maxSnapshotVersion + 1, (model.RevisionNo > 0 ? model.RevisionNo + 1 : 1));

                    model.AirId = air.Id;
                    model.AirNo = air.AIRNo ?? air.CtrlNo;
                    model.CtrlNo = air.CtrlNo;
                    model.AirDate = air.AIRDate;
                    model.InspectionDate = air.InspectedDate ?? model.InspectionDate;
                    model.InspectorName = air.InspectorName ?? model.InspectorName;
                    model.InspectorDesignation = air.InspectorDesignation ?? model.InspectorDesignation;
                    model.InspectionCommittee = air.InspectionCommittee ?? model.InspectionCommittee;
                    model.InspectionLocation = air.InspectionLocation ?? model.InspectionLocation;
                    model.GeneralRemarks = air.Remarks ?? model.GeneralRemarks;
                    model.Disposition = air.Disposition ?? model.Disposition;

                    if (string.IsNullOrEmpty(model.PONumber) && air.OrderId.HasValue)
                    {
                        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == air.OrderId.Value);
                        if (order != null)
                        {
                            model.PONumber = order.PoNo;
                            model.PODate = order.PoDate;
                            model.SupplierName = order.SupName;
                            model.DepartmentName = order.Department;
                            model.DeliveryPlace = order.DeliveryPlace;
                        }
                    }



                    var payloadJson = JsonConvert.SerializeObject(model, Formatting.Indented);

                    var snapshot = new AIRSubmissionSnapshot
                    {
                        Id = Guid.NewGuid(),
                        AIRId = air.Id,
                        WizardProgressId = model.DraftId,
                        VersionNo = versionNo,
                        PayloadJson = payloadJson,
                        SubmittedBy = user,
                        SubmittedDt = DateTime.Now,
                        CreatedDt = DateTime.Now
                    };
                    _db.AIRSubmissionSnapshots.Add(snapshot);
                    await _db.SaveChangesAsync();

                    string actionName = isRevision ? "Resubmit for Acceptance" : "Submit for Acceptance";
                    string statusBefore = AirStatuses.Draft;
                    string historyRemarks = isRevision
                        ? string.Format("Inspection resubmitted for acceptance review after revision #{0}. Submission Snapshot Created (Version {1}).", model.RevisionNo, versionNo)
                        : string.Format("Inspection submitted for acceptance review. Submission Snapshot Created (Version {0}).", versionNo);

                    _historyService.AddStatusHistory(
                        DocumentTypes.AcceptanceInspectionReport,
                        air.Id,
                        air.AIRNo ?? air.CtrlNo,
                        statusBefore,
                        AirStatuses.SubmittedForAcceptance,
                        actionName,
                        historyRemarks,
                        user
                    );
                    await _db.SaveChangesAsync();

                    tx.Commit();
                    return air.Id;
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    tx.Rollback();
                    var errors = new System.Collections.Generic.List<string>();
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            errors.Add(string.Format("{0}.{1}: {2}", eve.Entry.Entity.GetType().Name, ve.PropertyName, ve.ErrorMessage));
                        }
                    }
                    throw new InvalidOperationException("Validation failed: " + string.Join("; ", errors), ex);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

                public void SanitizeDraftForRevision(AIRWizardViewModel draftModel)
        {
            if (draftModel == null) return;
            draftModel.AirId = null;
            draftModel.AirNo = null;

            if (draftModel.Invoices != null)
            {
                foreach (var inv in draftModel.Invoices)
                {
                    inv.Id = Guid.Empty;
                    inv.AirId = null;
                    if (inv.SupportingDocument != null)
                    {
                        inv.SupportingDocument.Id = Guid.Empty;
                        inv.SupportingDocument.AirId = null;
                    }
                }
            }

            if (draftModel.SupportingDocuments != null)
            {
                foreach (var doc in draftModel.SupportingDocuments)
                {
                    doc.Id = Guid.Empty;
                    doc.AirId = null;
                }
            }

            if (draftModel.Items != null)
            {
                foreach (var item in draftModel.Items)
                {
                    item.AirItemId = null;
                    if (item.Allocations != null)
                    {
                        foreach (var alloc in item.Allocations)
                        {
                            alloc.Id = null;
                        }
                    }
                    if (item.InventoryDetails != null)
                    {
                        foreach (var det in item.InventoryDetails)
                        {
                            det.Id = Guid.Empty;
                        }
                    }
                    if (item.SubItems != null)
                    {
                        foreach (var sub in item.SubItems)
                        {
                            sub.AirSubItemId = null;
                            if (sub.InventoryDetails != null)
                            {
                                foreach (var sDet in sub.InventoryDetails)
                                {
                                    sDet.Id = Guid.Empty;
                                }
                            }
                        }
                    }
                }
            }
        }
        public bool CanWithdrawAir(AIR air)
        {
            if (air == null) return false;
            if (air.IsInspected != true) return false;
            if (!string.Equals(air.OverallStatus, AirStatuses.SubmittedForAcceptance, StringComparison.OrdinalIgnoreCase)) return false;
            if (air.PostedDt != null) return false;
            if (air.AcceptanceStartedDt != null) return false;
            if (air.AcceptedDate != null) return false;
            if (!string.IsNullOrEmpty(air.AcceptedBy)) return false;
            if (air.OverallStatus == AirStatuses.Posted || air.OverallStatus == AirStatuses.Accepted) return false;
            if (air.AIRItems != null && air.AIRItems.Any(ai => ai.AcceptedQty.HasValue && ai.AcceptedQty.Value > 0)) return false;
            return true;
        }

        public bool CanDiscardAir(AIR air, out string reason)
        {
            reason = null;
            if (air == null)
            {
                reason = "Inspection record not found.";
                return false;
            }

            if (air.PostedDt != null)
            {
                reason = "This AIR cannot be discarded because downstream acceptance or inventory transactions already exist.";
                return false;
            }

            if (string.Equals(air.OverallStatus, AirStatuses.SubmittedForAcceptance, StringComparison.OrdinalIgnoreCase) && air.IsInspected == true)
            {
                reason = "Cannot discard an AIR that is currently submitted for acceptance. Please withdraw it first.";
                return false;
            }

            if (air.AcceptedDate != null || air.AcceptanceStartedDt != null ||
                string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Accepted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.InProgress, StringComparison.OrdinalIgnoreCase) ||
                (air.AIRItems != null && air.AIRItems.Any(ai => ai.AcceptedQty.HasValue && ai.AcceptedQty.Value > 0)))
            {
                reason = "This AIR cannot be discarded because downstream acceptance or inventory transactions already exist.";
                return false;
            }

            var overallStatus = ResolveOverallStatus(air);
            if (!string.Equals(overallStatus, AirStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(overallStatus, AirStatuses.Withdrawn, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(overallStatus, AirStatuses.Unposted, StringComparison.OrdinalIgnoreCase))
            {
                reason = "Only draft or unposted inspection records can be discarded.";
                return false;
            }

            return true;
        }

        public async Task<Guid> WithdrawSubmissionAsync(Guid airId, string reason, string user)
        {
            using (var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var air = await _db.AIRs
                        .Include(a => a.AIRItems)
                        .FirstOrDefaultAsync(a => a.Id == airId);
                    if (air == null) throw new InvalidOperationException("AIR record not found.");

                    if (!CanWithdrawAir(air))
                    {
                        if (air.PostedDt != null)
                            throw new InvalidOperationException("Cannot withdraw a posted AIR. The acceptance has already been finalized.");
                        throw new InvalidOperationException("Only an AIR submitted for acceptance, before acceptance starts, can be withdrawn.");
                    }

                    // Withdraw operates on the SAME AIR record, transitioning it back to Draft
                    air.OverallStatus = AirStatuses.Draft;
                    air.InspectionStatus = AirInspectionStatuses.Draft;
                    air.IsInspected = false;
                    air.WithdrawnBy = user;
                    air.WithdrawnDt = DateTime.Now;
                    air.WithdrawalReason = string.IsNullOrWhiteSpace(reason) ? "Submission withdrawn by inspector." : reason;
                    air.UpdatedBy = user;
                    air.UpdatedDt = DateTime.Now;

                    // Seed draft payload for the wizard
                    var submittedModel = await GetSubmittedWizardViewModelAsync(air.Id);
                    var draftModel = submittedModel ?? await GetAirInspectionForEditAsync(air.Id);
                    if (draftModel != null)
                    {
                        SanitizeDraftForRevision(draftModel);
                        draftModel.AirId = air.Id;
                        draftModel.AirNo = air.AIRNo ?? air.CtrlNo;
                        draftModel.OverallStatus = AirStatuses.Draft;
                        draftModel.InspectionStatus = AirInspectionStatuses.Draft;
                        draftModel.IsReadOnly = false;
                        draftModel.IsSubmittedView = false;
                        draftModel.CanWithdraw = false;
                        draftModel.CurrentStep = 2;
                    }

                    // Sync or create associated AIRWizardProgress pointing to this SAME AIR
                    var progress = await _db.AIRWizardProgresses
                        .Where(p => p.AIRId == air.Id || p.SourceAIRId == air.Id || p.Id == air.Id)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (progress != null)
                    {
                        progress.AIRId = air.Id;
                        progress.SourceAIRId = air.Id;
                        progress.OrderId = air.OrderId;
                        progress.Status = AirWizardStatuses.Draft;
                        progress.IsCompleted = false;
                        progress.CurrentStep = 2;
                        progress.LastStep = 2;
                        progress.UserId = user;
                        progress.UpdatedAt = DateTime.Now;
                        if (draftModel != null)
                        {
                            draftModel.DraftId = progress.Id;
                            draftModel.DraftNo = progress.DraftNo;
                            progress.WizardStateJson = JsonConvert.SerializeObject(draftModel);
                        }
                    }
                    else
                    {
                        var newDraftId = air.Id;
                        var newDraftNo = await GenerateDraftNumberAsync(DateTime.Now);
                        if (draftModel != null)
                        {
                            draftModel.DraftId = newDraftId;
                            draftModel.DraftNo = newDraftNo;
                        }

                        progress = new AIRWizardProgress
                        {
                            Id = newDraftId,
                            DraftNo = newDraftNo,
                            OrderId = air.OrderId,
                            UserId = user,
                            CreatedBy = user,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            CurrentStep = 2,
                            LastStep = 2,
                            Status = AirWizardStatuses.Draft,
                            AIRId = air.Id,
                            SourceAIRId = air.Id,
                            RevisionNo = 1,
                            IsCompleted = false,
                            WizardStateJson = draftModel != null ? JsonConvert.SerializeObject(draftModel) : null
                        };
                        _db.AIRWizardProgresses.Add(progress);
                    }

                    _historyService.AddStatusHistory(
                        DocumentTypes.AcceptanceInspectionReport,
                        air.Id,
                        air.AIRNo ?? air.CtrlNo,
                        AirStatuses.SubmittedForAcceptance,
                        AirStatuses.Draft,
                        "Withdraw Submission",
                        air.WithdrawalReason,
                        user
                    );
                    await _db.SaveChangesAsync();

                    tx.Commit();
                    return air.Id;
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    tx.Rollback();
                    var errors = new System.Collections.Generic.List<string>();
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            errors.Add(string.Format("{0}.{1}: {2}", eve.Entry.Entity.GetType().Name, ve.PropertyName, ve.ErrorMessage));
                        }
                    }
                    throw new InvalidOperationException("Validation failed: " + string.Join("; ", errors), ex);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
        public async Task RequestWithdrawalAsync(Guid airId, string reason, string user)
        {
            var air = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR record not found.");

            if (air.PostedDt != null)
            {
                throw new InvalidOperationException("Cannot request recall for a posted AIR.");
            }

            air.WithdrawalRequested = true;
            air.WithdrawalReason = reason;
            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                AirStatuses.AcceptanceInProgress,
                AirStatuses.AcceptanceInProgress,
                "Recall Requested",
                string.Format("Recall requested by {0}: {1}", user, reason),
                user
            );
            await _db.SaveChangesAsync();
        }

        public async Task DiscardDraftAsync(Guid draftId, string user)
        {
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Resolve progress and air
                    var progress = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.Id == draftId);
                    Guid? targetAirId = null;
                    if (progress != null)
                    {
                        targetAirId = progress.AIRId ?? progress.SourceAIRId;
                    }

                    var air = await _db.AIRs
                        .Include(a => a.AIRItems.Select(ai => ai.AIRItemAllocations))
                        .Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns))
                        .Include(a => a.AIRItems.Select(ai => ai.AIRSubItems))
                        .Include(a => a.AIRInvoices)
                        .Include(a => a.AIRDocuments)
                        .Include(a => a.AIRSubmissionSnapshots)
                        .FirstOrDefaultAsync(a => a.Id == (targetAirId ?? draftId));

                    if (air != null && progress == null)
                    {
                        progress = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.AIRId == air.Id || p.SourceAIRId == air.Id || p.Id == air.Id);
                    }

                    if (air == null && progress == null)
                    {
                        throw new InvalidOperationException("Draft record not found.");
                    }

                    // 2. Validate AIR if present
                    if (air != null)
                    {
                        string reason;
                        if (!CanDiscardAir(air, out reason))
                        {
                            throw new InvalidOperationException(reason);
                        }

                        var inventoryPostedIds = await GetInventoryPostedAirIdsAsync();
                        if (inventoryPostedIds.Contains(air.Id))
                        {
                            throw new InvalidOperationException("This AIR cannot be discarded because downstream acceptance or inventory transactions already exist.");
                        }

                        // 3. Remove dependent child records belonging only to this AIR
                        if (air.AIRItems != null && air.AIRItems.Any())
                        {
                            var airItemIds = air.AIRItems.Select(ai => ai.Id).ToList();

                            var allocations = await _db.AIRItemAllocations.Where(al => airItemIds.Contains(al.AIRItemId)).ToListAsync();
                            if (allocations.Any())
                            {
                                _db.AIRItemAllocations.RemoveRange(allocations);
                            }

                            var extns = await _db.AIRItemExtns.Where(e => e.AIRItemId.HasValue && airItemIds.Contains(e.AIRItemId.Value)).ToListAsync();
                            if (extns.Any())
                            {
                                _db.AIRItemExtns.RemoveRange(extns);
                            }

                            var subItems = await _db.AIRSubItems.Where(s => airItemIds.Contains(s.AirItemId)).ToListAsync();
                            if (subItems.Any())
                            {
                                _db.AIRSubItems.RemoveRange(subItems);
                            }

                            _db.AIRItems.RemoveRange(air.AIRItems);
                        }

                        if (air.AIRInvoices != null && air.AIRInvoices.Any())
                        {
                            _db.AIRInvoices.RemoveRange(air.AIRInvoices);
                        }

                        if (air.AIRDocuments != null && air.AIRDocuments.Any())
                        {
                            _db.AIRDocuments.RemoveRange(air.AIRDocuments);
                        }

                        if (air.AIRSubmissionSnapshots != null && air.AIRSubmissionSnapshots.Any())
                        {
                            _db.AIRSubmissionSnapshots.RemoveRange(air.AIRSubmissionSnapshots);
                        }

                        _historyService.AddStatusHistory(
                            DocumentTypes.AcceptanceInspectionReport,
                            air.Id,
                            air.AIRNo ?? air.CtrlNo,
                            air.OverallStatus ?? AirStatuses.Draft,
                            AirStatuses.Cancelled,
                            "Draft Discarded",
                            "Inspection draft permanently discarded and abandoned. Inspected quantities restored to PO.",
                            user
                        );

                        _db.AIRs.Remove(air);
                    }

                    // 4. Mark related progress records as discarded
                    var relatedProgresses = await _db.AIRWizardProgresses
                        .Where(p => p.Id == draftId || (air != null && (p.AIRId == air.Id || p.SourceAIRId == air.Id)))
                        .ToListAsync();

                    foreach (var p in relatedProgresses)
                    {
                        p.Status = AirWizardStatuses.Discarded;
                        p.DiscardedDt = DateTime.Now;
                        p.DiscardedBy = user;
                        p.UpdatedAt = DateTime.Now;
                        p.IsCompleted = true;
                    }

                    await _db.SaveChangesAsync();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public async Task<AIRWizardViewModel> ChangeDraftPOAsync(Guid draftId, Guid newOrderId, string user)
        {
            var progress = await _db.AIRWizardProgresses.FirstOrDefaultAsync(p => p.Id == draftId);
            if (progress == null) throw new InvalidOperationException("Draft not found.");
            if (progress.Status != AirWizardStatuses.Draft || progress.IsCompleted)
                throw new InvalidOperationException("Only an active draft can change its purchase order.");
            if (progress.UserId != user && progress.CreatedBy != user)
                throw new InvalidOperationException("You cannot modify another user's inspection draft.");

            progress.OrderId = newOrderId;
            progress.CurrentStep = 2;
            progress.LastStep = 2;
            progress.UpdatedAt = DateTime.Now;

            var newDetails = await GetPOInspectionDetailsAsync(newOrderId, null);
            newDetails.DraftId = progress.Id;
            newDetails.DraftNo = progress.DraftNo;
            newDetails.CurrentStep = 2;
            newDetails.SourceAIRId = progress.SourceAIRId;
            newDetails.RevisionNo = progress.RevisionNo;
            newDetails.WizardProgressStatus = progress.Status;

            if (!string.IsNullOrWhiteSpace(progress.WizardStateJson))
            {
                try
                {
                    var prev = JsonConvert.DeserializeObject<AIRWizardViewModel>(progress.WizardStateJson);
                    if (prev != null)
                    {
                        newDetails.InspectionDate = prev.InspectionDate;
                        newDetails.InspectorName = prev.InspectorName;
                        newDetails.InspectorDesignation = prev.InspectorDesignation;
                        newDetails.InspectionCommittee = prev.InspectionCommittee;
                        newDetails.InspectionLocation = prev.InspectionLocation;
                        newDetails.GeneralRemarks = prev.GeneralRemarks;
                    }
                }
                catch { }
            }

            progress.WizardStateJson = JsonConvert.SerializeObject(newDetails);
            await _db.SaveChangesAsync();

            return newDetails;
        }

        private async Task<string> GenerateDraftNumberAsync(DateTime date)
        {
            var prefix = string.Format("DRAFT-AIR-{0:yyyyMM}-", date);
            var latest = await _db.AIRWizardProgresses
                .Where(p => p.DraftNo != null && p.DraftNo.StartsWith(prefix))
                .OrderByDescending(p => p.DraftNo)
                .Select(p => p.DraftNo)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (!string.IsNullOrEmpty(latest) && latest.Length >= prefix.Length + 4)
            {
                int parsed;
                if (int.TryParse(latest.Substring(prefix.Length, 4), out parsed))
                {
                    seq = parsed + 1;
                }
            }
            return string.Format("{0}{1:D4}", prefix, seq);
        }

        public async Task<AIRItemHistoryViewModel> GetItemInspectionHistoryAsync(Guid orderItemId)
        {
            var orderItem = await _db.OrderItems.FirstOrDefaultAsync(oi => oi.Id == orderItemId);
            if (orderItem == null) throw new InvalidOperationException("Order item not found.");

            var oirList = await _db.OrderItemRequests.Where(r => r.OrderItemId == orderItemId).ToListAsync();
            var oirIds = oirList.Select(r => r.Id).ToList();

            decimal orderedQty = oirList.Sum(r => (decimal?)r.QtyApplied ?? 0);
            if (orderedQty == 0) orderedQty = orderItem.Qty ?? 0;

            var inspections = await _db.AIRItems
                .Include(ai => ai.AIR)
                .Where(ai => (ai.OrderItemRequestId.HasValue && oirIds.Contains(ai.OrderItemRequestId.Value)) ||
                             (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == orderItemId))
                .OrderByDescending(ai => ai.AIR.AIRDate ?? ai.AIR.InsertedDt)
                .ToListAsync();

            var list = inspections.Select(i => new AIRItemPastInspectionViewModel
            {
                AirId = i.AIR.Id,
                AirNo = i.AIR.AIRNo ?? i.AIR.CtrlNo ?? "Draft",
                InspectionDate = i.AIR.InspectedDate ?? i.AIR.AIRDate ?? i.AIR.InsertedDt,
                InspectorName = i.AIR.InspectorName ?? i.AIR.Officer ?? "N/A",
                InspectedQty = i.Qty ?? 0,
                InvoiceNo = i.AIR.InvoiceNo ?? "N/A",
                Status = ResolveOverallStatus(i.AIR)
            }).ToList();

            decimal totalPosted = inspections.Where(i => i.AIR.PostedDt != null).Sum(i => i.Qty ?? 0);

            return new AIRItemHistoryViewModel
            {
                OrderItemId = orderItem.Id,
                ItemNo = orderItem.ItemNo,
                Description = orderItem.Description ?? orderItem.ItemName,
                OrderedQty = orderedQty,
                TotalPostedInspectedQty = totalPosted,
                Inspections = list
            };
        }

        private void SyncAirItems(AIR air, AIRWizardViewModel model, string user)
        {
            var processedItemIds = new HashSet<Guid>();

            foreach (var item in model.Items)
            {
                var existingItem = air.AIRItems.FirstOrDefault(ai =>
                    (ai.OrderItemRequestId.HasValue && item.OrderItemRequestId != Guid.Empty && ai.OrderItemRequestId.Value == item.OrderItemRequestId) ||
                    (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == item.OrderItemId) ||
                    (item.Allocations != null && item.Allocations.Any(al => ai.OrderItemRequestId == al.OrderItemRequestId)));

                if (item.InspectNowQty > 0)
                {
                    Guid primaryOirId = item.OrderItemRequestId;
                    if (primaryOirId == Guid.Empty && item.Allocations != null && item.Allocations.Any())
                    {
                        primaryOirId = item.Allocations.First().OrderItemRequestId;
                    }

                    if (existingItem == null)
                    {
                        var oir = primaryOirId != Guid.Empty 
                            ? _db.OrderItemRequests.FirstOrDefault(r => r.Id == primaryOirId) 
                            : _db.OrderItemRequests.FirstOrDefault(r => r.OrderItemId == item.OrderItemId);

                        if (oir == null)
                        {
                            oir = new OrderItemRequest
                            {
                                Id = primaryOirId != Guid.Empty ? primaryOirId : Guid.NewGuid(),
                                OrderItemId = item.OrderItemId,
                                QtyApplied = (int)Math.Round(item.OrderedQty),
                                InsertedBy = user,
                                InsertedDt = DateTime.Now
                            };
                            _db.OrderItemRequests.Add(oir);
                        }

                        existingItem = new AIRItem
                        {
                            Id = item.AirItemId.HasValue && item.AirItemId.Value != Guid.Empty ? item.AirItemId.Value : Guid.NewGuid(),
                            AirId = air.Id,
                            OrderItemRequestId = oir.Id,
                            Qty = item.InspectNowQty,
                            InspectedQty = item.InspectNowQty,
                            Remarks = item.Remarks,
                            Disposition = item.Disposition,
                            InvDist = item.Disposition == "For Distribution" ? "D" : (item.Disposition == "Inventory" ? "I" : item.Disposition),
                            InsertedBy = user,
                            InsertedDt = DateTime.Now
                        };
                        air.AIRItems.Add(existingItem);
                    }
                    else
                    {
                        existingItem.Qty = item.InspectNowQty;
                        existingItem.InspectedQty = item.InspectNowQty;
                        existingItem.Remarks = item.Remarks;
                        existingItem.Disposition = item.Disposition;
                        existingItem.InvDist = item.Disposition == "For Distribution" ? "D" : (item.Disposition == "Inventory" ? "I" : item.Disposition);
                        existingItem.UpdatedBy = user;
                        existingItem.UpdatedDt = DateTime.Now;
                    }

                    item.AirItemId = existingItem.Id;
                    processedItemIds.Add(existingItem.Id);

                    // Sync AIRItemAllocations children
                    if (item.Allocations != null && item.Allocations.Any())
                    {
                        var currentAllocations = existingItem.AIRItemAllocations != null
                            ? existingItem.AIRItemAllocations.ToList()
                            : _db.AIRItemAllocations.Where(a => a.AIRItemId == existingItem.Id).ToList();

                        foreach (var allocVm in item.Allocations)
                        {
                            var existingAlloc = currentAllocations.FirstOrDefault(a => a.OrderItemRequestId == allocVm.OrderItemRequestId);
                            if (existingAlloc == null)
                            {
                                existingAlloc = new AIRItemAllocation
                                {
                                    Id = allocVm.Id.HasValue && allocVm.Id.Value != Guid.Empty ? allocVm.Id.Value : Guid.NewGuid(),
                                    AIRItemId = existingItem.Id,
                                    OrderItemRequestId = allocVm.OrderItemRequestId,
                                    QtyAllocated = allocVm.QtyAllocated,
                                    QtyInspected = allocVm.QtyInspected,
                                    PRNumber = allocVm.PRNumber,
                                    Department = allocVm.Department,
                                    Remarks = allocVm.Remarks,
                                    InsertedBy = user,
                                    InsertedDt = DateTime.Now
                                };
                                _db.AIRItemAllocations.Add(existingAlloc);
                                existingItem.AIRItemAllocations.Add(existingAlloc);
                            }
                            else
                            {
                                existingAlloc.QtyAllocated = allocVm.QtyAllocated;
                                existingAlloc.QtyInspected = allocVm.QtyInspected;
                                existingAlloc.PRNumber = allocVm.PRNumber;
                                existingAlloc.Department = allocVm.Department;
                                existingAlloc.Remarks = allocVm.Remarks;
                                existingAlloc.UpdatedBy = user;
                                existingAlloc.UpdatedDt = DateTime.Now;
                            }
                        }

                        // Remove orphans no longer in model.Allocations
                        var modelOirIds = item.Allocations.Select(a => a.OrderItemRequestId).ToList();
                        var orphans = currentAllocations.Where(a => !modelOirIds.Contains(a.OrderItemRequestId)).ToList();
                        foreach (var orphan in orphans)
                        {
                            _db.AIRItemAllocations.Remove(orphan);
                            existingItem.AIRItemAllocations.Remove(orphan);
                        }
                    }
                }
                else
                {
                    if (existingItem != null)
                    {
                        var currentAllocations = existingItem.AIRItemAllocations != null
                            ? existingItem.AIRItemAllocations.ToList()
                            : _db.AIRItemAllocations.Where(a => a.AIRItemId == existingItem.Id).ToList();
                        foreach (var alloc in currentAllocations)
                        {
                            _db.AIRItemAllocations.Remove(alloc);
                            existingItem.AIRItemAllocations.Remove(alloc);
                        }

                        var subItems = _db.AIRSubItems.Where(s => s.AirItemId == existingItem.Id).ToList();
                        foreach (var s in subItems)
                        {
                            var sExtns = _db.AIRItemExtns.Where(e => e.AIRSubItemId == s.Id).ToList();
                            foreach (var se in sExtns)
                            {
                                _db.AIRItemExtns.Remove(se);
                            }
                            _db.AIRSubItems.Remove(s);
                        }

                        var extns = existingItem.AIRItemExtns != null 
                            ? existingItem.AIRItemExtns.ToList() 
                            : _db.AIRItemExtns.Where(e => e.AIRItemId == existingItem.Id).ToList();
                        foreach (var extn in extns)
                        {
                            _db.AIRItemExtns.Remove(extn);
                        }
                        _db.AIRItems.Remove(existingItem);
                    }
                }
            }

            // Clean up any remaining AIRItems on the entity that were completely removed from the model
            var orphanItems = air.AIRItems.Where(ai => !processedItemIds.Contains(ai.Id)).ToList();
            foreach (var orphan in orphanItems)
            {
                var allocs = orphan.AIRItemAllocations != null ? orphan.AIRItemAllocations.ToList() : _db.AIRItemAllocations.Where(a => a.AIRItemId == orphan.Id).ToList();
                foreach (var a in allocs)
                {
                    _db.AIRItemAllocations.Remove(a);
                    orphan.AIRItemAllocations.Remove(a);
                }
                var subItems = _db.AIRSubItems.Where(s => s.AirItemId == orphan.Id).ToList();
                foreach (var s in subItems)
                {
                    var sExtns = _db.AIRItemExtns.Where(e => e.AIRSubItemId == s.Id).ToList();
                    foreach (var se in sExtns) _db.AIRItemExtns.Remove(se);
                    _db.AIRSubItems.Remove(s);
                }
                var extns = orphan.AIRItemExtns != null ? orphan.AIRItemExtns.ToList() : _db.AIRItemExtns.Where(e => e.AIRItemId == orphan.Id).ToList();
                foreach (var e in extns) _db.AIRItemExtns.Remove(e);
                _db.AIRItems.Remove(orphan);
                air.AIRItems.Remove(orphan);
            }

            // Save AIR and parent AIRItems first so their IDs exist in DB before adding child AIRSubItems
            _db.SaveChanges();

            // Sync sub-item headers for inspected items
            foreach (var item in model.Items.Where(i => i.InspectNowQty > 0))
            {
                var existingItem = air.AIRItems.FirstOrDefault(ai =>
                    (item.AirItemId.HasValue && ai.Id == item.AirItemId.Value) ||
                    (ai.OrderItemRequestId.HasValue && item.OrderItemRequestId != Guid.Empty && ai.OrderItemRequestId.Value == item.OrderItemRequestId) ||
                    (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == item.OrderItemId) ||
                    (item.Allocations != null && item.Allocations.Any(al => ai.OrderItemRequestId == al.OrderItemRequestId)));

                if (existingItem != null && item.SubItems != null && item.SubItems.Any())
                {
                    SyncSubItemHeaders(existingItem, item, user);
                }
            }

            // Save sub-item headers so their IDs exist in DB before adding child extensions
            _db.SaveChanges();

            // Now sync extensions for parent items and sub-items
            foreach (var item in model.Items.Where(i => i.InspectNowQty > 0))
            {
                var existingItem = air.AIRItems.FirstOrDefault(ai =>
                    (item.AirItemId.HasValue && ai.Id == item.AirItemId.Value) ||
                    (ai.OrderItemRequestId.HasValue && item.OrderItemRequestId != Guid.Empty && ai.OrderItemRequestId.Value == item.OrderItemRequestId) ||
                    (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == item.OrderItemId) ||
                    (item.Allocations != null && item.Allocations.Any(al => ai.OrderItemRequestId == al.OrderItemRequestId)));

                if (existingItem != null)
                {
                    SyncItemExtensions(existingItem, item, user);
                    SyncSubItemExtensions(existingItem, item, user);
                }
            }
        }

        private void SyncSubItemHeaders(AIRItem airItem, AIRLineItemViewModel item, string user)
        {
            var existingSubItems = _db.AIRSubItems.Where(s => s.AirItemId == airItem.Id).ToList();
            var processedIds = new HashSet<Guid>();

            foreach (var subVm in item.SubItems)
            {
                AIRSubItem existingSub = null;
                if (subVm.AirSubItemId.HasValue && subVm.AirSubItemId.Value != Guid.Empty)
                {
                    existingSub = existingSubItems.FirstOrDefault(s => s.Id == subVm.AirSubItemId.Value);
                }
                else if (subVm.OrderSubItemId != Guid.Empty)
                {
                    existingSub = existingSubItems.FirstOrDefault(s => s.OrderSubItemId == subVm.OrderSubItemId);
                }

                bool isOrdered = subVm.OrderSubItemId != Guid.Empty;
                string normalizedSourceType = SubItemSourceTypes.Normalize(subVm.SourceType, isOrdered);
                bool isBundleRequired = SubItemSourceTypes.ResolveBundleRequired(normalizedSourceType, subVm.IsRequiredForBundle, isOrdered);

                string cleanSubItemNo = !string.IsNullOrWhiteSpace(subVm.SubItemNo)
                    ? subVm.SubItemNo.Trim().TrimStart('#').Trim()
                    : null;

                if (string.IsNullOrWhiteSpace(cleanSubItemNo))
                {
                    int maxSeq = 0;
                    foreach (var s in existingSubItems)
                    {
                        int num;
                        if (!string.IsNullOrWhiteSpace(s.SubItemNo))
                        {
                            string sClean = s.SubItemNo.Trim().TrimStart('#').Trim();
                            if (int.TryParse(sClean, out num) && num > maxSeq)
                            {
                                maxSeq = num;
                            }
                        }
                    }
                    cleanSubItemNo = (maxSeq > 0 ? maxSeq + 1 : (existingSubItems.Count + 1)).ToString();
                }

                if (existingSub == null)
                {
                    existingSub = new AIRSubItem
                    {
                        Id = subVm.AirSubItemId.HasValue && subVm.AirSubItemId.Value != Guid.Empty ? subVm.AirSubItemId.Value : Guid.NewGuid(),
                        AirItemId = airItem.Id,
                        OrderSubItemId = isOrdered ? (Guid?)subVm.OrderSubItemId : null,
                        OrderSubItemRequestId = subVm.OrderSubItemRequestId,
                        SubItemNo = cleanSubItemNo,
                        Description = subVm.Description,
                        Unit = subVm.Unit,
                        ExpectedQty = subVm.ExpectedQty > 0 ? subVm.ExpectedQty : subVm.InspectNowQty,
                        InspectedQty = subVm.InspectNowQty,
                        AcceptedQty = 0,
                        Remarks = subVm.Remarks,
                        QtyPerParent = subVm.QtyPerParent > 0 ? subVm.QtyPerParent : 1,
                        CategoryCode = subVm.CategoryCode,
                        ItemExtnName = subVm.ItemExtnName,
                        SourceType = normalizedSourceType,
                        IsRequiredForBundle = isBundleRequired,
                        InsertedBy = user,
                        InsertedDt = DateTime.Now
                    };
                    _db.AIRSubItems.Add(existingSub);
                    existingSubItems.Add(existingSub);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(cleanSubItemNo))
                    {
                        existingSub.SubItemNo = cleanSubItemNo;
                    }
                    else if (!string.IsNullOrWhiteSpace(existingSub.SubItemNo) && existingSub.SubItemNo.Contains("#"))
                    {
                        existingSub.SubItemNo = existingSub.SubItemNo.Trim().TrimStart('#').Trim();
                    }
                    existingSub.InspectedQty = subVm.InspectNowQty;
                    existingSub.Remarks = subVm.Remarks;
                    existingSub.QtyPerParent = subVm.QtyPerParent > 0 ? subVm.QtyPerParent : 1;
                    existingSub.CategoryCode = subVm.CategoryCode;
                    existingSub.ItemExtnName = subVm.ItemExtnName;
                    existingSub.SourceType = normalizedSourceType;
                    existingSub.IsRequiredForBundle = isBundleRequired;
                    if (!existingSub.OrderSubItemId.HasValue)
                    {
                        if (!string.IsNullOrWhiteSpace(subVm.Description))
                        {
                            existingSub.Description = subVm.Description;
                        }
                        if (!string.IsNullOrWhiteSpace(subVm.Unit))
                        {
                            existingSub.Unit = subVm.Unit;
                        }
                    }
                    existingSub.UpdatedBy = user;
                    existingSub.UpdatedDt = DateTime.Now;
                }
                subVm.AirSubItemId = existingSub.Id;
                processedIds.Add(existingSub.Id);
            }

            var toDelete = existingSubItems.Where(s => !s.OrderSubItemId.HasValue && !processedIds.Contains(s.Id)).ToList();
            foreach (var delSub in toDelete)
            {
                var delExtns = _db.AIRItemExtns.Where(e => e.AIRSubItemId == delSub.Id).ToList();
                if (delExtns.Any())
                {
                    _db.AIRItemExtns.RemoveRange(delExtns);
                }
                _db.AIRSubItems.Remove(delSub);
            }
        }

        private void SyncItemExtensions(AIRItem airItem, AIRLineItemViewModel item, string user)
        {
            var existingExtns = airItem.AIRItemExtns != null 
                ? airItem.AIRItemExtns.ToList() 
                : _db.AIRItemExtns.Where(e => e.AIRItemId == airItem.Id).ToList();

            if (item.Disposition != "Inventory" || !item.ParentRequiresInventory)
            {
                foreach (var extn in existingExtns)
                {
                    _db.AIRItemExtns.Remove(extn);
                }
                return;
            }

            int targetQty = (int)Math.Floor(item.InspectNowQty);
            string extnType = item.ItemExtnName;
            if (string.IsNullOrWhiteSpace(extnType)) extnType = "ItemExtnOther";

            // Remove excess extensions if quantity was reduced
            var excess = existingExtns.Where(e => e.ContentNo > targetQty).ToList();
            foreach (var e in excess)
            {
                _db.AIRItemExtns.Remove(e);
                existingExtns.Remove(e);
            }

            if (item.InventoryDetails == null) return;

            for (int q = 1; q <= targetQty; q++)
            {
                var detail = item.InventoryDetails.FirstOrDefault(d => d.ContentNo == q) 
                             ?? (q <= item.InventoryDetails.Count ? item.InventoryDetails[q - 1] : null);

                var existingExtn = existingExtns.FirstOrDefault(e => e.ContentNo == q);

                if (existingExtn != null)
                {
                    existingExtn.UpdatedBy = user;
                    existingExtn.UpdatedDt = DateTime.Now;
                    UpdateConcreteExtension(existingExtn, detail);
                }
                else
                {
                    var newExtn = CreateConcreteExtension(airItem.Id, null, q, detail, extnType, user);
                    airItem.AIRItemExtns.Add(newExtn);
                }
            }
        }

        private void SyncSubItemExtensions(AIRItem airItem, AIRLineItemViewModel item, string user)
        {
            var existingSubItems = _db.AIRSubItems.Where(s => s.AirItemId == airItem.Id).ToList();

            if (item.InspectNowQty <= 0 || item.SubItems == null || !item.SubItems.Any())
            {
                foreach (var sub in existingSubItems)
                {
                    var subExtns = _db.AIRItemExtns.Where(e => e.AIRSubItemId == sub.Id).ToList();
                    foreach (var e in subExtns)
                    {
                        _db.AIRItemExtns.Remove(e);
                    }
                }
                return;
            }

            foreach (var subVm in item.SubItems)
            {
                var existingSub = subVm.AirSubItemId.HasValue && subVm.AirSubItemId.Value != Guid.Empty ? existingSubItems.FirstOrDefault(s => s.Id == subVm.AirSubItemId.Value) : (subVm.OrderSubItemId != Guid.Empty ? existingSubItems.FirstOrDefault(s => s.OrderSubItemId == subVm.OrderSubItemId) : null);
                if (existingSub == null) continue;

                // Sync extensions for this sub-item
                var existingSubExtns = _db.AIRItemExtns.Where(e => e.AIRSubItemId == existingSub.Id).ToList();

                if (item.Disposition != "Inventory" || !subVm.RequiresInventory || subVm.InspectNowQty <= 0)
                {
                    foreach (var extn in existingSubExtns)
                    {
                        _db.AIRItemExtns.Remove(extn);
                    }
                    continue;
                }

                int targetQty = (int)Math.Floor(subVm.InspectNowQty);
                string extnType = subVm.ItemExtnName;
                if (string.IsNullOrWhiteSpace(extnType)) extnType = "ItemExtnOther";

                var excess = existingSubExtns.Where(e => e.ContentNo > targetQty).ToList();
                foreach (var e in excess)
                {
                    _db.AIRItemExtns.Remove(e);
                    existingSubExtns.Remove(e);
                }

                if (subVm.InventoryDetails == null) continue;

                for (int q = 1; q <= targetQty; q++)
                {
                    var detail = subVm.InventoryDetails.FirstOrDefault(d => d.ContentNo == q)
                                 ?? (q <= subVm.InventoryDetails.Count ? subVm.InventoryDetails[q - 1] : null);

                    var existingExtn = existingSubExtns.FirstOrDefault(e => e.ContentNo == q);

                    if (existingExtn != null)
                    {
                        existingExtn.UpdatedBy = user;
                        existingExtn.UpdatedDt = DateTime.Now;
                        UpdateConcreteExtension(existingExtn, detail);
                    }
                    else
                    {
                        var newExtn = CreateConcreteExtension(null, existingSub.Id, q, detail, extnType, user);
                        _db.AIRItemExtns.Add(newExtn);
                    }
                }
            }
        }

        private static AIRItemExtn CreateConcreteExtension(Guid? airItemId, Guid? airSubItemId, int contentNo, AIRItemInventoryDetailViewModel d, string extnType, string user)
        {
            DateTime now = DateTime.Now;
            if (extnType == "ItemExtnVehicle")
            {
                return new AIRItemExtnVehicle
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItemId,
                    AIRSubItemId = airSubItemId,
                    ContentNo = contentNo,
                    TContentNo = contentNo,
                    ConductionNo = d?.ConductionNo ?? "",
                    EngineNo = d?.EngineNo ?? "",
                    ChasisNo = d?.ChasisNo ?? "",
                    PlateNo = d?.PlateNo ?? "",
                    Color = d?.Color ?? "",
                    YearModel = d?.YearModel,
                    SeriesNo = d?.SeriesNo ?? "",
                    MVFileNo = d?.MVFileNo ?? "",
                    CRN = d?.CRN ?? "",
                    CRDate = d?.CRDate,
                    OrNo = d?.OrNo ?? "",
                    OrDate = d?.OrDate,
                    NetWeight = d?.NetWeight,
                    InsPolicyNo = d?.InsPolicyNo ?? "",
                    InsertedBy = user,
                    InsertedDt = now,
                    UpdatedBy = user,
                    UpdatedDt = now
                };
            }
            else if (extnType == "ItemExtnLand")
            {
                return new AIRItemExtnLand
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItemId,
                    AIRSubItemId = airSubItemId,
                    ContentNo = contentNo,
                    TContentNo = contentNo,
                    PIN = d?.PIN ?? "",
                    Address = d?.Address ?? "",
                    LandMarks = d?.LandMarks ?? "",
                    TctNo = d?.TctNo ?? "",
                    DRPNo = d?.DRPNo ?? "",
                    MarketValue = d?.MarketValue,
                    PricePerSqm = d?.PricePerSqm,
                    InsertedBy = user,
                    InsertedDt = now,
                    UpdatedBy = user,
                    UpdatedDt = now
                };
            }
            else if (extnType == "ItemExtnBuilding")
            {
                return new AIRItemExtnBuilding
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItemId,
                    AIRSubItemId = airSubItemId,
                    ContentNo = contentNo,
                    TContentNo = contentNo,
                    ProjectName = d?.ProjectName ?? "",
                    BuildingType = d?.BuildingType ?? "",
                    Address = d?.Address ?? "",
                    Area = d?.Area,
                    Status = d?.Status ?? "",
                    Condition = d?.Condition ?? "",
                    InsertedBy = user,
                    InsertedDt = now,
                    UpdatedBy = user,
                    UpdatedDt = now
                };
            }
            else // ItemExtnOther
            {
                return new AIRItemExtnOther
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItemId,
                    AIRSubItemId = airSubItemId,
                    ContentNo = contentNo,
                    TContentNo = contentNo,
                    SerialNo = d?.SerialNo ?? "",
                    Condition = d?.Condition ?? "",
                    InsertedBy = user,
                    InsertedDt = now,
                    UpdatedBy = user,
                    UpdatedDt = now
                };
            }
        }

        private static void PopulateDetailFromExtension(AIRItemInventoryDetailViewModel detail, AIRItemExtn extn)
        {
            if (extn is AIRItemExtnVehicle v)
            {
                detail.ConductionNo = v.ConductionNo;
                detail.EngineNo = v.EngineNo;
                detail.ChasisNo = v.ChasisNo;
                detail.PlateNo = v.PlateNo;
                detail.Color = v.Color;
                detail.YearModel = v.YearModel;
                detail.SeriesNo = v.SeriesNo;
                detail.MVFileNo = v.MVFileNo;
                detail.CRN = v.CRN;
                detail.CRDate = v.CRDate;
                detail.OrNo = v.OrNo;
                detail.OrDate = v.OrDate;
                detail.NetWeight = v.NetWeight;
                detail.InsPolicyNo = v.InsPolicyNo;
            }
            else if (extn is AIRItemExtnLand l)
            {
                detail.PIN = l.PIN;
                detail.Address = l.Address;
                detail.LandMarks = l.LandMarks;
                detail.TctNo = l.TctNo;
                detail.DRPNo = l.DRPNo;
                detail.MarketValue = l.MarketValue;
                detail.PricePerSqm = l.PricePerSqm;
            }
            else if (extn is AIRItemExtnBuilding b)
            {
                detail.ProjectName = b.ProjectName;
                detail.BuildingType = b.BuildingType;
                detail.Address = b.Address;
                detail.Area = b.Area;
                detail.Status = b.Status;
                detail.Condition = b.Condition;
            }
            else if (extn is AIRItemExtnOther o)
            {
                detail.SerialNo = o.SerialNo;
                detail.Condition = o.Condition;
            }
        }

        private static void UpdateConcreteExtension(AIRItemExtn extn, AIRItemInventoryDetailViewModel d)
        {
            if (d == null) return;

            if (extn is AIRItemExtnVehicle v)
            {
                v.ConductionNo = d.ConductionNo ?? v.ConductionNo;
                v.EngineNo = d.EngineNo ?? v.EngineNo;
                v.ChasisNo = d.ChasisNo ?? v.ChasisNo;
                v.PlateNo = d.PlateNo ?? v.PlateNo;
                v.Color = d.Color ?? v.Color;
                v.YearModel = d.YearModel ?? v.YearModel;
                v.SeriesNo = d.SeriesNo ?? v.SeriesNo;
                v.MVFileNo = d.MVFileNo ?? v.MVFileNo;
                v.CRN = d.CRN ?? v.CRN;
                v.CRDate = d.CRDate ?? v.CRDate;
                v.OrNo = d.OrNo ?? v.OrNo;
                v.OrDate = d.OrDate ?? v.OrDate;
                v.NetWeight = d.NetWeight ?? v.NetWeight;
                v.InsPolicyNo = d.InsPolicyNo ?? v.InsPolicyNo;
            }
            else if (extn is AIRItemExtnLand l)
            {
                l.PIN = d.PIN ?? l.PIN;
                l.Address = d.Address ?? l.Address;
                l.LandMarks = d.LandMarks ?? l.LandMarks;
                l.TctNo = d.TctNo ?? l.TctNo;
                l.DRPNo = d.DRPNo ?? l.DRPNo;
                l.MarketValue = d.MarketValue ?? l.MarketValue;
                l.PricePerSqm = d.PricePerSqm ?? l.PricePerSqm;
            }
            else if (extn is AIRItemExtnBuilding b)
            {
                b.ProjectName = d.ProjectName ?? b.ProjectName;
                b.BuildingType = d.BuildingType ?? b.BuildingType;
                b.Address = d.Address ?? b.Address;
                b.Area = d.Area ?? b.Area;
                b.Status = d.Status ?? b.Status;
                b.Condition = d.Condition ?? b.Condition;
            }
            else if (extn is AIRItemExtnOther o)
            {
                o.SerialNo = d.SerialNo ?? o.SerialNo;
                o.Condition = d.Condition ?? o.Condition;
            }
        }

        private static bool IsDetailCompleted(AIRItemInventoryDetailViewModel d, string extnType)
        {
            if (d == null) return false;
            if (extnType == "ItemExtnVehicle")
            {
                return !string.IsNullOrWhiteSpace(d.ConductionNo) || !string.IsNullOrWhiteSpace(d.EngineNo) || !string.IsNullOrWhiteSpace(d.PlateNo) || !string.IsNullOrWhiteSpace(d.ChasisNo);
            }
            else if (extnType == "ItemExtnLand")
            {
                return !string.IsNullOrWhiteSpace(d.PIN) || !string.IsNullOrWhiteSpace(d.TctNo) || !string.IsNullOrWhiteSpace(d.Address);
            }
            else if (extnType == "ItemExtnBuilding")
            {
                return !string.IsNullOrWhiteSpace(d.ProjectName) || !string.IsNullOrWhiteSpace(d.Address);
            }
            else // ItemExtnOther
            {
                return !string.IsNullOrWhiteSpace(d.SerialNo);
            }
        }

        private static string ResolveOverallStatus(AIR a)
        {
            if (a.PostedDt != null)
                return AirStatuses.Posted;

            if (string.Equals(a.OverallStatus, AirStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                return AirStatuses.Cancelled;

            if (string.Equals(a.OverallStatus, AirStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                return AirStatuses.Draft;

            if (string.Equals(a.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase) || string.Equals(a.OverallStatus, AirStatuses.Unposted, StringComparison.OrdinalIgnoreCase))
                return AirStatuses.Unposted;

            if (a.IsInspected == true && a.AcceptedDate != null)
                return AirStatuses.Accepted;

            if (a.IsInspected == true && a.AcceptanceStartedDt != null)
                return AirStatuses.AcceptanceInProgress;

            if (a.IsInspected == true && !string.IsNullOrWhiteSpace(a.RevisionComments))
                return AirStatuses.ReturnedForRevision;

            if (a.IsInspected == true && string.Equals(a.OverallStatus, AirStatuses.SubmittedForAcceptance, StringComparison.OrdinalIgnoreCase))
                return AirStatuses.SubmittedForAcceptance;

            if (string.Equals(a.OverallStatus, AirStatuses.Withdrawn, StringComparison.OrdinalIgnoreCase))
                return AirStatuses.Withdrawn;

            if (a.IsInspected == true)
                return AirStatuses.SubmittedForAcceptance;

            return AirStatuses.Draft;
        }

        private async Task<string> GenerateAirNumberAsync(DateTime date)
        {
            var prefix = date.ToString("yyyy-MM-");
            var latest = await _db.AIRs
                .Where(a => a.AIRNo != null && a.AIRNo.StartsWith(prefix))
                .OrderByDescending(a => a.AIRNo)
                .Select(a => a.AIRNo)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (!string.IsNullOrEmpty(latest) && latest.Length >= prefix.Length + 4)
            {
                var seqPart = latest.Substring(prefix.Length, 4);
                if (int.TryParse(seqPart, out int parsed))
                    seq = parsed + 1;
            }

            return prefix + seq.ToString("D4");
        }

        private void SyncAirInvoicesAndDocuments(AIR air, AIRWizardViewModel model, string user)
        {
            var submittedInvoices = model.Invoices ?? new List<AIRInvoiceViewModel>();
            var existingInvoices = air.AIRInvoices != null ? air.AIRInvoices.ToList() : new List<AIRInvoice>();
            var submittedInvIds = new HashSet<Guid>();

            // 1. Process submitted invoices (Add / Update)
            foreach (var invVm in submittedInvoices)
            {
                AIRInvoice invEntity = null;
                if (invVm.Id != Guid.Empty)
                {
                    invEntity = existingInvoices.FirstOrDefault(x => x.Id == invVm.Id);
                }
                if (invEntity == null && !string.IsNullOrWhiteSpace(invVm.SalesInvoiceNo))
                {
                    invEntity = existingInvoices.FirstOrDefault(x => string.Equals(x.InvoiceNo, invVm.SalesInvoiceNo, StringComparison.OrdinalIgnoreCase));
                }

                if (invEntity != null)
                {
                    // Update existing
                    invEntity.InvoiceNo = invVm.SalesInvoiceNo;
                    invEntity.InvoiceDate = invVm.InvoiceDate;
                    invEntity.Amount = invVm.InvoiceTotalAmount;
                    invEntity.UpdatedBy = user;
                    invEntity.UpdatedDt = DateTime.Now;
                    submittedInvIds.Add(invEntity.Id);
                }
                else
                {
                    // Insert new
                    var newId = invVm.Id != Guid.Empty ? invVm.Id : Guid.NewGuid();
                    invVm.Id = newId;
                    invEntity = new AIRInvoice
                    {
                        Id = newId,
                        AirId = air.Id,
                        InvoiceNo = invVm.SalesInvoiceNo,
                        InvoiceDate = invVm.InvoiceDate,
                        Amount = invVm.InvoiceTotalAmount,
                        InsertedBy = user,
                        InsertedDt = DateTime.Now,
                        UpdatedBy = user,
                        UpdatedDt = DateTime.Now
                    };
                    _db.AIRInvoices.Add(invEntity);
                    submittedInvIds.Add(newId);
                }

                // Invoice supporting document
                var docPrefix = "InvoiceDoc:" + invEntity.Id.ToString("D");
                var existingDoc = air.AIRDocuments != null ? air.AIRDocuments.FirstOrDefault(d => d.DocumentType == docPrefix) : null;
                if (invVm.SupportingDocument != null && !string.IsNullOrWhiteSpace(invVm.SupportingDocument.FilePath))
                {
                    if (existingDoc != null)
                    {
                        existingDoc.FileName = invVm.SupportingDocument.FileName;
                        existingDoc.FilePath = invVm.SupportingDocument.FilePath;
                        existingDoc.FileSize = invVm.SupportingDocument.FileSize;
                        existingDoc.UploadedBy = user;
                        existingDoc.UploadedDt = DateTime.Now;
                    }
                    else
                    {
                        var newDoc = new AIRDocument
                        {
                            Id = Guid.NewGuid(),
                            AirId = air.Id,
                            DocumentType = docPrefix,
                            FileName = invVm.SupportingDocument.FileName,
                            FilePath = invVm.SupportingDocument.FilePath,
                            FileSize = invVm.SupportingDocument.FileSize,
                            UploadedBy = user,
                            UploadedDt = DateTime.Now
                        };
                        _db.AIRDocuments.Add(newDoc);
                    }
                }
                else if (existingDoc != null)
                {
                    // Document removed from this invoice
                    _db.AIRDocuments.Remove(existingDoc);
                }
            }

            // 2. Delete removed invoices and their attached documents
            foreach (var existingInv in existingInvoices)
            {
                if (!submittedInvIds.Contains(existingInv.Id))
                {
                    var docPrefix = "InvoiceDoc:" + existingInv.Id.ToString("D");
                    var attachedDoc = air.AIRDocuments != null ? air.AIRDocuments.FirstOrDefault(d => d.DocumentType == docPrefix) : null;
                    if (attachedDoc != null)
                    {
                        _db.AIRDocuments.Remove(attachedDoc);
                    }
                    _db.AIRInvoices.Remove(existingInv);
                }
            }

            // 3. Process General Supplier Delivery Documents (SupportingDocuments)
            var submittedGeneralDocs = model.SupportingDocuments ?? new List<AIRDocumentViewModel>();
            var existingGeneralDocs = air.AIRDocuments != null 
                ? air.AIRDocuments.Where(d => !d.DocumentType.StartsWith("InvoiceDoc:")).ToList() 
                : new List<AIRDocument>();
            var submittedGeneralPaths = new HashSet<string>(submittedGeneralDocs.Select(d => d.FilePath).Where(p => !string.IsNullOrEmpty(p)), StringComparer.OrdinalIgnoreCase);

            foreach (var gDoc in submittedGeneralDocs)
            {
                if (string.IsNullOrWhiteSpace(gDoc.FilePath)) continue;
                var existingGDoc = existingGeneralDocs.FirstOrDefault(d => string.Equals(d.FilePath, gDoc.FilePath, StringComparison.OrdinalIgnoreCase));
                if (existingGDoc == null)
                {
                    var newDoc = new AIRDocument
                    {
                        Id = gDoc.Id != Guid.Empty ? gDoc.Id : Guid.NewGuid(),
                        AirId = air.Id,
                        DocumentType = !string.IsNullOrWhiteSpace(gDoc.DocumentType) ? gDoc.DocumentType : "GeneralSupplierDoc",
                        FileName = gDoc.FileName,
                        FilePath = gDoc.FilePath,
                        FileSize = gDoc.FileSize,
                        UploadedBy = user,
                        UploadedDt = DateTime.Now
                    };
                    _db.AIRDocuments.Add(newDoc);
                }
            }

            // Remove deleted general documents
            foreach (var egDoc in existingGeneralDocs)
            {
                if (!submittedGeneralPaths.Contains(egDoc.FilePath))
                {
                    _db.AIRDocuments.Remove(egDoc);
                }
            }

            // Set first invoice info on AIR header if present (for legacy queries, without comma concatenation)
            if (submittedInvoices.Any())
            {
                var first = submittedInvoices.First();
                air.InvoiceNo = first.SalesInvoiceNo;
                air.InvoiceDate = first.InvoiceDate;
                air.InvoiceAmount = first.InvoiceTotalAmount;
            }
            else
            {
                air.InvoiceNo = null;
                air.InvoiceDate = null;
                air.InvoiceAmount = null;
            }
        }
        public async Task<AIRSubmissionViewViewModel> GetSubmissionViewAsync(Guid airId, Guid? snapshotId = null)
        {
            var air = await _db.AIRs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) return null;

            var snapshots = await _db.AIRSubmissionSnapshots.AsNoTracking()
                .Where(s => s.AIRId == airId)
                .OrderByDescending(s => s.VersionNo)
                .ToListAsync();

            AIRSubmissionSnapshot activeSnapshot = null;
            if (snapshotId.HasValue)
            {
                activeSnapshot = snapshots.FirstOrDefault(s => s.Id == snapshotId.Value);
            }
            if (activeSnapshot == null && snapshots.Any())
            {
                activeSnapshot = snapshots.First();
            }

            AIRWizardViewModel payload = null;
            if (activeSnapshot != null && !string.IsNullOrWhiteSpace(activeSnapshot.PayloadJson))
            {
                try
                {
                    payload = JsonConvert.DeserializeObject<AIRWizardViewModel>(activeSnapshot.PayloadJson);
                }
                catch { }
            }

            if (payload == null)
            {
                payload = await GetAirInspectionForEditAsync(airId);
            }

            var vm = new AIRSubmissionViewViewModel
            {
                AIRId = air.Id,
                AirNo = air.AIRNo ?? air.CtrlNo ?? "AIR",
                OverallStatus = ResolveOverallStatus(air),
                CanWithdraw = CanWithdrawAir(air),
                CurrentSnapshotId = activeSnapshot != null ? activeSnapshot.Id : Guid.Empty,
                CurrentVersionNo = activeSnapshot != null ? activeSnapshot.VersionNo : 1,
                SubmittedDt = activeSnapshot != null ? activeSnapshot.SubmittedDt : (air.InsertedDt ?? DateTime.Now),
                SubmittedBy = activeSnapshot != null ? activeSnapshot.SubmittedBy : air.InsertedBy,
                ReviewPayload = payload,
                AvailableVersions = snapshots.Select(s => new AIRSnapshotVersionItemViewModel
                {
                    SnapshotId = s.Id,
                    VersionNo = s.VersionNo,
                    SubmittedDt = s.SubmittedDt,
                    SubmittedBy = s.SubmittedBy,
                    IsCurrent = activeSnapshot != null && s.Id == activeSnapshot.Id
                }).ToList()
            };

            return vm;
        }

        public async Task<AIRWizardViewModel> GetSubmittedWizardViewModelAsync(Guid airId, Guid? snapshotId = null)
        {
            var submissionView = await GetSubmissionViewAsync(airId, snapshotId);
            if (submissionView == null || submissionView.ReviewPayload == null) return null;

            var model = submissionView.ReviewPayload;
            model.IsReadOnly = true;
            model.IsSubmittedView = true;
            model.AirId = submissionView.AIRId;
            model.AirNo = submissionView.AirNo;
            model.OverallStatus = submissionView.OverallStatus;
            model.CanWithdraw = submissionView.CanWithdraw;
            model.CurrentSnapshotId = submissionView.CurrentSnapshotId;
            model.CurrentVersionNo = submissionView.CurrentVersionNo;
            model.SubmittedDt = submissionView.SubmittedDt;
            model.SubmittedBy = submissionView.SubmittedBy;
            model.AvailableVersions = submissionView.AvailableVersions;

            return model;
        }
    }
}



