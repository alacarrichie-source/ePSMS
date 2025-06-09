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
        private readonly IExceptionService<PsCardItemExtnBldgVM> _exceptionService = new ExceptionService<PsCardItemExtnBldgVM>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnBldgValidator _psCardItemExtnBldgValidator;
        private readonly IPsCardItemTransferItemService _psCardItemTransferItemService;

        public PsCardItemTransferItemBldgService(AppManEntities db, IPsCardItemTransferItemService psCardItemTransferItemService)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnBldgVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnBldgValidator = new PsCardItemExtnBldgValidator(_db);
            _psCardItemTransferItemService = psCardItemTransferItemService;
        }

        public List<PsCardItemExtnBldgVM> GetCardItemExtns(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnBldgVM>("Exec PsCardItemExtnTransferItem_GetItemExtnBuildingsByTransferId {0}", psCardTransferId).ToList();
            return data;
        }


        public ValueTask<PsCardItemExtnBldgVM> CreateAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBldgValidator.ValidateOnCreate(model);
            _psCardItemTransferItemService.ValidateIfTransit(model.TransferId);

            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().Where(w => w.PsCardItemId == model.PsCardItemId);
            //if (itemExtns.Any(a => a.SerialNo == model.SerialNo))
            //{
            //    throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            //}

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

            _db.PsCardItemExtns.Add(entity);
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.PsCardItemExtnId, model.PsCardItemId, "CARD", user, date);
            return model;
        });

        public ValueTask<PsCardItemExtnBldgVM> UpdateAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBldgValidator.ValidateOnUpdate(model);
            _psCardItemTransferItemService.ValidateIfTransit(model.TransferId);

            var itemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.PsCardItemExtnId).FirstOrDefaultAsync();

            //if (itemExtn != null)
            //{
            //    throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>()
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

        public void MapModelToEntityFields(PsCardItemExtnBuilding entity, PsCardItemExtnBldgVM model, Mode mode)
        {
            _psCardItemTransferItemService.MapModelToEntityFields(entity, model, mode);

            entity.Longitude = model.Longitude;
            entity.Latitude = model.Latitude;
            entity.Address = model.Address;
            entity.BuildingItem = model.BuildingItem;
            entity.BuildingType = model.BuildingType;
            entity.StartDate = model.StartDate;
            entity.TargetDate = model.TargetDate;
            entity.CompletionDate = model.CompletionDate;
            entity.PercentComplete = model.PercentComplete;
            entity.Area = model.Area;
            entity.AppraisedValue = model.AppraisedValue;
            entity.ProjectName = model.ProjectName;
            entity.PhaseNo = model.PhaseNo;
            entity.PhaseAmountCo = model.PhaseAmountCo;
            entity.PhaseAmountMooe = model.PhaseAmountMooe;
            entity.Status = model.Status;
        }

        public ValueTask<PsCardItemExtnBldgVM> DeleteAsync(PsCardItemExtnBldgVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnBldgValidator.ValidateOnDelete(model);
            _psCardItemTransferItemService.ValidateIfTransit(model.TransferId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = _db.PsCardItemExtns.OfType<PsCardItemExtnBuilding>().FirstOrDefault(f => f.Id == model.Id);

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