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

        public PsCardItemTransferItemLandService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnLandVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnLandValidator = new PsCardItemExtnLandValidator(_db);
        }

        public List<PsCardItemExtnLandVM> GetCardItemExtns(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnLandVM>("Exec PsCardItemExtnTransferItem_GetItemExtnLandByTransferId {0}", psCardTransferId).ToList();
            return data;
        }


        public ValueTask<PsCardItemExtnLandVM> CreateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnCreate(model);

            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().Where(w => w.PsCardItemId == model.PsCardItemId);

            if (itemExtns.Any(a => a.PIN == model.PIN))
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

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.PsCardItemExtnId, model.PsCardItemId, "CARD", user, date);
            return model;
        });

        public ValueTask<PsCardItemExtnLandVM> UpdateAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnUpdate(model);
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

        public ValueTask<PsCardItemExtnLandVM> DeleteAsync(PsCardItemExtnLandVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnLandValidator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = _db.PsCardItemExtns.OfType<PsCardItemExtnLand>().FirstOrDefault(f => f.Id == model.Id);

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