using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnVehicleService
    {
        IQueryable<PsCardItemExtnVehicleVM> GetByPsCardItemId(Guid? psCardItemId);
        IQueryable<PsCardItemExtnVehicleVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? transferId);
        ValueTask<PsCardItemExtnVehicleVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemExtnVehicleVM> CreateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicleVM> UpdateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicleVM> DeleteAsync(PsCardItemExtnVehicleVM model, string user, DateTime date);

        void ValidateItemExtnVechiles(Guid? psCardItemId);

        IPsCardItemExtnVehicleRepairService PsCardItemExtnVehicleRepair { get; }
    }

    public class PsCardItemExtnVehicleService : IPsCardItemExtnVehicleService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemExtnVehicleVM> _exceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnVehicleValidator _psCardItemExtnValidator;
        private readonly IPsCardItemExtnSharedService _psCardItemExtnSharedService;
        private readonly IPsCardItemExtnVehicleRepairService _psCardItemExtnVehicleRepairService;        

        public PsCardItemExtnVehicleService(AppManEntities db,
            IExceptionService<PsCardItemExtnVehicleVM> exceptionService,
            IPsCardItemTransactionService psCardItemTransactionService,
            IPsCardItemExtnVehicleValidator psCardItemExtnValidator,
            IPsCardItemExtnSharedService psCardItemExtnSharedService,
            IPsCardItemExtnVehicleRepairService psCardItemExtnVehicleRepairService)
        {
            _db = db;
            _exceptionService = exceptionService;
            _psCardItemTransactionService = psCardItemTransactionService;
            _psCardItemExtnValidator = psCardItemExtnValidator;
            _psCardItemExtnSharedService = psCardItemExtnSharedService;
            _psCardItemExtnVehicleRepairService = psCardItemExtnVehicleRepairService;
        }

        public IPsCardItemExtnVehicleRepairService PsCardItemExtnVehicleRepair => _psCardItemExtnVehicleRepairService;

        private Expression<Func<PsCardItemExtnVehicle, PsCardItemExtnVehicleVM>> GetProjection()
        {
            return s => new PsCardItemExtnVehicleVM
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
                YearModel = s.YearModel,
                PlateNo = s.PlateNo,
                BodyNo = s.BodyNo,
                EngineNo = s.EngineNo,
                ChasisNo = s.ChasisNo,
                Color = s.Color,
                CRN = s.CRN,
                CRDate = s.CRDate,
                MVFileNo = s.MVFileNo,
                OrNo = s.OrNo,
                OrDate = s.OrDate,
                NetWeight = s.NetWeight,
                InsPolicyNo = s.InsPolicyNo,
                ConductionNo = s.ConductionNo
            };
        }

        public IQueryable<PsCardItemExtnVehicleVM> GetByPsCardItemId(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId)
                .Select(GetProjection());
            return data;
        }

        public IQueryable<PsCardItemExtnVehicleVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? transferId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId && w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == transferId))
                .Select(GetProjection());
            return data;
        }

        public ValueTask<PsCardItemExtnVehicleVM> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().AsNoTracking()
                .Where(w => w.Id == id)
                .Select(GetProjection())
                .FirstOrDefaultAsync();
            return data;
        });

        public void ValidateItemExtnVechiles(Guid? psCardItemId)
        {
            if (_db.PsCardItems.Any(a => a.Id == psCardItemId && a.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Count() < a.Qty))
            {
                throw new InvalidValueException("Incomplete item quantity contents detected.");
            }
        }

        private void ValidateFields(PsCardItemExtnVehicle model)
        {
            if (string.IsNullOrWhiteSpace(model.SeriesNo))
            {
                throw new InvalidValueException("Series Number is required!");
            }
            if (string.IsNullOrWhiteSpace(model.PlateNo))
            {
                throw new InvalidValueException("Plate Number is required!");
            }
        }

        public ValueTask<PsCardItemExtnVehicleVM> CreateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            //ValidatorService.ValidateModel<PsCardItemExtn>(model);

            _psCardItemExtnValidator.ValidateOnCreate(model);

            var psCardItem = await _db.PsCardItems.FirstOrDefaultAsync(f => f.Id == model.PsCardItemId);
            var itemQty = (int)(psCardItem.Qty ?? 0) + (int)(psCardItem.TransferIn ?? 0);
            var itemExtnCount = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(w => w.PsCardItemId == model.PsCardItemId).Count();

            if (itemExtnCount >= itemQty)
            {
                throw new InvalidValueException($"Cannot create more than {itemQty} record(s).");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnVehicle();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);

            return model;
        });        

        public ValueTask<PsCardItemExtnVehicleVM> UpdateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnValidator.ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().FirstOrDefaultAsync(f => f.Id == model.Id);
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);

            return model;
        });

        public ValueTask<PsCardItemExtnVehicleVM> DeleteAsync(PsCardItemExtnVehicleVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {            
            _psCardItemExtnValidator.ValidateOnDelete(model);

            //using (var transaction = _db.Database.BeginTransaction())
            //{
            //    try
            //    {
                    // Delete References
                    var itemTransactions = _db.PsCardItemTransactions.Where(w => w.PsCardItemExtnId == model.Id);
                    _db.PsCardItemTransactions.RemoveRange(itemTransactions);
                    await _db.SaveChangesAsync();

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().FirstOrDefaultAsync(f => f.Id == model.Id);

                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    _db.PsCardItemExtns.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    _db.PsCardItemExtns.Remove(entity);
                    _db.Entry(entity).State = EntityState.Deleted;
                    await _db.SaveChangesAsync();

            //        transaction.Commit();
            //    }
            //    catch (Exception)
            //    {
            //        // Rollback the transaction if any operation fails
            //        transaction.Rollback();
            //        throw;
            //    }
            //}

            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnVehicle entity, PsCardItemExtnVehicleVM model, Mode mode)
        {
            _psCardItemExtnSharedService.MapModelToEntityFields(entity, model, mode);

            entity.YearModel = model.YearModel;
            entity.PlateNo = model.PlateNo;
            entity.BodyNo = model.BodyNo;
            entity.EngineNo = model.EngineNo;
            entity.ChasisNo = model.ChasisNo;
            entity.Color = model.Color;
            entity.CRN = model.CRN;
            entity.CRDate = model.CRDate;
            entity.MVFileNo = model.MVFileNo;
            entity.OrNo = model.OrNo;
            entity.OrDate = model.OrDate;
            entity.NetWeight = model.NetWeight;
            entity.InsPolicyNo = model.InsPolicyNo;
            entity.ConductionNo = model.ConductionNo;            
        }        
    }
}