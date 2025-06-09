using iLgs.Exceptions;
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
        private readonly IExceptionService<PsCardItemExtnLandVM> _exceptionService = new ExceptionService<PsCardItemExtnLandVM>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnLandValidator _psCardItemExtnLandValidator;
        private readonly IPsCardItemTransferItemService _psCardItemTransferItemService;

        public PsCardItemTransferItemLandService(AppManEntities db, IPsCardItemTransferItemService psCardItemTransferItemService)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnLandVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnLandValidator = new PsCardItemExtnLandValidator(_db);
            _psCardItemTransferItemService = psCardItemTransferItemService;
        }

        public List<PsCardItemExtnLandVM> GetCardItemExtns(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnLandVM>("Exec PsCardItemExtnTransferItem_GetItemExtnLandByTransferId {0}", psCardTransferId).ToList();
            return data;
        }


        public ValueTask<PsCardItemExtnLandVM> CreateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnCreate(model);
            _psCardItemTransferItemService.ValidateIfTransit(model.TransferId);

            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().Where(w => w.PsCardItemId == model.PsCardItemId);

            if (!string.IsNullOrWhiteSpace(model.PIN))
            {
                if (itemExtns.Any(a => a.PIN == model.PIN))
                {
                    throw new RecordAlreadyExistsException($"PIN {model.PIN} already exists!");
                }
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

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.PsCardItemExtnId, model.PsCardItemId, "CARD", user, date);
            return model;
        });

        public ValueTask<PsCardItemExtnLandVM> UpdateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnUpdate(model);
            _psCardItemTransferItemService.ValidateIfTransit(model.TransferId);

            var itemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.PsCardItemExtnId && w.PIN == model.PIN).FirstOrDefaultAsync();

            if (itemExtn != null)
            {
                throw new RecordAlreadyExistsException($"PIN {model.PIN} already exists!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>()
                .Include(i => i.PsCardItemTransferItems)
                .FirstOrDefaultAsync(f => f.Id == model.PsCardItemExtnId);
            var psCardItemTransferItem = entity.PsCardItemTransferItems.FirstOrDefault(f => f.Id == model.Id);
            psCardItemTransferItem.UpdatedBy = user;
            psCardItemTransferItem.UpdatedDt = date;

            entity.PsCardItemTransferItems.Add(psCardItemTransferItem);
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.PsCardItemExtnId, model.PsCardItemId, "CARD", user, date);

            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnLand entity, PsCardItemExtnLandVM model, Mode mode)
        {            
            _psCardItemTransferItemService.MapModelToEntityFields(entity, model, mode);

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
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        public ValueTask<PsCardItemExtnLandVM> DeleteAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnDelete(model);
            _psCardItemTransferItemService.ValidateIfTransit(model.TransferId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            // Delete TransferItem
            var transferItemEntity = await _db.PsCardItemTransferItems.FindAsync(model.Id);
            var psCardItemExtnId = transferItemEntity.PsCardItemExtnId;

            transferItemEntity.UpdatedBy = model.UpdatedBy;
            transferItemEntity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemTransferItems.Attach(transferItemEntity);
            _db.Entry(transferItemEntity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemTransferItems.Remove(transferItemEntity);
            _db.Entry(transferItemEntity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            // Delete CardItem
            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().FirstOrDefaultAsync(f => f.Id == psCardItemExtnId);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });


        private async ValueTask<bool> IsPostedAsync(Guid? PsCardItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == PsCardItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}