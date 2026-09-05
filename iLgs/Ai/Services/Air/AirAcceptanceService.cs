using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Ai.Models;
using iLgs.Models;

namespace iLgs.Ai.Services.Air
{
    public interface IAirAcceptanceService
    {
        Task<AIRAcceptanceWizardViewModel> GetAcceptanceDetailsAsync(Guid airId);
        Task StartAcceptanceAsync(Guid airId, string user);
        Task ReturnForRevisionAsync(Guid airId, string comments, string user);
        Task DeclineWithdrawalAsync(Guid airId, string reason, string user);
        Task PostAcceptanceAsync(AIRAcceptanceWizardViewModel model, string user);
    }

    public class AirAcceptanceService : IAirAcceptanceService
    {
        private readonly AppManEntities _db;
        private readonly IDocumentHistoryService _historyService;

        public AirAcceptanceService(AppManEntities db, IDocumentHistoryService historyService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        }

        public async Task<AIRAcceptanceWizardViewModel> GetAcceptanceDetailsAsync(Guid airId)
        {
            var air = await _db.AIRs.AsNoTracking()
                .Include(a => a.Order)
                .Include(a => a.AIRItems.Select(i => i.OrderItemRequest.OrderItem))
                .FirstOrDefaultAsync(a => a.Id == airId);

            if (air == null) throw new InvalidOperationException("AIR not found.");

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
                AcceptanceDate = air.AcceptedDate ?? DateTime.Today,
                AcceptedBy = air.AcceptedBy ?? air.Custodian ?? string.Empty,
                AcceptedByDesignation = air.AcceptedByDesignation ?? string.Empty,
                DepartmentOffice = air.Order != null ? air.Order.Department : string.Empty,
                AcceptanceRemarks = air.AcceptanceRemarks ?? air.Remarks
            };

            foreach (var item in air.AIRItems.OrderBy(i => i.OrderItemRequest != null ? i.OrderItemRequest.OrderItem.ItemNo : string.Empty))
            {
                var oi = item.OrderItemRequest != null ? item.OrderItemRequest.OrderItem : null;
                vm.Items.Add(new AIRAcceptanceItemViewModel
                {
                    AirItemId = item.Id,
                    ItemNo = oi != null ? oi.ItemNo : string.Empty,
                    Description = oi != null ? (oi.Description ?? oi.ItemName) : "Item",
                    Unit = oi != null ? oi.Unit : string.Empty,
                    OrderedQty = item.OrderItemRequest != null && item.OrderItemRequest.QtyApplied.HasValue ? item.OrderItemRequest.QtyApplied.Value : (oi != null ? (oi.Qty ?? 0) : 0),
                    InspectedQty = item.Qty ?? 0,
                    AcceptedQty = item.AcceptedQty ?? item.Qty ?? 0, // Default to inspected qty
                    Disposition = !string.IsNullOrEmpty(item.Disposition)
                        ? item.Disposition
                        : (item.InvDist == "D" ? AirDispositions.ForDistribution : AirDispositions.Inventory),
                    DestinationDepartment = item.DestinationDepartment,
                    DestinationCustodian = item.DestinationCustodian,
                    Remarks = item.Remarks
                });
            }

            return vm;
        }

        public async Task StartAcceptanceAsync(Guid airId, string user)
        {
            var air = await _db.AIRs.FirstOrDefaultAsync(a => a.Id == airId);
            if (air == null) throw new InvalidOperationException("AIR record not found.");

            if (air.PostedDt != null)
                throw new InvalidOperationException("AIR is already posted.");

            if (air.IsInspected != true)
                throw new InvalidOperationException("AIR has not been submitted for acceptance yet.");

            // Use AcceptanceStartedDt to mark start-of-acceptance (not AcceptedDate, which is reserved for final acceptance)
            air.AcceptanceStartedDt = DateTime.Now;
            air.AcceptanceStartedBy = user;
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

            // Revert inspection submission
            air.IsInspected = false;
            air.AcceptanceStartedDt = null;  // Resets acceptance start
            air.AcceptanceStartedBy = null;

            // Persist revision details using new dedicated columns
            air.RevisionComments = comments;
            air.ReturnedBy = user;
            air.ReturnedDt = DateTime.Now;

            // Also reset withdrawal request if any
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

            // Clear the withdrawal request flag
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
            var air = await _db.AIRs.Include(a => a.AIRItems).FirstOrDefaultAsync(a => a.Id == model.AirId);
            if (air == null) throw new InvalidOperationException("AIR record not found.");

            if (air.PostedDt != null)
                throw new InvalidOperationException("AIR is already posted.");

            // AcceptedDate = final acceptance date (not start date)
            air.AcceptedDate = model.AcceptanceDate;
            air.AcceptedBy = model.AcceptedBy;
            air.AcceptedByDesignation = model.AcceptedByDesignation;
            air.AcceptanceRemarks = model.AcceptanceRemarks;
            air.Custodian = model.AcceptedBy; // Legacy field — keep synced
            air.PostedBy = user;
            air.PostedDt = DateTime.Now;
            air.UpdatedBy = user;
            air.UpdatedDt = DateTime.Now;

            foreach (var itemVm in model.Items)
            {
                var dbItem = air.AIRItems.FirstOrDefault(i => i.Id == itemVm.AirItemId);
                if (dbItem != null)
                {
                    dbItem.AcceptedQty = itemVm.AcceptedQty;
                    dbItem.Qty = itemVm.AcceptedQty; // Keep Qty consistent with accepted qty
                    dbItem.Disposition = itemVm.Disposition;
                    dbItem.InvDist = itemVm.Disposition == AirDispositions.ForDistribution ? "D" : "I"; // Legacy field
                    dbItem.DestinationDepartment = itemVm.DestinationDepartment;
                    dbItem.DestinationCustodian = itemVm.DestinationCustodian;
                    dbItem.Remarks = itemVm.Remarks;
                    dbItem.UpdatedBy = user;
                    dbItem.UpdatedDt = DateTime.Now;
                }
            }

            _historyService.AddStatusHistory(
                DocumentTypes.AcceptanceInspectionReport,
                air.Id,
                air.AIRNo ?? air.CtrlNo,
                AirStatuses.AcceptanceInProgress,
                AirStatuses.Posted,
                "Post Acceptance",
                "AIR officially accepted, obligation posted, and ready for property recording.",
                user
            );

            await _db.SaveChangesAsync();
        }
    }
}
