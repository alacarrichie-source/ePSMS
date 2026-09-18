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
    public interface IPsCardItemTransferItemLandService
    {
        List<PsCardItemExtnLandVM> GetCardItemExtns(Guid? psCardTransferId);

        ValueTask<PsCardItemExtnLandVM> CreateAsync(PsCardItemExtnLandVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnLandVM> UpdateAsync(PsCardItemExtnLandVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnLandVM> DeleteAsync(PsCardItemExtnLandVM model, string user, DateTime date);
    }

    public class PsCardItemTransferItemLandService : BaseValidator, IPsCardItemTransferItemLandService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<PsCardItemExtnLandVM> _exceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnLandValidator _psCardItemExtnLandValidator;
        private readonly IPsCardItemTransferItemSharedService _psCardItemTransferItemSharedService;

        public PsCardItemTransferItemLandService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnLandVM>(propertyName);
            _exceptionService = new ExceptionService<PsCardItemExtnLandVM>();
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnLandValidator = new PsCardItemExtnLandValidator(_db);
            _psCardItemTransferItemSharedService = new PsCardItemTransferItemSharedService(_db);
        }

        public List<PsCardItemExtnLandVM> GetCardItemExtns(Guid? psCardTransferId)
        {
            if (!psCardTransferId.HasValue) return new List<PsCardItemExtnLandVM>();

            var transfer = _db.PsCardItemTransfers
                .Include(t => t.PsCardItem.PsCard)
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == psCardTransferId.Value);
            if (transfer == null) return new List<PsCardItemExtnLandVM>();

            bool isCardPosted = transfer.PsCardItem != null && transfer.PsCardItem.PsCard != null && transfer.PsCardItem.PsCard.PostedDt != null;
            decimal? unitCost = transfer.PsCardItem != null ? transfer.PsCardItem.UnitCost : null;
            int tContentNo = transfer.PsCardItem != null ? (int)(transfer.PsCardItem.Qty ?? 0) : 0;

            var records = (from ti in _db.PsCardItemTransferItems.AsNoTracking()
                           where ti.PsCardItemTransferId == psCardTransferId.Value
                           join e in _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().AsNoTracking() on ti.PsCardItemExtnId equals e.Id
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

            var list = new List<PsCardItemExtnLandVM>();
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

                list.Add(new PsCardItemExtnLandVM
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
                    PIN = e.PIN,
                    Address = e.Address,
                    LandMarks = e.LandMarks,
                    MarketValue = e.MarketValue,
                    PricePerSqm = e.PricePerSqm,
                    TctNo = e.TctNo,
                    OldTctNo = e.OldTctNo,
                    DRPNo = e.DRPNo,
                    DRPDate = e.DRPDate,
                    OldDRPNo = e.OldDRPNo,
                    OldDRPDate = e.OldDRPDate,
                    CGT = e.CGT,
                    CGTTransferTax = e.CGTTransferTax,
                    CGTSurcharge = e.CGTSurcharge,
                    CGTInteest = e.CGTInteest,
                    CGTCompromise = e.CGTCompromise,
                    DST = e.DST,
                    DSTTransferTax = e.DSTTransferTax,
                    DSTSurcharge = e.DSTSurcharge,
                    DSTInterest = e.DSTInterest,
                    DSTCompromise = e.DSTCompromise,
                    TransferTax = e.TransferTax,
                    Surcharge = e.Surcharge,
                    Interest = e.Interest,
                    ConfirmationFee = e.ConfirmationFee,
                    TransferRegsFee = e.TransferRegsFee,
                    RealPropertyTax = e.RealPropertyTax,
                    VAT = e.VAT,
                    EstateTax = e.EstateTax,
                    Titling = e.Titling,
                    CerttificationFee = e.CerttificationFee,
                    Relocation = e.Relocation,
                    Surveying = e.Surveying,
                    IncidentalExpenses = e.IncidentalExpenses,
                    CapitalOutlayOrExpense = e.CapitalOutlayOrExpense,
                    AreaXPrice = e.AreaXPrice,
                    Vendor = e.Vendor,
                    Representative = e.Representative,
                    TotalCap = e.TotalCap,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    AccountabilityStatus = status
                });
            }

            return list;
        }

        public ValueTask<PsCardItemExtnLandVM> CreateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnCreate(model);
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

            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().Where(w => w.PsCardItemId == model.PsCardItemId && w.PsCardSubItemId == null);
            if (!string.IsNullOrWhiteSpace(model.PIN) && itemExtns.Any(a => a.PIN == model.PIN))
            {
                throw new RecordAlreadyExistsException($"PIN {model.PIN} already exists!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnLand();

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

        public ValueTask<PsCardItemExtnLandVM> UpdateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnUpdate(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>()
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
                    !string.Equals(entity.TctNo?.Trim(), model.TctNo?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(entity.PIN?.Trim(), model.PIN?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidValueException("Cannot modify identity-sensitive fields (Property No. / TCT No. / PIN) because this physical unit is already assigned to an accountability record.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.PIN))
            {
                var duplicate = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>()
                    .Where(w => w.PsCardItemId == entity.PsCardItemId && w.Id != model.Id && w.PsCardSubItemId == null && w.PIN == model.PIN)
                    .FirstOrDefaultAsync();
                if (duplicate != null)
                {
                    throw new RecordAlreadyExistsException($"PIN {model.PIN} already exists!");
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

        public void MapModelToEntityFields(PsCardItemExtnLand entity, PsCardItemExtnLandVM model, Mode mode)
        {            
            _psCardItemTransferItemSharedService.MapModelToEntityFields(entity, model, mode);

            entity.PropNo= model.PropNo;
            entity.PIN = model.PIN;
            entity.Address = model.Address;
            entity.LandMarks = model.LandMarks;
            entity.MarketValue = model.MarketValue;
            entity.PricePerSqm = model.PricePerSqm;
            entity.AreaXPrice = model.AreaXPrice;
            entity.OldAmount = model.OldAmount;
            entity.Vendor = model.Vendor;
            entity.Representative = model.Representative;
            entity.TctNo = model.TctNo;
            entity.OldTctNo = model.OldTctNo;
            entity.DRPNo = model.DRPNo;
            entity.DRPDate = model.DRPDate;
            entity.OldDRPNo = model.OldDRPNo;
            entity.OldDRPDate = model.OldDRPDate;
            entity.CGT = model.CGT;
            entity.CGTCompromise = model.CGTCompromise;
            entity.CGTCompromiseCap = model.CGTCompromiseCap;
            entity.CGTInteest = model.CGTInteest;
            entity.CGTInterestCap = model.CGTInterestCap;
            entity.CGTSurcharge = model.CGTSurcharge;
            entity.CGTSurchargeCap = model.CGTSurchargeCap;
            entity.CGTTransferTax = model.CGTTransferTax;
            entity.CGTTransferTaxCap = model.CGTTransferTaxCap;
            entity.DST = model.DST;
            entity.DSTCompromise = model.DSTCompromise;
            entity.DSTCompromiseCap = model.DSTCompromiseCap;
            entity.DSTInterest = model.DSTInterest;
            entity.DSTInterestCap = model.DSTInterestCap;
            entity.DSTSurcharge = model.DSTSurcharge;
            entity.DSTSurchargeCap = model.DSTSurchargeCap;
            entity.DSTTransferTax = model.DSTTransferTax;
            entity.DSTTransferTaxCap = model.DSTTransferTaxCap;
            entity.TransferTax = model.TransferTax;
            entity.Surcharge = model.Surcharge;
            entity.Interest = model.Interest;
            entity.TransferTaxCap = model.TransferTaxCap;
            entity.SurchargeCap = model.SurchargeCap;
            entity.InterestCap = model.InterestCap;
            entity.ConfirmationFee = model.ConfirmationFee;
            entity.TransferRegsFee = model.TransferRegsFee;
            entity.RealPropertyTax = model.RealPropertyTax;
            entity.ConfirmationFeeCap = model.ConfirmationFeeCap;
            entity.TransferRegsFeeCap = model.TransferRegsFeeCap;
            entity.RealPropertyTaxCap = model.RealPropertyTaxCap;
            entity.VAT = model.VAT;
            entity.EstateTax = model.EstateTax;
            entity.Titling = model.Titling;
            entity.CerttificationFee = model.CerttificationFee;
            entity.Relocation = model.Relocation;
            entity.Surveying = model.Surveying;
            entity.IncidentalExpenses = model.IncidentalExpenses;
            entity.VATCap = model.VATCap;
            entity.EstateTaxCap = model.EstateTaxCap;
            entity.TitlingCap = model.TitlingCap;
            entity.CertificationFeeCap = model.CertificationFeeCap;
            entity.RelocationCap = model.RelocationCap;
            entity.SurveyingCap = model.SurveyingCap;
            entity.IncidentalExpensesCap = model.IncidentalExpensesCap;
        }

        public ValueTask<PsCardItemExtnLandVM> DeleteAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnDelete(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>()
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