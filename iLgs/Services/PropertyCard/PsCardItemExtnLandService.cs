using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnLandService
    {
        IQueryable<PsCardItemExtnLandVM> GetByPsCardItemId(Guid? psCardItemId);
        IQueryable<PsCardItemExtnLandVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? trasferId);
        ValueTask<PsCardItemExtnLandVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemExtnLandVM> CreateAsync(PsCardItemExtnLandVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnLandVM> UpdateAsync(PsCardItemExtnLandVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnLandVM> DeleteAsync(PsCardItemExtnLandVM model, string user, DateTime date);
    }

    internal class PsCardItemExtnLandService : IPsCardItemExtnLandService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemExtnLandVM> _exceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnLandValidator _psCardItemExtnLandValidator;
        private readonly IPsCardItemExtnSharedService _psCardItemExtnSharedService;

        public PsCardItemExtnLandService(AppManEntities db)
        {
            _db = db;
            _exceptionService = new ExceptionService<PsCardItemExtnLandVM>();
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnLandValidator = new PsCardItemExtnLandValidator(_db);
            _psCardItemExtnSharedService = new PsCardItemExtnSharedService(_db);
        }

        //public PsCardItemExtnLandService(AppManEntities db,
        //    IExceptionService<PsCardItemExtnLandVM> exceptionService,
        //    IPsCardItemTransactionService psCardItemTransactionService,
        //    IPsCardItemExtnLandValidator psCardItemExtnLandValidator,
        //    IPsCardItemExtnSharedService psCardItemExtnSharedService)
        //{
        //    _db = db;
        //    _exceptionService = exceptionService;
        //    _psCardItemTransactionService = psCardItemTransactionService;
        //    _psCardItemExtnLandValidator = psCardItemExtnLandValidator;
        //    _psCardItemExtnSharedService = psCardItemExtnSharedService;
        //}

        private Expression<Func<PsCardItemExtnLand, PsCardItemExtnLandVM>> GetProjection()
        {
            return s => new PsCardItemExtnLandVM
            {
                Location = s.Codextn.Description,
                Id = s.Id,
                //PsCardItemExtnId = s.Id,
                PsCardItemId = s.PsCardItemId,
                AIRItemExtnId = s.AIRItemExtnId,
                SetLotNo = s.SetLotNo,
                SetLotQtyNo = s.SetLotQtyNo,
                ContentNo = s.ContentNo,
                CustItemNo = s.CustItemNo,
                IsAutoGen = s.IsAutoGen,
                LocationId = s.LocationId,
                PropNo = s.PropNo,
                PropYear = s.PropYear,
                PropSeq = s.PropSeq,
                SeriesNo = s.SeriesNo,
                Remarks = s.Remarks,
                Annex = s.Annex,
                OldAmount = s.OldAmount,
                OldPropNo = s.OldPropNo,
                UpcomingOfficer = s.UpcomingOfficer,
                SubLocation = s.SubLocation,
                Condition = s.Condition,
                AddCost = s.AddCost,
                AcqCost = s.AcqCost,
                AcqDate = s.AcqDate,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                //Extension                
                PIN = s.PIN,
                Address = s.Address,
                LandMarks = s.LandMarks,
                MarketValue = s.MarketValue,
                PricePerSqm = s.PricePerSqm,
                AreaXPrice = s.AreaXPrice,
                Vendor = s.Vendor,
                Representative = s.Representative,
                TctNo = s.TctNo,
                OldTctNo = s.OldTctNo,
                DRPNo = s.DRPNo,
                DRPDate = s.DRPDate,
                OldDRPNo = s.OldDRPNo,
                OldDRPDate = s.OldDRPDate,
                CGT = s.CGT,
                CGTTransferTax = s.CGTTransferTax,
                CGTSurcharge = s.CGTSurcharge,
                CGTInteest = s.CGTInteest,
                CGTCompromise = s.CGTCompromise,
                CGTTransferTaxCap = s.CGTTransferTaxCap,
                CGTSurchargeCap = s.CGTSurchargeCap,
                CGTInterestCap = s.CGTInterestCap,
                CGTCompromiseCap = s.CGTCompromiseCap,
                DST = s.DST,
                DSTTransferTax = s.DSTTransferTax,
                DSTSurcharge = s.DSTSurcharge,
                DSTInterest = s.DSTInterest,
                DSTCompromise = s.DSTCompromise,
                DSTTransferTaxCap = s.DSTTransferTaxCap,
                DSTSurchargeCap = s.DSTSurchargeCap,
                DSTInterestCap = s.DSTInterestCap,
                DSTCompromiseCap = s.DSTCompromiseCap,
                TransferTax = s.TransferTax,
                Surcharge = s.Surcharge,
                Interest = s.Interest,
                TransferTaxCap = s.TransferTaxCap,
                SurchargeCap = s.SurchargeCap,
                InterestCap = s.InterestCap,
                ConfirmationFee = s.ConfirmationFee,
                TransferRegsFee = s.TransferRegsFee,
                RealPropertyTax = s.RealPropertyTax,
                ConfirmationFeeCap = s.ConfirmationFeeCap,
                TransferRegsFeeCap = s.TransferRegsFeeCap,
                RealPropertyTaxCap = s.RealPropertyTaxCap,
                VAT = s.VAT,
                EstateTax = s.EstateTax,
                Titling = s.Titling,
                CerttificationFee = s.CerttificationFee,
                Relocation = s.Relocation,
                Surveying = s.Surveying,
                IncidentalExpenses = s.IncidentalExpenses,
                VATCap = s.VATCap,
                EstateTaxCap = s.EstateTaxCap,
                TitlingCap = s.TitlingCap,
                CertificationFeeCap = s.CertificationFeeCap,
                RelocationCap = s.RelocationCap,
                SurveyingCap = s.SurveyingCap,
                IncidentalExpensesCap = s.IncidentalExpensesCap,
                CapitalOutlayOrExpense = s.CapitalOutlayOrExpense
            };
        }

        public IQueryable<PsCardItemExtnLandVM> GetByPsCardItemId(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId)
                .Select(GetProjection());
            return data;
        }

        public IQueryable<PsCardItemExtnLandVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? transferId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId && w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == transferId))
                .Select(GetProjection());
            return data;
        }

        public ValueTask<PsCardItemExtnLandVM> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().AsNoTracking()
                .Where(w => w.Id == id)
                .Select(GetProjection())
                .FirstOrDefaultAsync();
            return data;
        });


        public ValueTask<PsCardItemExtnLandVM> CreateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnCreate(model);

            var psCardItem = await _db.PsCardItems.FirstOrDefaultAsync(f => f.Id == model.PsCardItemId);
            var itemQty = (int)(psCardItem.Qty ?? 0);
            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnLandVM>().Where(w => w.PsCardItemId == model.PsCardItemId);
            var itemExtnCount = itemExtns.Count();

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnLand();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemExtnLandVM> UpdateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().FirstOrDefaultAsync(f => f.Id == model.Id);
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnLand entity, PsCardItemExtnLandVM model, Mode mode)
        {
            _psCardItemExtnSharedService.MapModelToEntityFields(entity, model, mode);

            // Extension
            entity.PIN = model.PIN;
            entity.Address = model.Address;
            entity.LandMarks = model.LandMarks;
            entity.MarketValue = model.MarketValue;
            entity.PricePerSqm = model.PricePerSqm;
            entity.AreaXPrice = model.AreaXPrice;
            entity.Vendor = model.Vendor;
            entity.Representative = model.Representative;
            entity.TctNo = model.TctNo;
            entity.OldTctNo = model.OldTctNo;
            entity.DRPNo = model.DRPNo;
            entity.DRPDate = model.DRPDate;
            entity.OldDRPNo = model.OldDRPNo;
            entity.OldDRPDate = model.OldDRPDate;
            entity.CGT = model.CGT;
            entity.CGTTransferTax = model.CGTTransferTax;
            entity.CGTSurcharge = model.CGTSurcharge;
            entity.CGTInteest = model.CGTInteest;
            entity.CGTCompromise = model.CGTCompromise;
            entity.CGTTransferTaxCap = model.CGTTransferTaxCap;
            entity.CGTSurchargeCap = model.CGTSurchargeCap;
            entity.CGTInterestCap = model.CGTInterestCap;
            entity.CGTCompromiseCap = model.CGTCompromiseCap;
            entity.DST = model.DST;
            entity.DSTTransferTax = model.DSTTransferTax;
            entity.DSTSurcharge = model.DSTSurcharge;
            entity.DSTInterest = model.DSTInterest;
            entity.DSTCompromise = model.DSTCompromise;
            entity.DSTTransferTaxCap = model.DSTTransferTaxCap;
            entity.DSTSurchargeCap = model.DSTSurchargeCap;
            entity.DSTInterestCap = model.DSTInterestCap;
            entity.DSTCompromiseCap = model.DSTCompromiseCap;
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
            entity.CapitalOutlayOrExpense = model.CapitalOutlayOrExpense;
            decimal? totalCap = 0;
            if (model.CGTCompromiseCap.Value == true)
            {
                totalCap += model.CGTCompromise;
            }
            if (model.CGTInterestCap.Value == true)
            {
                totalCap += model.CGTInteest;
            }
            if (model.CGTSurchargeCap.Value == true)
            {
                totalCap += model.CGTSurcharge;
            }
            if (model.CGTTransferTaxCap.Value == true)
            {
                totalCap += model.CGTTransferTax;
            }
            if (model.DSTCompromiseCap.Value == true)
            {
                totalCap += model.DSTCompromise;
            }
            if (model.DSTInterestCap.Value == true)
            {
                totalCap += model.DSTInterest;
            }
            if (model.DSTSurchargeCap.Value == true)
            {
                totalCap += model.DSTSurcharge;
            }
            if (model.DSTTransferTaxCap.Value == true)
            {
                totalCap += model.DSTTransferTax;
            }            
            if (model.TransferTaxCap.Value == true)
            {
                totalCap += model.TransferTax;
            }
            if (model.SurchargeCap.Value == true)
            {
                totalCap += model.Surcharge;
            }
            if (model.InterestCap.Value == true)
            {
                totalCap += model.Interest;
            }
            if (model.ConfirmationFeeCap.Value == true)
            {
                totalCap += model.ConfirmationFee;
            }
            if (model.TransferRegsFeeCap.Value == true)
            {
                totalCap += model.TransferRegsFee;
            }
            if (model.RealPropertyTaxCap.Value == true)
            {
                totalCap += model.RealPropertyTax;
            }
            if (model.VATCap.Value == true)
            {
                totalCap += model.VAT;
            }
            if (model.EstateTaxCap.Value == true)
            {
                totalCap += model.EstateTax;
            }
            if (model.TitlingCap.Value == true)
            {
                totalCap += model.Titling;
            }
            if (model.CertificationFeeCap.Value == true)
            {
                totalCap += model.CerttificationFee;
            }
            if (model.RelocationCap.Value == true)
            {
                totalCap += model.Relocation;
            }
            if (model.SurveyingCap.Value == true)
            {
                totalCap += model.Surveying;
            }
            if (model.IncidentalExpensesCap.Value == true)
            {
                totalCap += model.IncidentalExpenses;
            }
            entity.TotalCap = totalCap;
        }

        public ValueTask<PsCardItemExtnLandVM> DeleteAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().FirstOrDefaultAsync(f => f.Id == model.Id);

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