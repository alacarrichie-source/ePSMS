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
    public interface IPsCardItemTransferItemOtherService
    {
        List<PsCardItemExtnOtherVM> GetCardItemExtns(Guid? psCardTransferId);
        
        ValueTask<PsCardItemExtnOtherVM> CreateAsync(PsCardItemExtnOtherVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnOtherVM> UpdateAsync(PsCardItemExtnOtherVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnOtherVM> DeleteAsync(PsCardItemExtnOtherVM model, string user, DateTime date);
    }

    public class PsCardItemTransferItemOtherService : BaseValidator, IPsCardItemTransferItemOtherService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<PsCardItemExtnOtherVM> _exceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnOtherValidator _psCardItemExtnOtherValidator;
        private readonly IPsCardItemTransferItemSharedService _psCardItemTransferItemSharedService;

        public PsCardItemTransferItemOtherService(AppManEntities db,
            IExceptionService<PsCardItemExtnOtherVM> exceptionService,
            IPsCardItemTransactionService psCardItemTransactionService,
            IPsCardItemExtnOtherValidator psCardItemExtnOtherValidator,
            IPsCardItemTransferItemSharedService psCardItemTransferItemSharedService)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnOtherVM>(propertyName);
            _exceptionService = exceptionService;
            _psCardItemTransactionService = psCardItemTransactionService;
            _psCardItemExtnOtherValidator = psCardItemExtnOtherValidator;
            _psCardItemTransferItemSharedService = psCardItemTransferItemSharedService;
        }

        public List<PsCardItemExtnOtherVM> GetCardItemExtns(Guid? psCardTransferId)
        {
            var data = _db.Database.SqlQuery<PsCardItemExtnOtherVM>("Exec PsCardItemExtnTransferItem_GetItemExtnOthersByTransferId {0}", psCardTransferId).ToList();
            return data;
        }
        

        public ValueTask<PsCardItemExtnOtherVM> CreateAsync(PsCardItemExtnOtherVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {            
            _psCardItemExtnOtherValidator.ValidateOnCreate(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == model.PsCardItemId);            
            if (itemExtns.Any(a => a.SerialNo == model.SerialNo))
            {
                throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnOther();
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

        public ValueTask<PsCardItemExtnOtherVM> UpdateAsync(PsCardItemExtnOtherVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnOtherValidator.ValidateOnUpdate(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            var itemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.PsCardItemExtnId && w.SerialNo == model.SerialNo).FirstOrDefaultAsync();
            if (itemExtn != null)
            {
                throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>()
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

        public void MapModelToEntityFields(PsCardItemExtnOther entity, PsCardItemExtnOtherVM model, Mode mode)
        {
            _psCardItemTransferItemSharedService.MapModelToEntityFields(entity, model, mode);

            entity.SerialNo = model.SerialNo;         
        }

        public ValueTask<PsCardItemExtnOtherVM> DeleteAsync(PsCardItemExtnOtherVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnOtherValidator.ValidateOnDelete(model);
            _psCardItemTransferItemSharedService.ValidateIfTransit(model.TransferId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefault(f => f.Id == model.Id);

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