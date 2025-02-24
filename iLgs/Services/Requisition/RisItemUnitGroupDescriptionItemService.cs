using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Requisition
{
    public interface IRisItemUnitGroupDescriptionItemService
    {
        IQueryable<RisItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<RisItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        IQueryable<RisItemUnitGroupAvailableVM> GetAvailableUnitGroupItem(Guid? risId);
        ValueTask<RisItemUnitGroupDescriptionItemVM> CreateAsync(RisItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<RisItemUnitGroupDescriptionItemVM> UpdateAsync(RisItemUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<RisItemUnitGroupDescriptionItemVM> DeleteAsync(RisItemUnitGroupDescriptionItemVM model, string user, DateTime date);
    }

    public class RisItemUnitGroupDescriptionItemService : BaseValidator, IRisItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemUnitGroupDescriptionItemVM> _vmExceptionService = new ExceptionService<RisItemUnitGroupDescriptionItemVM>();
        private readonly IExceptionService<RisItemUnitGroupAvailableVM> _vmUnitGroupAvailableExceptionService = new ExceptionService<RisItemUnitGroupAvailableVM>();
        private readonly IExceptionService<RisItemUnitGroupDescriptionItem> _exceptionService = new ExceptionService<RisItemUnitGroupDescriptionItem>();
        private readonly IRisService _risService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public RisItemUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<RisItemUnitGroupDescriptionItemVM>(propertyName);
            _risService = new RisService(_db);
        }

        public ValueTask<RisItemUnitGroupDescriptionItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RisItemUnitGroupDescriptionItems.FindAsync(id);
            return data;
        });

        public IQueryable<RisItemUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisItemUnitGroupDescriptionItems
                .Where(w => w.UnitGroupDescriptionId == unitGroupDescriptionId)
                .AsNoTracking()
                .Select(s => new RisItemUnitGroupDescriptionItemVM
                {
                    Id = s.Id,
                    UnitGroupDescriptionId = s.UnitGroupDescriptionId,
                    RisItemId = s.RisItemId,
                    PsNo = s.RisItem.PsNoDisplay,
                    ItemName = s.RisItem.ItemName,
                    Description = s.RisItem.Description,
                    Unit = s.RisItem.Unit,
                    QtyRequest = s.RisItem.QtyRequest,                    
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public IQueryable<RisItemUnitGroupAvailableVM> GetAvailableUnitGroupItem(Guid? risId) =>
        _vmUnitGroupAvailableExceptionService.TryCatch(() =>
        {
            var data = _db.RisItems
            .Where(w => w.RisId == risId && !w.RisItemUnitGroupDescriptionItems.Any(a => a.RisItemId == w.Id))
            .AsNoTracking()
            .Select(s => new RisItemUnitGroupAvailableVM
            {
                Id = s.Id,
                PsNo = s.PsNoDisplay,
                ItemName = s.ItemName,
                Description = s.Description,
                Unit = s.Unit,
                QtyRequest = s.QtyRequest,
                InsertedDt = s.InsertedDt
            });            
            return data;
        });

        public ValueTask<RisItemUnitGroupDescriptionItemVM> CreateAsync(RisItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnCreate(model);

            if (string.IsNullOrWhiteSpace(model.GridItems))
            {
                throw new InvalidValueException("No selected items, cannot continue!");
            }
            
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var selectedItems = model.GridItems.Split(',');
            foreach (var item in selectedItems)
            {
                model.Id = Guid.NewGuid();
                RisItemUnitGroupDescriptionItem entity = new RisItemUnitGroupDescriptionItem()
                {
                    Id = model.Id,
                    UnitGroupDescriptionId = model.UnitGroupDescriptionId,
                    RisItemId = Guid.Parse(item),
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt
                };

                _db.RisItemUnitGroupDescriptionItems.Add(entity);
            }

            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<RisItemUnitGroupDescriptionItemVM> DeleteAsync(RisItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItemUnitGroupDescriptionItem entity = await _db.RisItemUnitGroupDescriptionItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroupDescriptionItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RisItemUnitGroupDescriptionItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupDescriptionItemVM> UpdateAsync(RisItemUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItemUnitGroupDescriptionItem entity = await _db.RisItemUnitGroupDescriptionItems.FindAsync(model.Id);

            entity.RisItemId = model.RisItemId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroupDescriptionItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });

        public void ValidateOnCreate(RisItemUnitGroupDescriptionItemVM model)
        {
            ValidateModel(model);
            ValidateIfPosted(model);
            //ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnUpdate(RisItemUnitGroupDescriptionItemVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted(model);
            //ValidateFieldsOnCreateUpdate(model);
        }

        public void ValidateOnDelete(RisItemUnitGroupDescriptionItemVM model)
        {
            ValidateModel(model);
            ValidateRecord(model.Id);
            ValidateIfPosted(model);
        }

        public void ValidateIfPosted(RisItemUnitGroupDescriptionItemVM model)
        {
            var isPosted = _risService.IsPosted(model);
            if (isPosted)
            {
                throw new RecordAlreadyPostedException();
            }
        }

        public void ValidateFieldsOnCreateUpdate(RisItemUnitGroupDescriptionItemVM model)
        {
            var ex = new InvalidModelException();

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            }

            ex.ThrowIfContainsErrors();
        }


        private void ValidateRecord(Guid id)
        {
            if (!_db.RisItemUnitGroupDescriptionItems.Any(a => a.Id == id))
            {
                throw new NotFoundException(id);
            }
        }

        private static void ValidateModel(RisItemUnitGroupDescriptionItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }
    }
}