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
    public interface IAirInspectionService
    {
        Task<List<AIRGridItemViewModel>> GetAirListAsync(string statusFilter);
        Task<List<AIRWizardDraftListItemViewModel>> GetMyDraftsAsync(string user);
        Task<List<POInspectionCandidateViewModel>> GetEligiblePurchaseOrdersAsync();
        Task<AIRWizardViewModel> GetPOInspectionDetailsAsync(Guid orderId, Guid? airId = null);
        Task<AIRWizardViewModel> GetAirInspectionForEditAsync(Guid airId);
        Task<Guid> SaveInspectionDraftAsync(AIRWizardViewModel model, string user);
        Task<Guid> SubmitForAcceptanceAsync(AIRWizardViewModel model, string user);
        Task WithdrawSubmissionAsync(Guid airId, string reason, string user);
        Task RequestWithdrawalAsync(Guid airId, string reason, string user);
        Task<AIRItemHistoryViewModel> GetItemInspectionHistoryAsync(Guid orderItemId);
        Task DiscardDraftAsync(Guid draftId, string user);
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

        public async Task<List<AIRGridItemViewModel>> GetAirListAsync(string statusFilter)
        {
            var query = _db.AIRs.AsNoTracking().Include(a => a.Order).Include(a => a.AIRItems);

            var list = await query.OrderByDescending(a => a.InsertedDt ?? a.AIRDate).ToListAsync();
            var result = new List<AIRGridItemViewModel>();

            foreach (var a in list)
            {
                var overallStatus = ResolveOverallStatus(a);

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
                    AcceptanceStatus = a.PostedDt != null ? AirAcceptanceStatuses.Accepted : (a.AcceptedDate != null ? AirAcceptanceStatuses.Accepted : (a.AcceptanceStartedDt != null ? AirAcceptanceStatuses.InProgress : AirAcceptanceStatuses.Pending)),
                    RevisionComments = a.RevisionComments,
                    WithdrawalReason = a.WithdrawalReason,
                    WithdrawalRequested = a.WithdrawalRequested == true,

                    CanContinue = a.IsInspected != true && a.PostedDt == null,
                    CanWithdraw = a.IsInspected == true && a.AcceptanceStartedDt == null && a.PostedDt == null,
                    CanRequestWithdrawal = a.IsInspected == true && a.AcceptanceStartedDt != null && a.PostedDt == null,
                    CanRevise = !string.IsNullOrWhiteSpace(a.RevisionComments) && a.PostedDt == null,
                    CanStartAcceptance = a.IsInspected == true && a.AcceptanceStartedDt == null && a.PostedDt == null,
                    CanContinueAcceptance = a.AcceptanceStartedDt != null && a.PostedDt == null,
                    CanPost = a.AcceptedDate != null && a.PostedDt == null,
                    CanPrint = a.PostedDt != null
                };

                result.Add(vm);
            }

            return result;
        }

        public async Task<List<AIRWizardDraftListItemViewModel>> GetMyDraftsAsync(string user)
        {
            return await _db.AIRs.AsNoTracking()
                .Where(a => a.IsInspected != true && (a.InsertedBy == user || a.UpdatedBy == user))
                .OrderByDescending(a => a.UpdatedDt ?? a.InsertedDt)
                .Select(a => new AIRWizardDraftListItemViewModel
                {
                    Id = a.Id,
                    DraftNo = a.CtrlNo ?? a.AIRNo,
                    PONumber = a.Order != null ? a.Order.PoNo : "N/A",
                    SupplierName = a.Order != null ? a.Order.SupName : "N/A",
                    CurrentStep = 1,
                    CreatedAt = a.InsertedDt ?? DateTime.Today,
                    LastUpdatedAt = a.UpdatedDt ?? a.InsertedDt ?? DateTime.Today
                })
                .ToListAsync();
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
                InspectionDate = DateTime.Today,
                InspectionLocation = order.DeliveryPlace ?? "GSO Warehouse"
            };

            AIR existingAir = null;
            if (airId.HasValue)
            {
                existingAir = await _db.AIRs
                    .Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns))
                    .FirstOrDefaultAsync(a => a.Id == airId.Value);

                if (existingAir != null)
                {
                    vm.AirId = existingAir.Id;
                    vm.AirNo = existingAir.AIRNo;
                    vm.CtrlNo = existingAir.CtrlNo;
                    vm.AirDate = existingAir.AIRDate ?? DateTime.Today;
                    vm.InspectionDate = existingAir.InspectedDate ?? DateTime.Today;
                    vm.InspectorName = existingAir.InspectorName ?? existingAir.Officer;
                    vm.InspectorDesignation = existingAir.InspectorDesignation;
                    vm.InspectionCommittee = existingAir.InspectionCommittee;
                    vm.InspectionLocation = existingAir.InspectionLocation ?? order.DeliveryPlace ?? "GSO Warehouse";
                    vm.GeneralRemarks = existingAir.Remarks;
                    vm.RevisionComments = existingAir.RevisionComments;
                    vm.Invoice.InvoiceNo = existingAir.InvoiceNo;
                    vm.Invoice.InvoiceDate = existingAir.InvoiceDate ?? DateTime.Today;
                    vm.Invoice.DrNo = existingAir.DrNo;
                    vm.Invoice.InvoiceAmount = existingAir.InvoiceAmount;
                    vm.Invoice.InvoiceType = existingAir.InvoiceType;
                    vm.Invoice.BillingReference = existingAir.BillingReference;
                    vm.Disposition = existingAir.Disposition ?? (existingAir.InvDist == "D" ? "For Distribution" : (existingAir.InvDist == "I" ? "Inventory" : existingAir.InvDist));
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

                // Calculate Previously Inspected from posted AIRs only (excluding current draft)
                var postedQty = await _db.AIRItems
                    .Where(ai => ai.AIR.PostedDt != null &&
                                 (airId == null || ai.AirId != airId.Value) &&
                                 ((ai.OrderItemRequestId.HasValue && oirIds.Contains(ai.OrderItemRequestId.Value)) ||
                                  (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == oi.Id)))
                    .SumAsync(ai => (decimal?)ai.Qty) ?? 0;

                decimal currentInspectNow = 0;
                string itemRemarks = string.Empty;
                AIRItem existingItem = null;

                if (existingAir != null)
                {
                    existingItem = existingAir.AIRItems.FirstOrDefault(ai => 
                        (ai.OrderItemRequestId.HasValue && oirIds.Contains(ai.OrderItemRequestId.Value)) ||
                        (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == oi.Id));

                    if (existingItem != null)
                    {
                        currentInspectNow = existingItem.Qty ?? 0;
                        itemRemarks = existingItem.Remarks;
                    }
                }

                // Determine extension type dynamically using _airItemSharedService
                string category = oi.ItemCode?.ItemType?.Code;
                string itemExtnName = _airItemSharedService.GetItemExtnNameByCategory(category);
                if (string.IsNullOrWhiteSpace(itemExtnName))
                {
                    itemExtnName = "ItemExtnOther";
                }

                var lineItem = new AIRLineItemViewModel
                {
                    OrderItemId = oi.Id,
                    OrderItemRequestId = oirId,
                    ItemNo = oi.ItemNo,
                    PRNumber = prNumber,
                    Department = department,
                    Sources = prSources,
                    PPMPCode = oi.PpmpCode,
                    Description = oi.Description ?? oi.ItemName,
                    Unit = oi.Unit,
                    UnitCost = oi.UnitCost ?? 0,
                    OrderedQty = orderedQty,
                    PreviousInspectedQty = postedQty,
                    InspectNowQty = currentInspectNow,
                    Remarks = itemRemarks,
                    IsSetLot = oi.OrderSubItems.Any(),
                    CategoryCode = category,
                    ItemExtnName = itemExtnName,
                    Disposition = existingItem?.Disposition 
                        ?? (existingItem?.InvDist == "D" ? "For Distribution" : (existingItem?.InvDist == "I" ? "Inventory" : existingItem?.InvDist)) 
                        ?? vm.Disposition
                };

                // Populate existing extension records if any
                if (existingItem != null && existingItem.AIRItemExtns != null && existingItem.AIRItemExtns.Any())
                {
                    foreach (var extn in existingItem.AIRItemExtns.OrderBy(e => e.ContentNo))
                    {
                        var detail = new AIRItemInventoryDetailViewModel
                        {
                            Id = extn.Id,
                            AirItemId = extn.AIRItemId,
                            ContentNo = extn.ContentNo ?? 1,
                            TContentNo = extn.TContentNo,
                            SetLotNo = extn.SetLotNo,
                            SetLotQtyNo = extn.SetLotQtyNo
                        };

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

                        detail.IsCompleted = IsDetailCompleted(detail, itemExtnName);
                        lineItem.InventoryDetails.Add(detail);
                    }
                }

                // Reconcile units for lineItem if Disposition == "Inventory"
                if (lineItem.Disposition == "Inventory" && lineItem.InspectNowQty > 0)
                {
                    int targetCount = (int)Math.Floor(lineItem.InspectNowQty);
                    for (int q = lineItem.InventoryDetails.Count + 1; q <= targetCount; q++)
                    {
                        lineItem.InventoryDetails.Add(new AIRItemInventoryDetailViewModel
                        {
                            ContentNo = q,
                            TContentNo = q,
                            IsCompleted = false,
                            Condition = "Good"
                        });
                    }
                }

                // Sub-items if Set/Lot
                foreach (var sub in oi.OrderSubItems.OrderBy(s => s.SortOrder))
                {
                    decimal subExpected = orderedQty * sub.QtyPerSet;
                    lineItem.SubItems.Add(new AIRSubItemViewModel
                    {
                        OrderSubItemId = sub.Id,
                        SubItemNo = sub.ItemNoIndex ?? sub.ItemNo,
                        Description = sub.Description,
                        Unit = sub.Unit,
                        ExpectedQty = subExpected,
                        PreviousInspectedQty = 0,
                        InspectNowQty = 0
                    });
                }

                vm.Items.Add(lineItem);
            }

            return vm;
        }

        public async Task<AIRWizardViewModel> GetAirInspectionForEditAsync(Guid airId)
        {
            var air = await _db.AIRs.Include(a => a.Order).Include(a => a.AIRItems).FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR not found.");

            var vm = await GetPOInspectionDetailsAsync(air.OrderId.Value, airId);
            vm.OverallStatus = ResolveOverallStatus(air);
            vm.InspectionStatus = air.PostedDt != null ? AirInspectionStatuses.Posted : (air.IsInspected == true ? AirInspectionStatuses.Submitted : AirInspectionStatuses.Draft);

            return vm;
        }

        public async Task<Guid> SaveInspectionDraftAsync(AIRWizardViewModel model, string user)
        {
            AIR air;
            if (model.AirId.HasValue)
            {
                air = await _db.AIRs.Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns)).FirstOrDefaultAsync(a => a.Id == model.AirId.Value);
                if (air == null) throw new InvalidOperationException("Draft AIR not found.");
            }
            else
            {
                air = new AIR
                {
                    Id = Guid.NewGuid(),
                    OrderId = model.OrderId,
                    CtrlNo = await GenerateAirNumberAsync(DateTime.Now),
                    AIRNo = model.AirNo,
                    InsertedBy = user,
                    InsertedDt = DateTime.Now
                };
                _db.AIRs.Add(air);
                model.AirId = air.Id;
            }

            // Core AIR fields
            air.AIRDate = model.AirDate;
            air.InspectedDate = model.InspectionDate;
            air.IsInspected = false; // Remains false in draft
            air.Officer = model.InspectorName; // Legacy field — keep synced
            air.Remarks = model.GeneralRemarks;

            // New inspection detail fields
            air.InspectorName = model.InspectorName;
            air.InspectorDesignation = model.InspectorDesignation;
            air.InspectionCommittee = model.InspectionCommittee;
            air.InspectionLocation = model.InspectionLocation;

            // Invoice fields
            air.InvoiceNo = model.Invoice?.InvoiceNo;
            air.InvoiceDate = model.Invoice?.InvoiceDate;
            air.DrNo = model.Invoice?.DrNo;
            air.InvoiceAmount = model.Invoice?.InvoiceAmount;
            air.InvoiceType = model.Invoice?.InvoiceType;
            air.BillingReference = model.Invoice?.BillingReference;

            // Disposition
            air.Disposition = model.Disposition;
            air.InvDist = model.Disposition == "For Distribution" ? "D" : (model.Disposition == "Inventory" ? "I" : model.Disposition);

            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            // Sync line items and extensions
            SyncAirItems(air, model, user);

            await _db.SaveChangesAsync();

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                AirStatuses.Draft,
                AirStatuses.Draft,
                "Save Draft",
                "AIR inspection draft saved.",
                user
            );
            await _db.SaveChangesAsync();

            return air.Id;
        }

        public async Task<Guid> SubmitForAcceptanceAsync(AIRWizardViewModel model, string user)
        {
            if (model.Items == null || !model.Items.Any(i => i.InspectNowQty > 0))
            {
                throw new InvalidOperationException("At least one item must have an Inspect Now quantity greater than 0.");
            }

            // Validate inventory asset details completion for items with Disposition = Inventory
            foreach (var item in model.Items)
            {
                if (item.Disposition == "Inventory" && item.InspectNowQty > 0)
                {
                    int required = (int)Math.Floor(item.InspectNowQty);
                    int completed = item.InventoryDetails != null ? item.InventoryDetails.Count(d => IsDetailCompleted(d, item.ItemExtnName)) : 0;
                    if (completed < required)
                    {
                        throw new InvalidOperationException($"Cannot submit for acceptance: Inventory details for item '{item.Description}' are incomplete ({completed} of {required} units completed). Please complete all inventory unit entries before submitting.");
                    }
                }
            }

            // Server-side concurrency guard: recalculate remaining from DB based on OrderItemRequests
            foreach (var item in model.Items.Where(i => i.InspectNowQty > 0))
            {
                var oirList = await _db.OrderItemRequests.Where(r => r.OrderItemId == item.OrderItemId).ToListAsync();
                var oirIds = oirList.Select(r => r.Id).ToList();

                var dbPosted = await _db.AIRItems
                    .Where(ai => ai.AIR.PostedDt != null &&
                                 (model.AirId == null || ai.AirId != model.AirId.Value) &&
                                 ((ai.OrderItemRequestId.HasValue && oirIds.Contains(ai.OrderItemRequestId.Value)) ||
                                  (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == item.OrderItemId)))
                    .SumAsync(ai => (decimal?)ai.Qty) ?? 0;

                decimal totalQty = oirList.Sum(r => (decimal?)r.QtyApplied ?? 0);
                if (totalQty == 0)
                {
                    var orderItem = await _db.OrderItems.FindAsync(item.OrderItemId);
                    if (orderItem == null) throw new InvalidOperationException($"Order item not found: {item.ItemNo}");
                    totalQty = orderItem.Qty ?? 0;
                }

                var remainingAllowed = totalQty - dbPosted;
                if (item.InspectNowQty > remainingAllowed)
                {
                    throw new InvalidOperationException(
                        $"Concurrency conflict on item {item.ItemNo}: inspected qty ({item.InspectNowQty}) " +
                        $"exceeds remaining deliverable qty ({remainingAllowed}). Another inspection may have posted.");
                }
            }

            AIR air;
            bool isResubmit = false;

            if (model.AirId.HasValue)
            {
                air = await _db.AIRs.Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns)).FirstOrDefaultAsync(a => a.Id == model.AirId.Value);
                if (air == null) throw new InvalidOperationException("Draft AIR not found.");
                isResubmit = !string.IsNullOrWhiteSpace(air.RevisionComments);
            }
            else
            {
                air = new AIR
                {
                    Id = Guid.NewGuid(),
                    OrderId = model.OrderId,
                    CtrlNo = await GenerateAirNumberAsync(DateTime.Now),
                    InsertedBy = user,
                    InsertedDt = DateTime.Now
                };
                _db.AIRs.Add(air);
                model.AirId = air.Id;
            }

            air.AIRDate = model.AirDate;
            air.InspectedDate = model.InspectionDate;
            air.IsInspected = true; // Signals inspection is complete
            air.Officer = model.InspectorName; // Legacy field — keep synced
            air.Remarks = model.GeneralRemarks;

            // Generate official AIR number on first submission if not set
            if (string.IsNullOrWhiteSpace(air.AIRNo))
            {
                air.AIRNo = await GenerateAirNumberAsync(DateTime.Now);
            }

            air.InspectorName = model.InspectorName;
            air.InspectorDesignation = model.InspectorDesignation;
            air.InspectionCommittee = model.InspectionCommittee;
            air.InspectionLocation = model.InspectionLocation;

            air.InvoiceNo = model.Invoice?.InvoiceNo;
            air.InvoiceDate = model.Invoice?.InvoiceDate;
            air.DrNo = model.Invoice?.DrNo;
            air.InvoiceAmount = model.Invoice?.InvoiceAmount;
            air.InvoiceType = model.Invoice?.InvoiceType;
            air.BillingReference = model.Invoice?.BillingReference;

            // Disposition
            air.Disposition = model.Disposition;
            air.InvDist = model.Disposition == "For Distribution" ? "D" : (model.Disposition == "Inventory" ? "I" : model.Disposition);

            // Clear revision comments on resubmit so it enters clean review
            if (isResubmit)
            {
                air.RevisionComments = null;
                air.ReturnedBy = null;
                air.ReturnedDt = null;
            }

            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            SyncAirItems(air, model, user);

            await _db.SaveChangesAsync();

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                isResubmit ? AirStatuses.ReturnedForRevision : AirStatuses.Draft,
                AirStatuses.SubmittedForAcceptance,
                "Submit for Acceptance",
                "Inspection submitted for acceptance review.",
                user
            );
            await _db.SaveChangesAsync();

            return air.Id;
        }

        public async Task WithdrawSubmissionAsync(Guid airId, string reason, string user)
        {
            var air = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR record not found.");

            // Block if acceptance has already started (AcceptanceStartedDt set) or is fully posted
            if (air.PostedDt != null)
            {
                throw new InvalidOperationException("Cannot withdraw a posted AIR. The acceptance has already been finalized.");
            }
            if (air.AcceptanceStartedDt != null)
            {
                throw new InvalidOperationException(
                    "Cannot directly withdraw — acceptance has already started. " +
                    "Please use 'Request Recall' to notify the Acceptance Officer.");
            }

            air.IsInspected = false;
            air.WithdrawalReason = string.IsNullOrWhiteSpace(reason) ? "Submission withdrawn by inspector." : reason;
            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            await _db.SaveChangesAsync();

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
                $"Recall requested by {user}: {reason}",
                user
            );
            await _db.SaveChangesAsync();
        }

        public async Task DiscardDraftAsync(Guid draftId, string user)
        {
            var air = await _db.AIRs.Include(a => a.AIRItems.Select(ai => ai.AIRItemExtns)).FirstOrDefaultAsync(a => a.Id == draftId);
            if (air == null) throw new InvalidOperationException("Draft not found.");

            if (air.IsInspected == true || air.PostedDt != null)
            {
                throw new InvalidOperationException("Only draft AIR records can be discarded.");
            }

            var items = air.AIRItems.ToList();
            foreach (var item in items)
            {
                var extns = item.AIRItemExtns.ToList();
                foreach (var extn in extns)
                {
                    _db.AIRItemExtns.Remove(extn);
                }
                _db.AIRItems.Remove(item);
            }
            _db.AIRs.Remove(air);

            await _db.SaveChangesAsync();
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
                InspectionDate = i.AIR.AIRDate ?? i.AIR.InsertedDt ?? DateTime.Today,
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
            foreach (var item in model.Items)
            {
                var existingItem = air.AIRItems.FirstOrDefault(ai =>
                    (ai.OrderItemRequestId.HasValue && item.OrderItemRequestId != Guid.Empty && ai.OrderItemRequestId.Value == item.OrderItemRequestId) ||
                    (ai.OrderItemRequest != null && ai.OrderItemRequest.OrderItemId == item.OrderItemId));

                if (item.InspectNowQty > 0)
                {
                    if (existingItem == null)
                    {
                        var oir = item.OrderItemRequestId != Guid.Empty 
                            ? _db.OrderItemRequests.FirstOrDefault(r => r.Id == item.OrderItemRequestId) 
                            : _db.OrderItemRequests.FirstOrDefault(r => r.OrderItemId == item.OrderItemId);

                        if (oir == null)
                        {
                            oir = new OrderItemRequest
                            {
                                Id = Guid.NewGuid(),
                                OrderItemId = item.OrderItemId,
                                QtyApplied = (int)Math.Round(item.OrderedQty),
                                InsertedBy = user,
                                InsertedDt = DateTime.Now
                            };
                            _db.OrderItemRequests.Add(oir);
                        }

                        existingItem = new AIRItem
                        {
                            Id = Guid.NewGuid(),
                            AirId = air.Id,
                            OrderItemRequestId = oir.Id,
                            Qty = item.InspectNowQty,
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
                        existingItem.Remarks = item.Remarks;
                        existingItem.Disposition = item.Disposition;
                        existingItem.InvDist = item.Disposition == "For Distribution" ? "D" : (item.Disposition == "Inventory" ? "I" : item.Disposition);
                        existingItem.UpdatedBy = user;
                        existingItem.UpdatedDt = DateTime.Now;
                    }

                    // Sync extension records for this item
                    SyncItemExtensions(existingItem, item, user);
                }
                else
                {
                    if (existingItem != null)
                    {
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
        }

        private void SyncItemExtensions(AIRItem airItem, AIRLineItemViewModel item, string user)
        {
            var existingExtns = airItem.AIRItemExtns != null 
                ? airItem.AIRItemExtns.ToList() 
                : _db.AIRItemExtns.Where(e => e.AIRItemId == airItem.Id).ToList();

            if (item.Disposition != "Inventory")
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
                    var newExtn = CreateConcreteExtension(airItem.Id, q, detail, extnType, user);
                    airItem.AIRItemExtns.Add(newExtn);
                }
            }
        }

        private static AIRItemExtn CreateConcreteExtension(Guid airItemId, int contentNo, AIRItemInventoryDetailViewModel d, string extnType, string user)
        {
            DateTime now = DateTime.Now;
            if (extnType == "ItemExtnVehicle")
            {
                return new AIRItemExtnVehicle
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItemId,
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
            else // Default to ItemExtnOther
            {
                return new AIRItemExtnOther
                {
                    Id = Guid.NewGuid(),
                    AIRItemId = airItemId,
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

            if (a.IsInspected == true && a.AcceptedDate != null)
                return AirStatuses.Accepted;

            if (a.IsInspected == true && a.AcceptanceStartedDt != null)
                return AirStatuses.AcceptanceInProgress;

            if (a.IsInspected == true && !string.IsNullOrWhiteSpace(a.RevisionComments))
                return AirStatuses.ReturnedForRevision;

            if (a.IsInspected == true)
                return AirStatuses.SubmittedForAcceptance;

            return AirStatuses.Draft;
        }

        private async Task<string> GenerateAirNumberAsync(DateTime date)
        {
            var prefix = "AIR-" + date.ToString("yyyyMM") + "-";
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
    }
}
