using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemTransferItemBldgService
    {
        List<PsCardItemExtnBldgVM> GetCardItemExtns(Guid? psCardTransferId);

        ValueTask<PsCardItemExtnBldgVM> CreateAsync(PsCardItemExtnBldgVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnBldgVM> UpdateAsync(PsCardItemExtnBldgVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnBldgVM> DeleteAsync(PsCardItemExtnBldgVM model, string user, DateTime date);
    }

    public class PsCardItemTransferItemBldgService : BaseValidator, IPsCardItemTransferItemBldgService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<PsCardItemExtnBldgVM> _exceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnBldgValidator _psCardItemExtnBldgValidator;
        private readonly IPsCardItemTransferItemSharedService _psCardItemTransferItemSharedService;

        public PsCardItemTransferItemBldgService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnBldgVM>(propertyName);
            _exceptionService = new ExceptionService<PsCardItemExtnBldgVM>();
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnBldgValidator = new PsCardItemExtnBldgValidator(_db);
            _psCardItemTransferItemSharedService = new PsCardItemTransferItemSharedService(_db);
        }

        public List<PsCardItemExtnBldgVM> GetCardItemExtns(Guid? psCardTransferId)
        {
            if (!psCardTransferId.HasValue) return new List<PsCardItemExtnBldgVM>();

            var transfer = _db.PsCardItemTransfers
                .Include(t => t.PsCardItem.PsCard)
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == psCardTransferId.Value);
            if (transfer == null) return new List<PsCardItemExtnBldgVM>();

            bool isCardPosted = transfer.PsCardItem != null && transfer.PsCardItem.PsCard != null && transfer.PsCardItem.PsCard.PostedDt != null;
            decimal? unitCost = transfer.PsCardItem != null ? transfer.PsCardItem.UnitCost : null;
            int tContentNo = transfer.PsCardItem != null ? (int)(transfer.PsCardItem.Qty ?? 0) : 0;

            var records = (from ti in _db.PsCardItemTransferItems.AsNoTracking()
                           where ti.PsCardItemTransferId == psCardTransferId.Value
                           join e in _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().AsNoTracking() on ti.PsCardItemExtnId equals e.Id
                           where e.PsCardSubItemId == null
                           let hasIcsPar = _db.IcsParItems.Any(i => i.PsCardItemExtnId == e.Id)
                           let hasPosted = _db.IcsParItems.Any(i => i.PsCardItemExtnId == e.Id && i.IcsPar.PostedDt != null)
                           let draftRef = _db.IcsParItems.Where(i => i.PsCardItemExtnId == e.Id && i.IcsPar.PostedDt == null).Select(i => i.IcsPar.RefType).FirstOrDefault()
                           let currentPosted = _db.IcsParItems.Where(i => i.PsCardItemExtnId == e.Id && i.IcsPar.PostedDt != null && !_db.IcsParItems.Any(s => s.PrevItemId == i.Id && s.IcsPar.PostedDt != null)).OrderByDescending(i => i.IcsPar.PostedDt).Select(i => new { i.IcsPar.RefType, i.Id }).FirstOrDefault()
                           let hasDraftSuccessor = currentPosted != null && _db.IcsParItems.Any(s => s.PrevItemId == currentPosted.Id && s.IcsPar.PostedDt == null)
                           let compRef = _db.IcsParItemComponents.Where(c => c.PsCardItemExtnId == e.Id).Select(c => new { c.IcsParItem.IcsPar.RefType, c.IcsParItem.IcsPar.RefNo }).FirstOrDefault()
                           let isTransferred = _db.PsCardItemTransferItems.Any(t => t.PsCardItemExtnId == e.Id && t.PsCardItemTransfer.ParentId != null)
                           let isIssued = _db.PsCardItemTransferItems.Any(t => t.PsCardItemExtnId == e.Id && t.PsCardItemTransferIssuanceItems.Any())
                           select new
                           {
                               Extn = e,
                               TransferItemId = ti.Id,
                               TransferId = ti.PsCardItemTransferId,
                               LocationName = e.Codextn != null ? e.Codextn.Description : null,
                               HasIcsPar = hasIcsPar,
                               HasPosted = hasPosted,
                               DraftRef = draftRef,
                               CurrentPosted = currentPosted,
                               HasDraftSuccessor = hasDraftSuccessor,
                               CompRef = compRef,
                               IsTransferred = isTransferred,
                               IsIssued = isIssued
                           }).ToList();

            var list = new List<PsCardItemExtnBldgVM>();
            foreach (var r in records)
            {
                var e = r.Extn;
                bool isAccountable = r.HasIcsPar || r.CompRef != null || r.IsTransferred || r.IsIssued;
                bool canDelete = !isCardPosted && !isAccountable && e.AIRItemExtnId == null;
                bool canEdit = !isCardPosted && !isAccountable;

                string status;
                if (!r.HasIcsPar)
                {
                    if (r.CompRef != null)
                    {
                        status = $"Assigned as component to {r.CompRef.RefType} ({r.CompRef.RefNo})";
                    }
                    else if (r.IsIssued)
                    {
                        status = "Issued";
                    }
                    else if (r.IsTransferred)
                    {
                        status = "Transferred";
                    }
                    else
                    {
                        status = "AVAILABLE";
                    }
                }
                else if (!r.HasPosted)
                {
                    status = (r.DraftRef == "P" || r.DraftRef == "PAR") ? "PAR DRAFT" : "ICS DRAFT";
                }
                else if (r.HasDraftSuccessor)
                {
                    status = "TRANSFER PENDING";
                }
                else
                {
                    status = (r.CurrentPosted != null && (r.CurrentPosted.RefType == "P" || r.CurrentPosted.RefType == "PAR")) ? "PAR POSTED" : "ICS POSTED";
                }

                list.Add(new PsCardItemExtnBldgVM
                {
                    Id = e.Id,
                    TransferItemId = r.TransferItemId,
                    TransferId = r.TransferId,
                    PsCardItemId = e.PsCardItemId,
                    AIRItemExtnId = e.AIRItemExtnId,
                    SetLotNo = e.SetLotNo,
                    SetLotQtyNo = e.SetLotQtyNo,
                    ContentNo = e.ContentNo,
                    CustItemNo = e.CustItemNo,
                    PropNo = e.PropNo,
                    PropYear = e.PropYear,
                    PropSeq = e.PropSeq,
                    SeriesNo = e.SeriesNo,
                    Remarks = e.Remarks,
                    Annex = e.Annex,
                    OldPropNo = e.OldPropNo,
                    UpcomingOfficer = e.UpcomingOfficer,
                    SubLocation = e.SubLocation,
                    AddCost = e.AddCost,
                    AcqCost = e.AcqCost,
                    AcqDate = e.AcqDate,
                    OldAmount = e.OldAmount,
                    Condition = e.Condition,
                    LocationId = e.LocationId,
                    Location = r.LocationName,
                    Description = transfer.PsCardItem != null ? transfer.PsCardItem.Description : null,
                    TContentNo = tContentNo,
                    UnitCost = unitCost,
                    Address = e.Address,
                    BuildingItem = e.BuildingItem,
                    ProjectName = e.ProjectName,
                    BuildingType = e.BuildingType,
                    Area = e.Area,
                    AppraisedValue = e.AppraisedValue,
                    TotalAmount = e.TotalAmount,
                    PhaseNo = e.PhaseNo,
                    PhaseAmountCo = e.PhaseAmountCo,
                    PhaseAmountMooe = e.PhaseAmountMooe,
                    StartDate = e.StartDate,
                    TargetDate = e.TargetDate,
                    PercentComplete = e.PercentComplete,
                    CompletionDate = e.CompletionDate,
                    Status = e.Status,
                    Longitude = e.Longitude,
                    Latitude = e.Latitude,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    AccountabilityStatus = status
                });
            }

            return list;
        }

        public ValueTask<PsCardItemExtnBldgVM> CreateAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBldgValidator.ValidateOnCreate(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            var transfer = await _db.PsCardItemTransfers
                .Include(t => t.PsCardItem.PsCard)
                .FirstOrDefaultAsync(f => f.Id == model.TransferId);
            if (transfer == null || transfer.PsCardItem == null)
            {
                throw new NotFoundException("Parent acquisition record not found.");
            }

            model.PsCardItemId = transfer.PsCardItemId;

            var psCardItem = transfer.PsCardItem;
            int maxQty = (int)(psCardItem.Qty ?? 0);
            int existingMainUnitsCount = await _db.PsCardItemExtns
                .CountAsync(w => w.PsCardItemId == psCardItem.Id && w.PsCardSubItemId == null);

            if (existingMainUnitsCount >= maxQty)
            {
                throw new InvalidValueException($"Cannot create another physical unit. This acquisition has quantity {maxQty} and already has {existingMainUnitsCount} main physical units.");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnBuilding();
            var psCardItemTransferItem = new PsCardItemTransferItem()
            {
                Id = Guid.NewGuid(),
                PsCardItemTransferId = model.TransferId,
                PsCardItemExtnId = model.Id,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            entity.PsCardItemTransferItems.Add(psCardItemTransferItem);
            MapModelToEntityFields(entity, model, Mode.ADD);
            entity.PsCardSubItemId = null; // Always force main unit

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);
            return model;
        });

        public ValueTask<PsCardItemExtnBldgVM> UpdateAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBldgValidator.ValidateOnUpdate(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>()
                .Include(i => i.PsCardItemTransferItems)
                .FirstOrDefaultAsync(f => f.Id == model.Id);
            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }
            if (entity.PsCardSubItemId != null)
            {
                throw new InvalidValueException("Component units cannot be deleted here.");
            }
            if (entity.PsCardSubItemId != null)
            {
                throw new InvalidValueException("Component units cannot be modified here.");
            }

            bool isAccountable = await _db.IcsParItems.AnyAsync(a => a.PsCardItemExtnId == model.Id)
                || await _db.IcsParItemComponents.AnyAsync(a => a.PsCardItemExtnId == model.Id)
                || await _db.PsCardItemTransferItems.AnyAsync(a => a.PsCardItemExtnId == model.Id && a.PsCardItemTransfer.ParentId != null)
                || await _db.PsCardItemTransferItems.AnyAsync(a => a.PsCardItemExtnId == model.Id && a.PsCardItemTransferIssuanceItems.Any());

            if (isAccountable)
            {
                if (!string.Equals(entity.PropNo?.Trim(), model.PropNo?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(entity.BuildingItem?.Trim(), model.BuildingItem?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidValueException("Cannot modify identity-sensitive fields (Property No. / Building Item) because this physical unit is already assigned to an accountability record.");
                }
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var psCardItemTransferItem = entity.PsCardItemTransferItems.FirstOrDefault(f => f.PsCardItemExtnId == model.Id);
            if (psCardItemTransferItem != null)
            {
                psCardItemTransferItem.UpdatedBy = user;
                psCardItemTransferItem.UpdatedDt = date;
            }

            var originalPsCardItemId = entity.PsCardItemId;
            MapModelToEntityFields(entity, model, Mode.EDIT);
            entity.PsCardItemId = originalPsCardItemId; // Prevent cross-acquisition tampering
            entity.PsCardSubItemId = null; // Always ensure main unit

            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);

            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnBuilding entity, PsCardItemExtnBldgVM model, Mode mode)
        {
            _psCardItemTransferItemSharedService.MapModelToEntityFields(entity, model, mode);

            entity.Address = model.Address;
            entity.BuildingItem = model.BuildingItem;
            entity.ProjectName = model.ProjectName;
            entity.BuildingType = model.BuildingType;
            entity.Area = model.Area;
            entity.AppraisedValue = model.AppraisedValue;
            entity.TotalAmount = model.TotalAmount;
            entity.PhaseNo = model.PhaseNo?.ToString();
            entity.PhaseAmountCo = model.PhaseAmountCo;
            entity.PhaseAmountMooe = model.PhaseAmountMooe;
            entity.StartDate = model.StartDate;
            entity.TargetDate = model.TargetDate;            
            entity.PercentComplete = model.PercentComplete;
            entity.CompletionDate = model.CompletionDate;
            entity.Status = model.Status;
            entity.Longitude = model.Longitude;
            entity.Latitude = model.Latitude;            
        }

        public ValueTask<PsCardItemExtnBldgVM> DeleteAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBldgValidator.ValidateOnDelete(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>()
                .Include(i => i.PsCardItemTransferItems)
                .FirstOrDefaultAsync(f => f.Id == model.Id);
            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            var transferItems = _db.PsCardItemTransferItems.Where(w => w.PsCardItemExtnId == model.Id).ToList();
            _db.PsCardItemTransferItems.RemoveRange(transferItems);

            _db.PsCardItemExtns.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private async ValueTask<bool> IsPostedAsync(Guid? PsCardItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == PsCardItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity?.PostedBy);
        }
    }
}