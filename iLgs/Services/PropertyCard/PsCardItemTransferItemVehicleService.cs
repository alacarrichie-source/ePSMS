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
    public interface IPsCardItemTransferItemVehicleService
    {
        List<PsCardItemExtnVehicleVM> GetCardItemExtns(Guid? psCardTransferId);

        ValueTask<PsCardItemExtnVehicleVM> CreateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicleVM> UpdateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicleVM> DeleteAsync(PsCardItemExtnVehicleVM model, string user, DateTime date);
    }

    public class PsCardItemTransferItemVehicleService : BaseValidator, IPsCardItemTransferItemVehicleService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<PsCardItemExtnVehicleVM> _exceptionService = new ExceptionService<PsCardItemExtnVehicleVM>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnVehicleValidator _psCardItemExtnVehicleValidator;

        public PsCardItemTransferItemVehicleService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnVehicleVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnVehicleValidator = new PsCardItemExtnVehicleValidator(_db);
        }

        public List<PsCardItemExtnVehicleVM> GetCardItemExtns(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnVehicleVM>("Exec PsCardItemExtnTransferItem_GetItemExtnVehiclesByTransferId {0}", psCardTransferId).ToList();
            return data;
        }


        public ValueTask<PsCardItemExtnVehicleVM> CreateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnVehicleValidator.ValidateOnCreate(model);

            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(w => w.PsCardItemId == model.PsCardItemId);

            if (itemExtns.Any(a => a.ConductionNo == model.ConductionNo))
            {
                throw new RecordAlreadyExistsException($"Conduction Sticker No. {model.ConductionNo} already exists!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnVehicle();
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

        public ValueTask<PsCardItemExtnVehicleVM> UpdateAsync(PsCardItemExtnVehicleVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnVehicleValidator.ValidateOnUpdate(model);
            var itemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.PsCardItemExtnId && w.ConductionNo == model.ConductionNo).FirstOrDefaultAsync();

            if (itemExtn != null)
            {
                throw new RecordAlreadyExistsException($"Conduction Sticker No. {model.ConductionNo} already exists!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>()
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

        public void MapModelToEntityFields(PsCardItemExtnVehicle entity, PsCardItemExtnVehicleVM model, Mode mode)
        {

            if (mode == Mode.ADD)
            {
                entity.Id = (Guid)model.PsCardItemExtnId;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.PsCardItemId = model.PsCardItemId;
            entity.SetLotNo = model.SetLotNo;
            entity.SetLotQtyNo = model.SetLotQtyNo;
            entity.ContentNo = model.ContentNo;
            entity.Condition = model.Condition;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            entity.Condition = model.Condition;
            entity.SubLocation = model.SubLocation;
            entity.Annex = model.Annex;
            entity.OldPropNo = model.OldPropNo;
            entity.UpcomingOfficer = model.UpcomingOfficer;
        }

        public ValueTask<PsCardItemExtnVehicleVM> DeleteAsync(PsCardItemExtnVehicleVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnVehicleValidator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = _db.PsCardItemExtns.OfType<PsCardItemExtnVehicle>().FirstOrDefault(f => f.Id == model.Id);

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