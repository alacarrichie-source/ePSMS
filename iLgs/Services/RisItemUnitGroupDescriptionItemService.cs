using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
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

    public class RisItemUnitGroupDescriptionItemService : IRisItemUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemUnitGroupDescriptionItemVM> _vmExceptionService = new ExceptionService<RisItemUnitGroupDescriptionItemVM>();
        private readonly IExceptionService<RisItemUnitGroupAvailableVM> _vmUnitGroupAvailableExceptionService = new ExceptionService<RisItemUnitGroupAvailableVM>();
        private readonly IExceptionService<RisItemUnitGroupDescriptionItem> _exceptionService = new ExceptionService<RisItemUnitGroupDescriptionItem>();
        
        public RisItemUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;            
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
            var data = _db.RisItemUnitGroupDescriptionItems.Where(w => w.UnitGroupDescriptionId == unitGroupDescriptionId)
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
            var data = _db.RisItems.Where(w => w.RisId == risId && !w.RisItemUnitGroupDescriptionItems.Any(a => a.RisItemId == w.Id))
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
    }
}