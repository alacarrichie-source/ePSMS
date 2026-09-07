using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Ai.Models;
using iLgs.Models;
using iLgs.Services.AIRs_;

namespace iLgs.Ai.Services.Air
{
    public interface IAirAcceptanceService
    {
        Task<AIRAcceptanceWizardViewModel> GetAcceptanceDetailsAsync(Guid airId);
        Task<AIRAcceptanceWizardViewModel> GetReadOnlyAcceptanceDetailsAsync(Guid airId);
        Task StartAcceptanceAsync(Guid airId, string user);
        Task ReturnForRevisionAsync(Guid airId, string comments, string user);
        Task DeclineWithdrawalAsync(Guid airId, string reason, string user);
        Task PostAcceptanceAsync(AIRAcceptanceWizardViewModel model, string user);
        Task UnpostAcceptanceAsync(Guid airId, string reason, string user);
        Task DeleteAcceptanceAsync(Guid airId, string reason, string user);
    }

    public class AirAcceptanceService : IAirAcceptanceService
    {
        private readonly AppManEntities _db;
        private readonly IDocumentHistoryService _historyService;
        private readonly IAirItemAbstractService _airItemSharedService;

        public AirAcceptanceService(AppManEntities db, IDocumentHistoryService historyService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            _airItemSharedService = new AirItemAbstractService(_db);
        }

        public async Task<AIRAcceptanceWizardViewModel> GetAcceptanceDetailsAsync(Guid airId)
        {
            var air = await _db.AIRs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR not found.");
            if (string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This AIR Acceptance has been deleted and cannot be edited.");
            if (air.IsInspected != true || air.OverallStatus == AirStatuses.Withdrawn || air.PostedDt != null)
                throw new InvalidOperationException("This AIR is not eligible for acceptance editing.");
            if (air.AcceptanceStartedDt == null && !string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Acceptance has not been started for this AIR.");

            return await BuildAcceptanceDetailsAsync(airId, isReadOnlyView: false);
        }

        public async Task<AIRAcceptanceWizardViewModel> GetReadOnlyAcceptanceDetailsAsync(Guid airId)
        {
            var air = await _db.AIRs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR not found.");
            if (air.AcceptanceStartedDt == null && air.AcceptedDate == null && air.PostedDt == null &&
                !string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Acceptance data does not exist for this AIR.");

            return await BuildAcceptanceDetailsAsync(airId, isReadOnlyView: true);
        }

        private async Task<AIRAcceptanceWizardViewModel> BuildAcceptanceDetailsAsync(Guid airId, bool isReadOnlyView)
        {
            var air = await _db.AIRs.AsNoTracking()
                .Include(a => a.Order.OrderItems.Select(oi => oi.ItemCode.ItemType))
                .Include(a => a.Order.OrderItems.Select(oi => oi.OrderSubItems))
                .Include(a => a.Order.OrderItems.Select(oi => oi.OrderItemRequests.Select(oir => oir.RequestItem.Request)))
                .Include(a => a.AIRItems.Select(ai => ai.OrderItemRequest.OrderItem.ItemCode.ItemType))
                .Include(a => a.AIRItems.Select(ai => ai.OrderItemRequest.OrderItem.OrderSubItems))
                .Include(a => a.AIRItems.Select(ai => ai.AIRSubItems))
                .Include(a => a.AIRItems.Select(ai => ai.AIRItemAllocations))
                .Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns))
                .Include(a => a.AIRInvoices)
                .Include(a => a.AIRDocuments)
                .FirstOrDefaultAsync(a => a.Id == airId);

            if (air == null) throw new InvalidOperationException("AIR not found.");

            // Sub-item extensions for items in this AIR
            var allSubItemIds = air.AIRItems.SelectMany(ai => ai.AIRSubItems).Select(s => s.Id).ToList();
            var subExtns = allSubItemIds.Any()
                ? await _db.AIRItemExtns.AsNoTracking().Where(e => e.AIRSubItemId.HasValue && allSubItemIds.Contains(e.AIRSubItemId.Value)).ToListAsync()
                : new List<AIRItemExtn>();

            // Query previous posted AIR items for this Order (cumulative historical totals)
            var orderId = air.OrderId;
            var otherPostedAirItems = await _db.AIRItems.AsNoTracking()
                .Where(ai => ai.AIR.OrderId == orderId && ai.AIR.PostedDt != null && ai.AirId != air.Id)
                .Include(ai => ai.AIRSubItems)
                .Include(ai => ai.AIRItemAllocations)
                .Include(ai => ai.OrderItemRequest)
                .ToListAsync();

            var prevInspectedMap = otherPostedAirItems
                .Where(ai => ai.OrderItemRequest != null)
                .GroupBy(ai => ai.OrderItemRequest.OrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.InspectedQty ?? x.Qty ?? 0));

            var prevAcceptedMap = otherPostedAirItems
                .Where(ai => ai.OrderItemRequest != null)
                .GroupBy(ai => ai.OrderItemRequest.OrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AcceptedQty ?? 0));

            var prevSubInspectedMap = otherPostedAirItems
                .SelectMany(ai => ai.AIRSubItems)
                .GroupBy(s => s.OrderSubItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.InspectedQty));

