using FluentValidation;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.AIRs_
{
    public interface IAirItemExtnVehicleService
    {
        IQueryable<AIRItemExtnVehicle> GetByAirItemId(Guid? airItemId);
        ValueTask<AIRItemExtnVehicle> GetByIdAsync(Guid? id);

        ValueTask<AIRItemExtnVehicle> CreateAsync(AIRItemExtnVehicle model, string user, DateTime date);
        ValueTask<AIRItemExtnVehicle> UpdateAsync(AIRItemExtnVehicle model, string user, DateTime date);
        ValueTask<AIRItemExtnVehicle> DeleteAsync(AIRItemExtnVehicle model, string user, DateTime date);

        ValueTask<bool> IsPostedAsync(Guid? airItemId);
        ValueTask<bool> IsUniquePlateNoAddAsync(Guid? airItemId, string plateNo);
        ValueTask<bool> IsUniquePlateNoUpdateAsync(Guid? id, Guid? airItemId, string plateNo);
        ValueTask<bool> IsValidItemQty(Guid? airItemId);
        void ValidateItemExtnVechiles(Guid? airItemId);
    }

    public class AirItemExtnVehicleService : BaseValidator, IAirItemExtnVehicleService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<AIRItemExtnVehicle> _exceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IAirAbstractService _airService;

        public AirItemExtnVehicleService(AppManEntities db, IExceptionService<AIRItemExtnVehicle> exceptionService, IAirAbstractService airService)
        {
            _db = db;
            _exceptionService = exceptionService;
            _airService = airService;
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianIIRUP>(propertyName);
        }

        public IQueryable<AIRItemExtnVehicle> GetByAirItemId(Guid? airItemId)
        {
            var data = _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItemId == airItemId);
            return data;
        }

        public ValueTask<AIRItemExtnVehicle> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        public void ValidateItemExtnVechiles(Guid? airItemId)
        {
            if (_db.AIRItems.Any(a => a.Id == airItemId && a.AIRItemExtns.OfType<AIRItemExtnVehicle>().Count() < a.Qty))
            {
                throw new InvalidValueException("Incomplete item quantitny contents detected.");
            }
        }
        
        public ValueTask<AIRItemExtnVehicle> CreateAsync(AIRItemExtnVehicle model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {            
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            var entity = new AIRItemExtnVehicle()
            {
                Id = model.Id,
                ContentNo = model.ContentNo,
                CustItemNo = model.CustItemNo,
                IsAutoGen = model.IsAutoGen,
                AIRItemId = model.AIRItemId,
                SeriesNo = model.SeriesNo,
                YearModel = model.YearModel,
                PlateNo = model.PlateNo,
                BodyNo = model.BodyNo,
                EngineNo = model.EngineNo,
                ChasisNo = model.ChasisNo,
                Color = model.Color,
                CRN = model.CRN,
                CRDate = model.CRDate,
                MVFileNo = model.MVFileNo,
                OrNo = model.OrNo,
                OrDate = model.OrDate,
                NetWeight = model.NetWeight,
                InsPolicyNo = model.InsPolicyNo,
                SubLocation = model.SubLocation,
                ConductionNo = model.ConductionNo,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt,
                SetLotNo = model.SetLotNo,
                SetLotQtyNo = model.SetLotQtyNo
            };

            _db.AIRItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemExtnVehicle> DeleteAsync(AIRItemExtnVehicle model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {            
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AIRItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AIRItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AIRItemExtnVehicle> UpdateAsync(AIRItemExtnVehicle model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.ContentNo = model.ContentNo;
            entity.CustItemNo = model.CustItemNo;
            entity.IsAutoGen = model.IsAutoGen;
            entity.SeriesNo = model.SeriesNo;
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
            entity.SubLocation = model.SubLocation;
            entity.ConductionNo = model.ConductionNo;
            entity.SetLotNo = model.SetLotNo;
            entity.SetLotQtyNo = model.SetLotQtyNo;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.AIRItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public async ValueTask<bool> IsPostedAsync(Guid? airItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == airItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public async ValueTask<bool> IsUniquePlateNoAddAsync(Guid? airItemId, string plateNo)
        {
            return (!await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItemId == airItemId && w.PlateNo == plateNo).AnyAsync());
        }

        public async ValueTask<bool> IsUniquePlateNoUpdateAsync(Guid? id, Guid? airItemId, string plateNo)
        {
            return (!await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.Id != id && w.AIRItemId == airItemId && w.PlateNo == plateNo).AnyAsync());
        }

        public async ValueTask<bool> IsValidItemQty(Guid? airItemId)
        {
            var airItemQty = (int)(await _db.AIRItems.FirstOrDefaultAsync(f => f.Id == airItemId)).Qty;
            var airItemExtnCount = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItemId == airItemId).CountAsync();

            return !(airItemExtnCount >= airItemQty);
        }
        
        private void ValidateFields(AIRItemExtnVehicle model, Mode mode)
        {
            if (!model.YearModel.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.YearModel)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.ConductionNo))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.ConductionNo)), "Field is required.");
            }            

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(AIRItemExtnVehicle model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(AIRItemExtnVehicle entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateIfPosted(AIRItemExtnVehicle entity)
        {
            //if (entity.PostedDt != null)
            //{
            //    var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
            //    throw new RecordAlreadyPostedException(msg);
            //}
        }

        private void ValidateIfNotPosted(AIRItemExtnVehicle entity)
        {
            //if (entity.PostedDt == null)
            //{
            //    throw new RecordNotYetPostedException($"Record is not yet posted!");
            //}
        }

        public void ValidateIfPosted(Guid risId)
        {
            var isPosted = _airService.IsPosted(risId);
            if (isPosted)
            {
                throw new RecordAlreadyPostedException();
            }
        }
    }
}