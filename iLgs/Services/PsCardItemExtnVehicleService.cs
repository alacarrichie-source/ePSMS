using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services
{
    public interface IPsCardItemExtnVehicleService
    {
        IQueryable<PsCardItemExtnVehicle> GetByPsCardItemId(Guid? psCardItemId);
        ValueTask<PsCardItemExtnVehicle> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemExtnVehicle> CreateAsync(PsCardItemExtnVehicle model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicle> UpdateAsync(PsCardItemExtnVehicle model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicle> DeleteAsync(PsCardItemExtnVehicle model, string user, DateTime date);

        void ValidateItemExtnVechiles(Guid? psCardItemId);
    }

    public class PsCardItemExtnVehicleService : IPsCardItemExtnVehicleService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemExtnVehicle> _exceptionService = new ExceptionService<PsCardItemExtnVehicle>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;

        public PsCardItemExtnVehicleService(AppManEntities db)
        {
            _db = db;
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
        }

        public IQueryable<PsCardItemExtnVehicle> GetByPsCardItemId(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(w => w.PsCardItemId == psCardItemId);
            return data;
        }

        public ValueTask<PsCardItemExtnVehicle> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(w => w.Id == id).FirstOrDefaultAsync();
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
        
        public ValueTask<PsCardItemExtnVehicle> CreateAsync(PsCardItemExtnVehicle model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidatorService.ValidateModel<PsCardItemExtn>(model);

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
        

        public ValueTask<PsCardItemExtnVehicle> UpdateAsync(PsCardItemExtnVehicle model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {            
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

        public ValueTask<PsCardItemExtnVehicle> DeleteAsync(PsCardItemExtnVehicle model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            // check in Par/Ics
            if (await _db.IcsParItems.AnyAsync(a => a.PsCardItemExtnId == model.Id))
            {
                throw new RecordAlreadyExistsException("PAR/ICS already exists for this record, cannot delete!");
            }

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

            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnVehicle entity, PsCardItemExtnVehicle model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

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
            entity.ParReissuance = model.ParReissuance;
            entity.Condition = model.Condition;
            entity.SubLocation = model.SubLocation;
            entity.ConductionNo = model.ConductionNo;
            entity.ContentNo = model.ContentNo;
            entity.CustItemNo = model.CustItemNo;
            entity.PsCardItemId = model.PsCardItemId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }
    }
}