            var prevSubAcceptedMap = otherPostedAirItems
                .SelectMany(ai => ai.AIRSubItems)
                .GroupBy(s => s.OrderSubItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AcceptedQty));

            var vm = new AIRAcceptanceWizardViewModel
            {
                AirId = air.Id,
                AirNo = air.AIRNo ?? air.CtrlNo,
                CtrlNo = air.CtrlNo,
                PONumber = air.Order != null ? air.Order.PoNo : string.Empty,
                PODate = air.Order != null ? air.Order.PoDate : null,
                SupplierName = air.Order != null ? air.Order.SupName : string.Empty,
                Department = air.Order != null ? air.Order.Department : air.Custodian,
                InspectionDate = air.InspectedDate,
                InspectorName = air.InspectorName ?? air.Officer,
                InspectionLocation = air.InspectionLocation,
                DrNo = air.DrNo,
                InvoiceNo = air.InvoiceNo,
                InvoiceDate = air.InvoiceDate,
                InvoiceAmount = air.InvoiceAmount,
                AcceptanceDate = air.AcceptedDate ?? (isReadOnlyView ? air.AcceptanceStartedDt : DateTime.Today),
                AcceptedBy = air.AcceptedBy ?? (isReadOnlyView ? air.AcceptanceStartedBy : air.Custodian) ?? string.Empty,
                AcceptedByDesignation = air.AcceptedByDesignation ?? string.Empty,
                DepartmentOffice = air.Order != null ? air.Order.Department : string.Empty,
                AcceptanceRemarks = air.AcceptanceRemarks ?? air.Remarks,
                IsReadOnly = isReadOnlyView,
                IsAcceptanceView = isReadOnlyView,
                OverallStatus = air.OverallStatus,
                AcceptanceStartedBy = air.AcceptanceStartedBy,
                AcceptanceStartedDt = air.AcceptanceStartedDt,
                PostedBy = air.PostedBy,
                PostedDt = air.PostedDt,
                HasAcceptanceData = true
            };

            var hasPostedHistory = await _db.DocumentStatusHistories.AsNoTracking().AnyAsync(h =>
                h.DocumentType == DocumentTypes.AcceptanceInspectionReport &&
                h.DocumentId == air.Id &&
                (h.ToStatus == AirStatuses.Posted || h.Action == "Post Acceptance" || h.Action == "AIR_ACCEPTANCE_REPOSTED"));

            bool isPosted = air.PostedDt != null || string.Equals(air.OverallStatus, AirStatuses.Posted, StringComparison.OrdinalIgnoreCase);
            bool isUnposted = string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(air.OverallStatus, AirStatuses.Unposted, StringComparison.OrdinalIgnoreCase);
            bool isDeleted = string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase);

            vm.AcceptanceStatus = isDeleted ? AirAcceptanceStatuses.Deleted : (isUnposted ? AirAcceptanceStatuses.Unposted : (isPosted ? AirAcceptanceStatuses.Accepted : (air.AcceptanceStatus ?? AirAcceptanceStatuses.InProgress)));
            vm.IsUnposted = isUnposted;
            vm.IsDeleted = isDeleted;
            vm.WasPreviouslyPosted = isPosted || hasPostedHistory;
            vm.CanUnpost = isPosted && !isDeleted;
            vm.CanDelete = !isPosted && !isDeleted && (isUnposted || air.AcceptanceStartedDt != null || string.Equals(air.OverallStatus, AirStatuses.AcceptanceInProgress, StringComparison.OrdinalIgnoreCase));

            // Invoices mapping
            if (air.AIRInvoices != null && air.AIRInvoices.Any())
            {
                vm.Invoices = air.AIRInvoices
                    .OrderBy(inv => inv.InsertedDt ?? inv.InvoiceDate)
                    .Select(inv => {
                        var docPrefix = "InvoiceDoc:" + inv.Id.ToString("D");
                        var doc = air.AIRDocuments != null ? air.AIRDocuments.FirstOrDefault(d => d.DocumentType == docPrefix) : null;
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

            // Supporting documents mapping
            if (air.AIRDocuments != null)
            {
                vm.SupportingDocuments = air.AIRDocuments
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

            // Map AIR items
            foreach (var item in air.AIRItems.OrderBy(i => i.OrderItemRequest != null && i.OrderItemRequest.OrderItem != null ? i.OrderItemRequest.OrderItem.ItemNo : string.Empty))
            {
                var oi = item.OrderItemRequest != null ? item.OrderItemRequest.OrderItem : null;
                Guid orderItemId = oi != null ? oi.Id : Guid.Empty;

                decimal poQty = oi != null ? (oi.Qty ?? 0) : (item.Qty ?? 0);
                if (poQty == 0 && item.OrderItemRequest != null && item.OrderItemRequest.QtyApplied.HasValue)
                {
                    poQty = item.OrderItemRequest.QtyApplied.Value;
                }

                decimal prevInspected = orderItemId != Guid.Empty && prevInspectedMap.ContainsKey(orderItemId) ? prevInspectedMap[orderItemId] : 0;
                decimal thisInspected = item.InspectedQty ?? item.Qty ?? 0;
                decimal totalInspected = prevInspected + thisInspected;

                decimal prevAccepted = orderItemId != Guid.Empty && prevAcceptedMap.ContainsKey(orderItemId) ? prevAcceptedMap[orderItemId] : 0;
                decimal available = Math.Max(0, totalInspected - prevAccepted);

                decimal qtyThisAcceptance;
                if (isReadOnlyView)
                {
                    qtyThisAcceptance = item.AcceptedQty ?? 0;
                }
                else if (item.AcceptedQty.HasValue && item.AcceptedQty.Value > 0)
                {
                    qtyThisAcceptance = Math.Min(item.AcceptedQty.Value, available);
                }
                else
                {
                    // Default to available (inspect-now quantity of this AIR bounded by Available)
                    qtyThisAcceptance = Math.Min(thisInspected, available);
                }

                string category = oi?.ItemCode?.ItemType?.Code;
                string itemExtnName = _airItemSharedService.GetItemExtnNameByCategory(category);
                if (string.IsNullOrWhiteSpace(itemExtnName)) itemExtnName = "ItemExtnOther";

                bool isSet = (oi != null && oi.OrderSubItems.Any()) || item.AIRSubItems.Any();
                bool parentRequiresInventory = false;
                if (isSet)
                {
                    string pDesc = (oi?.Description ?? oi?.ItemName ?? item.Remarks ?? "").ToLower();
                    parentRequiresInventory = !(pDesc.Contains("set") || pDesc.Contains("lot") || pDesc.Contains("kit") || pDesc.Contains("package") || pDesc.Contains("desktop") || pDesc.Contains("bundle"));
                }
                else
                {
                    parentRequiresInventory = true;
                }

                var itemVm = new AIRAcceptanceItemViewModel
                {
                    AirItemId = item.Id,
                    OrderItemId = orderItemId,
                    OrderItemRequestId = item.OrderItemRequestId,
                    ItemNo = oi != null ? oi.ItemNo : string.Empty,
                    PRNumber = (item.AIRItemAllocations != null && item.AIRItemAllocations.Any()) ? item.AIRItemAllocations.First().PRNumber : (air.Order != null ? air.Order.PrNo : string.Empty),
                    Department = (item.AIRItemAllocations != null && item.AIRItemAllocations.Any()) ? item.AIRItemAllocations.First().Department : (air.Order != null ? air.Order.Department : string.Empty),
                    PPMPCode = oi != null ? oi.PpmpCode : string.Empty,
                    Description = oi != null ? (oi.Description ?? oi.ItemName) : "Item",
                    Unit = oi != null ? oi.Unit : string.Empty,
                    OrderedQty = poQty,
                    POQty = poQty,
                    TotalInspectedQty = totalInspected,
                    PreviouslyAcceptedQty = prevAccepted,
                    QtyThisAcceptance = qtyThisAcceptance,
                    AcceptedQty = qtyThisAcceptance,
                    InspectedQty = thisInspected,
                    Disposition = !string.IsNullOrEmpty(item.Disposition)
                        ? item.Disposition
                        : (item.InvDist == "D" ? AirDispositions.ForDistribution : AirDispositions.Inventory),
                    DestinationDepartment = item.DestinationDepartment,
                    DestinationCustodian = item.DestinationCustodian,
                    Remarks = item.Remarks,
                    IsSetLot = isSet,
                    ItemExtnName = itemExtnName,
                    CategoryCode = category,
                    ParentRequiresInventory = parentRequiresInventory
                };

                // Allocations mapping
                if (item.AIRItemAllocations != null && item.AIRItemAllocations.Any())
                {
                    itemVm.Allocations = item.AIRItemAllocations.Select(a => new AIRItemAllocationViewModel
                    {
                        Id = a.Id,
                        OrderItemRequestId = a.OrderItemRequestId,
                        QtyAllocated = a.QtyAllocated,
                        QtyInspected = a.QtyInspected,
                        PreviousInspected = 0,
                        PRNumber = a.PRNumber,
                        Department = a.Department,
                        Remarks = a.Remarks
                    }).ToList();
                }

                // Inventory details mapping (parent)
                if (item.AIRItemExtns != null && item.AIRItemExtns.Any(e => e.AIRItemId == item.Id))
                {
                    var orderedExtns = item.AIRItemExtns.Where(e => e.AIRItemId == item.Id).OrderBy(e => e.ContentNo).ToList();
                    foreach (var extn in orderedExtns)
                    {
                        var detail = new AIRItemInventoryDetailViewModel
                        {
                            Id = extn.Id,
                            AirItemId = extn.AIRItemId,
                            ContentNo = extn.ContentNo ?? (itemVm.InventoryDetails.Count + 1),
                            TContentNo = extn.TContentNo,
                            SetLotNo = extn.SetLotNo,
                            SetLotQtyNo = extn.SetLotQtyNo,
                            OrderItemRequestId = item.OrderItemRequestId ?? Guid.Empty,
                            PRNumber = itemVm.PRNumber,
                            DepartmentName = itemVm.Department
                        };
                        PopulateDetailFromExtension(detail, extn);
                        detail.IsCompleted = IsDetailCompleted(detail, itemExtnName);
                        itemVm.InventoryDetails.Add(detail);
                    }
                }

                // Sub-items mapping (Set / Lot)
                if (isSet)
                {
                    var dbSubItems = item.AIRSubItems.OrderBy(s => s.SubItemNo).ToList();
                    foreach (var sub in dbSubItems)
                    {
                        decimal subExpected = sub.ExpectedQty;
                        decimal prevSubInsp = prevSubInspectedMap.ContainsKey(sub.OrderSubItemId) ? prevSubInspectedMap[sub.OrderSubItemId] : 0;
                        decimal totalSubInsp = prevSubInsp + sub.InspectedQty;
                        decimal prevSubAcc = prevSubAcceptedMap.ContainsKey(sub.OrderSubItemId) ? prevSubAcceptedMap[sub.OrderSubItemId] : 0;
                        decimal subAvail = Math.Max(0, totalSubInsp - prevSubAcc);

                        decimal subQtyThisAcc;
                        if (isReadOnlyView)
                        {
                            subQtyThisAcc = sub.AcceptedQty;
                        }
                        else if (sub.AcceptedQty > 0)
                        {
                            subQtyThisAcc = Math.Min(sub.AcceptedQty, subAvail);
                        }
                        else
                        {
                            decimal subRatio = (sub.QtyPerParent > 0) ? sub.QtyPerParent.Value : 1;
                            subQtyThisAcc = Math.Min(subAvail, qtyThisAcceptance * subRatio);
                        }

                        string subCategory = sub.CategoryCode;
                        string subExtnName = !string.IsNullOrWhiteSpace(sub.ItemExtnName)
                            ? sub.ItemExtnName
                            : _airItemSharedService.GetItemExtnNameByCategory(subCategory) ?? "ItemExtnOther";

                        var subVm = new AIRAcceptanceSubItemViewModel
                        {
                            AirSubItemId = sub.Id,
                            OrderSubItemId = sub.OrderSubItemId,
                            OrderSubItemRequestId = sub.OrderSubItemRequestId,
                            SubItemNo = sub.SubItemNo,
                            Description = sub.Description,
                            Unit = sub.Unit,
                            ExpectedQty = subExpected,
                            POQty = subExpected,
                            TotalInspectedQty = totalSubInsp,
                            PreviouslyAcceptedQty = prevSubAcc,
                            QtyThisAcceptance = subQtyThisAcc,
                            Remarks = sub.Remarks,
                            QtyPerParent = sub.QtyPerParent ?? 1,
                            CategoryCode = subCategory,
                            ItemExtnName = subExtnName,
                            RequiresInventory = true
                        };

                        // Map sub-item inventory details
                        var thisSubExtns = subExtns.Where(e => e.AIRSubItemId == sub.Id).OrderBy(e => e.ContentNo).ToList();
                        foreach (var se in thisSubExtns)
                        {
                            var sDetail = new AIRItemInventoryDetailViewModel
                            {
                                Id = se.Id,
                                AirItemId = se.AIRItemId,
                                AirSubItemId = se.AIRSubItemId,
                                ContentNo = se.ContentNo ?? (subVm.InventoryDetails.Count + 1),
                                TContentNo = se.TContentNo,
                                SetLotNo = se.SetLotNo,
                                SetLotQtyNo = se.SetLotQtyNo,
                                OrderItemRequestId = item.OrderItemRequestId ?? Guid.Empty,
                                PRNumber = itemVm.PRNumber,
                                DepartmentName = itemVm.Department
                            };
                            PopulateDetailFromExtension(sDetail, se);
                            sDetail.IsCompleted = IsDetailCompleted(sDetail, subExtnName);
                            subVm.InventoryDetails.Add(sDetail);
                        }

                        itemVm.SubItems.Add(subVm);
                    }
                }

                vm.Items.Add(itemVm);
            }

            // Determine Completion status
            bool isComplete = vm.Items.Any() && vm.Items.All(i =>
                i.TotalAcceptedQty >= i.POQty &&
                (!i.IsSetLot || i.SubItems.All(s => s.TotalAcceptedQty >= s.ExpectedQty)));

            bool isPartial = !isComplete && vm.Items.Any(i => i.TotalAcceptedQty > 0);

            vm.IsComplete = isComplete;
            vm.IsPartial = isPartial;

            return vm;
        }

        public async Task StartAcceptanceAsync(Guid airId, string user)
        {
            var air = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR record not found.");

            if (air.PostedDt != null)
                throw new InvalidOperationException("AIR is already posted.");

            if (air.IsInspected != true || air.OverallStatus != AirStatuses.SubmittedForAcceptance)
                throw new InvalidOperationException("AIR has not been submitted for acceptance or is no longer eligible.");
            if (air.AcceptanceStartedDt != null)
                throw new InvalidOperationException("Acceptance has already been started for this AIR.");

            air.AcceptanceStartedDt = DateTime.Now;
            air.AcceptanceStartedBy = user;
            air.OverallStatus = AirStatuses.AcceptanceInProgress;
            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                AirStatuses.SubmittedForAcceptance,
                AirStatuses.AcceptanceInProgress,
                "Start Acceptance",
                "Acceptance process started by Acceptance Officer.",
                user
            );

            await _db.SaveChangesAsync();
        }

        public async Task ReturnForRevisionAsync(Guid airId, string comments, string user)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Revision comments are required when returning an AIR.");

            var air = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR record not found.");

            if (air.PostedDt != null)
                throw new InvalidOperationException("Cannot return a posted AIR.");
            if (air.IsInspected != true || air.AcceptanceStartedDt == null || air.OverallStatus != AirStatuses.AcceptanceInProgress)
                throw new InvalidOperationException("Only an AIR with acceptance in progress can be returned for revision.");

            air.IsInspected = false;
            air.AcceptanceStartedDt = null;
            air.AcceptanceStartedBy = null;

            air.RevisionComments = comments;
            air.OverallStatus = AirStatuses.ReturnedForRevision;
            air.ReturnedBy = user;
            air.ReturnedDt = DateTime.Now;

            air.WithdrawalRequested = false;
            air.WithdrawalRequestedBy = null;
            air.WithdrawalRequestedDt = null;

            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                AirStatuses.AcceptanceInProgress,
                AirStatuses.ReturnedForRevision,
                "Return for Revision",
                comments,
                user
            );

            await _db.SaveChangesAsync();
        }

        public async Task DeclineWithdrawalAsync(Guid airId, string reason, string user)
        {
            var air = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR record not found.");

            air.WithdrawalRequested = false;
            air.WithdrawalRequestedBy = null;
            air.WithdrawalRequestedDt = null;
            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                AirStatuses.AcceptanceInProgress,
                AirStatuses.AcceptanceInProgress,
                "Decline Withdrawal Request",
                string.IsNullOrWhiteSpace(reason) ? "Withdrawal request declined." : reason,
                user
            );

            await _db.SaveChangesAsync();
        }

        public async Task PostAcceptanceAsync(AIRAcceptanceWizardViewModel model, string user)
        {
            var air = await _db.AIRs
                .Include(a => a.Order.OrderItems.Select(oi => oi.OrderSubItems))
                .Include(a => a.AIRItems.Select(i => i.AIRSubItems))
                .Include(a => a.AIRDocuments)
                .FirstOrDefaultAsync(a => a.Id == model.AirId);

            if (air == null) throw new InvalidOperationException("AIR record not found.");
            if (air.PostedDt != null && string.Equals(air.OverallStatus, AirStatuses.Posted, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("AIR is already posted.");
            if (string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This AIR Acceptance has been deleted and cannot be posted.");

            bool isEligibleToPost = (string.Equals(air.OverallStatus, AirStatuses.AcceptanceInProgress, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(air.OverallStatus, AirStatuses.Unposted, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.InProgress, StringComparison.OrdinalIgnoreCase)) &&
                                    air.IsInspected == true;

            if (!isEligibleToPost)
                throw new InvalidOperationException("Only an AIR with acceptance in progress or unposted can be posted.");

            // Query other posted AIR items for cumulative recalculation and validation
            var otherPostedAirItems = await _db.AIRItems.AsNoTracking()
                .Where(ai => ai.AIR.OrderId == air.OrderId && ai.AIR.PostedDt != null && ai.AirId != air.Id)
                .Include(ai => ai.AIRSubItems)
                .Include(ai => ai.OrderItemRequest)
                .ToListAsync();

            var prevInspectedMap = otherPostedAirItems
                .Where(ai => ai.OrderItemRequest != null)
                .GroupBy(ai => ai.OrderItemRequest.OrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.InspectedQty ?? x.Qty ?? 0));

            var prevAcceptedMap = otherPostedAirItems
                .Where(ai => ai.OrderItemRequest != null)
                .GroupBy(ai => ai.OrderItemRequest.OrderItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AcceptedQty ?? 0));

            var prevSubInspectedMap = otherPostedAirItems
                .SelectMany(ai => ai.AIRSubItems)
                .GroupBy(s => s.OrderSubItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.InspectedQty));

            var prevSubAcceptedMap = otherPostedAirItems
                .SelectMany(ai => ai.AIRSubItems)
                .GroupBy(s => s.OrderSubItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AcceptedQty));

            // Server-side validations and persistence
            foreach (var itemVm in model.Items)
            {
                var dbItem = air.AIRItems.FirstOrDefault(i => i.Id == itemVm.AirItemId);
                if (dbItem == null) continue;

                var oi = air.Order != null && air.Order.OrderItems != null
                    ? air.Order.OrderItems.FirstOrDefault(o => o.Id == (dbItem.OrderItemRequest != null ? dbItem.OrderItemRequest.OrderItemId : itemVm.OrderItemId))
                    : null;

                decimal poQty = oi != null ? (oi.Qty ?? 0) : (dbItem.Qty ?? 0);
                decimal prevInsp = oi != null && prevInspectedMap.ContainsKey(oi.Id) ? prevInspectedMap[oi.Id] : 0;
                decimal totalInsp = prevInsp + (dbItem.InspectedQty ?? dbItem.Qty ?? 0);
                decimal prevAcc = oi != null && prevAcceptedMap.ContainsKey(oi.Id) ? prevAcceptedMap[oi.Id] : 0;
                decimal available = Math.Max(0, totalInsp - prevAcc);

                // Validation 1: Cannot be negative
                if (itemVm.QtyThisAcceptance < 0)
                    throw new InvalidOperationException($"Accepted quantity for Item #{itemVm.ItemNo} cannot be negative.");

                // Validation 2: Cannot exceed AvailableForAcceptance
                if (itemVm.QtyThisAcceptance > available)
                    throw new InvalidOperationException($"Accepted quantity ({itemVm.QtyThisAcceptance}) for Item #{itemVm.ItemNo} cannot exceed available inspected quantity ({available}).");

                // Validation 3: Total accepted cannot exceed PO Qty
                if (prevAcc + itemVm.QtyThisAcceptance > poQty)
                    throw new InvalidOperationException($"Total accepted quantity for Item #{itemVm.ItemNo} cannot exceed PO quantity ({poQty}).");

                dbItem.AcceptedQty = itemVm.QtyThisAcceptance;
                dbItem.Qty = itemVm.QtyThisAcceptance;
                dbItem.Disposition = itemVm.Disposition;
                dbItem.InvDist = itemVm.Disposition == AirDispositions.ForDistribution ? "D" : "I";
                dbItem.DestinationDepartment = itemVm.DestinationDepartment;
                dbItem.DestinationCustodian = itemVm.DestinationCustodian;
                dbItem.Remarks = itemVm.Remarks;
                dbItem.UpdatedBy = user;
                dbItem.UpdatedDt = DateTime.Now;

                // Sub-items validation and persistence
                if (itemVm.SubItems != null && itemVm.SubItems.Any())
                {
                    foreach (var subVm in itemVm.SubItems)
                    {
                        var dbSub = dbItem.AIRSubItems.FirstOrDefault(s => s.Id == subVm.AirSubItemId || s.OrderSubItemId == subVm.OrderSubItemId);
                        if (dbSub == null) continue;

                        decimal prevSubInsp = prevSubInspectedMap.ContainsKey(dbSub.OrderSubItemId) ? prevSubInspectedMap[dbSub.OrderSubItemId] : 0;
                        decimal totalSubInsp = prevSubInsp + dbSub.InspectedQty;
                        decimal prevSubAcc = prevSubAcceptedMap.ContainsKey(dbSub.OrderSubItemId) ? prevSubAcceptedMap[dbSub.OrderSubItemId] : 0;
                        decimal subAvail = Math.Max(0, totalSubInsp - prevSubAcc);

                        if (subVm.QtyThisAcceptance < 0)
                            throw new InvalidOperationException($"Accepted quantity for sub-item {subVm.SubItemNo} cannot be negative.");
                        if (subVm.QtyThisAcceptance > subAvail)
                            throw new InvalidOperationException($"Accepted quantity ({subVm.QtyThisAcceptance}) for sub-item {subVm.SubItemNo} cannot exceed available inspected quantity ({subAvail}).");

                        dbSub.AcceptedQty = subVm.QtyThisAcceptance;
                        dbSub.Remarks = subVm.Remarks;
                        dbSub.UpdatedBy = user;
                        dbSub.UpdatedDt = DateTime.Now;
                    }

                    // Set/Lot consistency validation:
                    // Ensure parent accepted set quantity does not exceed what components can satisfy
                    foreach (var subVm in itemVm.SubItems)
                    {
                        decimal subRatio = (subVm.QtyPerParent > 0) ? subVm.QtyPerParent : 1;
                        decimal requiredComponentQty = itemVm.QtyThisAcceptance * subRatio;
                        if (subVm.QtyThisAcceptance < requiredComponentQty)
                        {
                            throw new InvalidOperationException($"Parent Set #{itemVm.ItemNo} accepts {itemVm.QtyThisAcceptance} set(s), which requires {requiredComponentQty} unit(s) of component '{subVm.Description}', but only {subVm.QtyThisAcceptance} were accepted.");
                        }
                    }
                }
            }

            // Determine Completion status
            bool isComplete = air.AIRItems.Any() && air.AIRItems.All(i =>
            {
                var oi = air.Order != null && air.Order.OrderItems != null
                    ? air.Order.OrderItems.FirstOrDefault(o => o.Id == (i.OrderItemRequest != null ? i.OrderItemRequest.OrderItemId : Guid.Empty))
                    : null;
                decimal poQty = oi != null ? (oi.Qty ?? 0) : (i.Qty ?? 0);
                decimal prevAcc = oi != null && prevAcceptedMap.ContainsKey(oi.Id) ? prevAcceptedMap[oi.Id] : 0;
                decimal totalAcc = prevAcc + (i.AcceptedQty ?? 0);
                bool parentComplete = totalAcc >= poQty;

                if (i.AIRSubItems != null && i.AIRSubItems.Any())
                {
                    bool subComplete = i.AIRSubItems.All(s =>
                    {
                        decimal prevSubAcc = prevSubAcceptedMap.ContainsKey(s.OrderSubItemId) ? prevSubAcceptedMap[s.OrderSubItemId] : 0;
                        return (prevSubAcc + s.AcceptedQty) >= s.ExpectedQty;
                    });
                    return parentComplete && subComplete;
                }
                return parentComplete;
            });

            bool isPartial = !isComplete && air.AIRItems.Any(i => (i.AcceptedQty ?? 0) > 0);

            air.IsComplete = isComplete;
            air.IsPartial = isPartial;
            air.AcceptedDate = model.AcceptanceDate ?? DateTime.Today;
            air.AcceptedBy = model.AcceptedBy;
            air.AcceptedByDesignation = model.AcceptedByDesignation;
            air.AcceptanceRemarks = model.AcceptanceRemarks;
            air.Custodian = model.AcceptedBy;
            air.PostedBy = user;
            air.PostedDt = DateTime.Now;
            bool wasEverPosted = await _db.DocumentStatusHistories.AsNoTracking().AnyAsync(h =>
                h.DocumentType == DocumentTypes.AcceptanceInspectionReport &&
                h.DocumentId == air.Id &&
                (h.ToStatus == AirStatuses.Posted || h.Action == "Post Acceptance" || h.Action == "AIR_ACCEPTANCE_REPOSTED" || h.Action == "AIR_ACCEPTANCE_UNPOSTED"));

            bool isRepost = wasEverPosted || string.Equals(air.OverallStatus, AirStatuses.Unposted, StringComparison.OrdinalIgnoreCase);

            var prevStatus = air.OverallStatus ?? (isRepost ? AirStatuses.Unposted : AirStatuses.AcceptanceInProgress);

            air.OverallStatus = AirStatuses.Posted;
            air.AcceptanceStatus = AirAcceptanceStatuses.Accepted;
            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            // Persist Supporting Documents
            if (model.SupportingDocuments != null)
            {
                var existingDocs = air.AIRDocuments != null
                    ? air.AIRDocuments.Where(d => !d.DocumentType.StartsWith("InvoiceDoc:")).ToList()
                    : new List<AIRDocument>();
                var submittedPaths = new HashSet<string>(model.SupportingDocuments.Select(d => d.FilePath).Where(p => !string.IsNullOrEmpty(p)), StringComparer.OrdinalIgnoreCase);

                foreach (var gDoc in model.SupportingDocuments)
                {
                    if (string.IsNullOrWhiteSpace(gDoc.FilePath)) continue;
                    var existingGDoc = existingDocs.FirstOrDefault(d => string.Equals(d.FilePath, gDoc.FilePath, StringComparison.OrdinalIgnoreCase));
                    if (existingGDoc == null)
                    {
                        var newDoc = new AIRDocument
                        {
                            Id = gDoc.Id != Guid.Empty ? gDoc.Id : Guid.NewGuid(),
                            AirId = air.Id,
                            DocumentType = !string.IsNullOrWhiteSpace(gDoc.DocumentType) ? gDoc.DocumentType : "AcceptanceDoc",
                            FileName = gDoc.FileName,
                            FilePath = gDoc.FilePath,
                            FileSize = gDoc.FileSize,
                            UploadedBy = user,
                            UploadedDt = DateTime.Now
                        };
                        _db.AIRDocuments.Add(newDoc);
                    }
                }

                // Remove deleted documents (only of type AcceptanceDoc or general supporting doc if user removed it before submitting)
                foreach (var egDoc in existingDocs.Where(d => d.DocumentType == "AcceptanceDoc" || d.DocumentType == "SignedAIR"))
                {
                    if (!submittedPaths.Contains(egDoc.FilePath))
                    {
                        _db.AIRDocuments.Remove(egDoc);
                    }
                }
            }

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                prevStatus,
                AirStatuses.Posted,
                isRepost ? "AIR_ACCEPTANCE_REPOSTED" : "Post Acceptance",
                isRepost
                    ? string.Format("AIR acceptance reposted ({0}) after revisions.", isComplete ? "Complete" : "Partial")
                    : string.Format("AIR officially accepted ({0}), obligation posted, and ready for property recording.", isComplete ? "Complete" : "Partial"),
                user
            );

            await _db.SaveChangesAsync();
        }

        private async Task ValidateDownstreamDependenciesAsync(AIR air)
        {
            var airNo = air.AIRNo;
            var ctrlNo = air.CtrlNo;
            var airItemIds = air.AIRItems.Select(i => i.Id).ToList();

            // 1. PsCardItems (Stock Card / Property Card entries)
            bool hasPsCardItems = await _db.PsCardItems.AsNoTracking().AnyAsync(p =>
                (!string.IsNullOrEmpty(airNo) && p.AirNo == airNo) ||
                (!string.IsNullOrEmpty(ctrlNo) && p.AirNo == ctrlNo));
            if (hasPsCardItems)
            {
                throw new InvalidOperationException("This AIR Acceptance cannot be unposted because downstream inventory/accountability transactions (Stock/Property Card entries) already exist. Reverse or remove the dependent transactions first.");
            }

            // 2. PsCardItemExtns (Extensions linked to AIRItemExtns)
            bool hasPsCardItemExtns = await _db.AIRItemExtns.AsNoTracking().AnyAsync(e => 
                e.AIRItemId.HasValue && airItemIds.Contains(e.AIRItemId.Value) && e.PsCardItemExtns.Any());
            if (hasPsCardItemExtns)
            {
                throw new InvalidOperationException("This AIR Acceptance cannot be unposted because downstream inventory/accountability transactions (Property Card Extension items) already exist. Reverse or remove the dependent transactions first.");
            }

            // 3. CustodianReportItems
            bool hasCustodianReports = await _db.CustodianReportItems.AsNoTracking().AnyAsync(c =>
                (!string.IsNullOrEmpty(airNo) && c.AirNo == airNo) ||
                (!string.IsNullOrEmpty(ctrlNo) && c.AirNo == ctrlNo));
            if (hasCustodianReports)
            {
                throw new InvalidOperationException("This AIR Acceptance cannot be unposted because downstream inventory/accountability transactions (Custodian Report entries) already exist. Reverse or remove the dependent transactions first.");
            }

            // 4. RPCI Items (Report on Physical Count of Inventories)
            bool hasRpci = await _db.RPCIItems.AsNoTracking().AnyAsync(r =>
                (!string.IsNullOrEmpty(airNo) && r.AirNo == airNo) ||
                (!string.IsNullOrEmpty(ctrlNo) && r.AirNo == ctrlNo));
            if (hasRpci)
            {
                throw new InvalidOperationException("This AIR Acceptance cannot be unposted because downstream inventory/accountability transactions (RPCI inventory items) already exist. Reverse or remove the dependent transactions first.");
            }

            // 5. RpcPpe Items (Report on Physical Count of PPE)
            bool hasRpcPpe = await _db.RpcPpeItems.AsNoTracking().AnyAsync(r =>
                (!string.IsNullOrEmpty(airNo) && r.AirNo == airNo) ||
                (!string.IsNullOrEmpty(ctrlNo) && r.AirNo == ctrlNo));
            if (hasRpcPpe)
            {
                throw new InvalidOperationException("This AIR Acceptance cannot be unposted because downstream inventory/accountability transactions (RPCPPE property items) already exist. Reverse or remove the dependent transactions first.");
            }
        }

        public async Task UnpostAcceptanceAsync(Guid airId, string reason, string user)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Reason for unposting is required.");

            using (var tx = _db.Database.BeginTransaction())
            {
                var air = await _db.AIRs
                    .Include(a => a.AIRItems.Select(i => i.AIRItemExtns))
                    .FirstOrDefaultAsync(a => a.Id == airId);

                if (air == null) throw new InvalidOperationException("AIR record not found.");

                // Concurrency & state validation
                if (string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This AIR Acceptance has been deleted and cannot be unposted.");

                if (air.PostedDt == null || !string.Equals(air.OverallStatus, AirStatuses.Posted, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only a Posted Acceptance can be unposted. Current status: " + (air.OverallStatus ?? "Unknown") + ".");

                // Validate downstream dependencies
                await ValidateDownstreamDependenciesAsync(air);

                var prevStatus = air.OverallStatus;
                air.OverallStatus = AirStatuses.Unposted;
                air.AcceptanceStatus = AirAcceptanceStatuses.Unposted;
                air.PostedDt = null;
                air.PostedBy = null;
                air.UpdatedBy = user;
                air.UpdatedDt = DateTime.Now;

                _historyService.AddStatusHistory(
                    DocumentTypes.AcceptanceInspectionReport,
                    air.Id,
                    air.AIRNo ?? air.CtrlNo,
                    prevStatus,
                    AirStatuses.Unposted,
                    "AIR_ACCEPTANCE_UNPOSTED",
                    reason.Trim(),
                    user
                );

                await _db.SaveChangesAsync();
                tx.Commit();
            }
        }

        public async Task DeleteAcceptanceAsync(Guid airId, string reason, string user)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Reason for deletion is required.");

            using (var tx = _db.Database.BeginTransaction())
            {
                var air = await _db.AIRs
                    .Include(a => a.AIRItems.Select(i => i.AIRSubItems))
                    .Include(a => a.AIRDocuments)
                    .FirstOrDefaultAsync(a => a.Id == airId);

                if (air == null) throw new InvalidOperationException("AIR record not found.");

                // Concurrency & state validation
                if (string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This AIR Acceptance is already deleted.");

                if (air.PostedDt != null || string.Equals(air.OverallStatus, AirStatuses.Posted, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("A Posted Acceptance cannot be deleted directly. Unpost it first.");

                bool isUnposted = string.Equals(air.AcceptanceStatus, AirAcceptanceStatuses.Unposted, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(air.OverallStatus, AirStatuses.Unposted, StringComparison.OrdinalIgnoreCase);
                bool isInProgress = string.Equals(air.OverallStatus, AirStatuses.AcceptanceInProgress, StringComparison.OrdinalIgnoreCase) ||
                                    air.AcceptanceStartedDt != null;

                if (!isUnposted && !isInProgress)
                    throw new InvalidOperationException("Only an Unposted or Draft Acceptance can be deleted.");

                // Check whether it was ever posted
                bool wasEverPosted = await _db.DocumentStatusHistories.AsNoTracking().AnyAsync(h =>
                    h.DocumentType == DocumentTypes.AcceptanceInspectionReport &&
                    h.DocumentId == air.Id &&
                    (h.ToStatus == AirStatuses.Posted || h.Action == "Post Acceptance" || h.Action == "AIR_ACCEPTANCE_REPOSTED" || h.Action == "AIR_ACCEPTANCE_UNPOSTED"));

                var prevStatus = isUnposted ? AirStatuses.Unposted : (air.OverallStatus ?? AirStatuses.AcceptanceInProgress);

                // Logical deletion of Acceptance portion ONLY - Keep Inspection intact!
                air.AcceptanceStatus = AirAcceptanceStatuses.Deleted;
                air.AcceptanceStartedDt = null;
                air.AcceptanceStartedBy = null;
                air.AcceptedDate = null;
                air.AcceptedBy = null;
                air.AcceptedByDesignation = null;
                air.AcceptanceRemarks = null;
                air.IsComplete = false;
                air.IsPartial = false;

                // Deactivate/reset Acceptance quantities on items so they cannot be treated as active accepted data
                foreach (var item in air.AIRItems)
                {
                    item.AcceptedQty = 0;
                    item.Disposition = null;
                    item.InvDist = null;
                    item.DestinationDepartment = null;
                    item.DestinationCustodian = null;

                    if (item.AIRSubItems != null)
                    {
                        foreach (var sub in item.AIRSubItems)
                        {
                            sub.AcceptedQty = 0;
                        }
                    }
                }

                if (!wasEverPosted)
                {
                    // If Draft (never posted), Inspection returns to SubmittedForAcceptance
                    air.OverallStatus = AirStatuses.SubmittedForAcceptance;
                }
                else
                {
                    // If previously posted and then unposted, keep status Cancelled for overall while AcceptanceStatus is Deleted
                    air.OverallStatus = AirStatuses.Cancelled;
                }

                air.UpdatedBy = user;
                air.UpdatedDt = DateTime.Now;

                _historyService.AddStatusHistory(
                    DocumentTypes.AcceptanceInspectionReport,
                    air.Id,
                    air.AIRNo ?? air.CtrlNo,
                    prevStatus,
                    AirAcceptanceStatuses.Deleted,
                    "AIR_ACCEPTANCE_DELETED",
                    reason.Trim(),
                    user
                );

                await _db.SaveChangesAsync();
                tx.Commit();
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
    }
}


