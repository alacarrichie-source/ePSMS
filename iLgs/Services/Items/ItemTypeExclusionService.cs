using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Items
{
    public interface IItemTypeExclusionService
    {
        IQueryable<ItemTypeExclusion> GetAll(Guid? itemUserId);
        ValueTask<ItemTypeExclusion> CreateAsync(ItemTypeExclusion model, string user, DateTime date);
        ValueTask<ItemTypeExclusion> UpdateAsync(ItemTypeExclusion model, string user, DateTime date);
        ValueTask<ItemTypeExclusion> DeleteAsync(ItemTypeExclusion model, string user, DateTime date);
    }

    public class ItemTypeExclusionService : BaseValidator, IItemTypeExclusionService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<ItemTypeExclusion> _exceptionService = new ExceptionService<ItemTypeExclusion>();

        public ItemTypeExclusionService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<ItemTypeExclusion>(propertyName);
        }

        public IQueryable<ItemTypeExclusion> GetAll(Guid? itemUserId)
        {
            var data = _db.ItemTypeExclusions.Include(i => i.ItemType).Where(w => w.ItemUserId == itemUserId).AsNoTracking();                
            return data;
        }
        
        public ValueTask<ItemTypeExclusion> CreateAsync(ItemTypeExclusion model, string user, DateTime date) => _exceptionService.TryCatch(async () =>        
        {

            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new ItemTypeExclusion();
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.ItemTypeExclusions.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });
        
        public ValueTask<ItemTypeExclusion> UpdateAsync(ItemTypeExclusion model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {

            ValidateIfNull(model);
            ValidateFields(model, Mode.EDIT);

            var entity = await _db.ItemTypeExclusions.FindAsync(model.Id);
            ValidateRecord(entity);            

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.ItemTypeExclusions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ItemTypeExclusion> DeleteAsync(ItemTypeExclusion model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.ItemTypeExclusions.FindAsync(model.Id);
            ValidateRecord(entity);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.ItemTypeExclusions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.ItemTypeExclusions.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(ItemTypeExclusion entity, ItemTypeExclusion model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.ItemUserId = model.ItemUserId;
            entity.ItemTypeId = model.ItemTypeId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateFields(ItemTypeExclusion model, Mode mode)
        {
            //if (!model.AsOf.HasValue)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.AsOf)), "Field is required.");
            //}

            //if (mode == Mode.POST)
            //{
            //    if (string.IsNullOrWhiteSpace(model.Chairman))
            //    {
            //        var chairman = _getDisplayName(nameof(model.Chairman));
            //        _imex.UpsertDataList(chairman, $"{chairman} Field is required.");
            //    }

            //    if (string.IsNullOrWhiteSpace(model.ChairmanTitle))
            //    {
            //        var chairmanTitle = _getDisplayName(nameof(model.ChairmanTitle));
            //        _imex.UpsertDataList(chairmanTitle, $"{chairmanTitle} Field is required.");
            //    }

            //    if (string.IsNullOrWhiteSpace(model.ViceChairman))
            //    {
            //        var vice = _getDisplayName(nameof(model.ViceChairman));
            //        _imex.UpsertDataList(vice, $"{vice} Field is required.");
            //    }

            //    if (string.IsNullOrWhiteSpace(model.ViceChairmanTitle))
            //    {
            //        var viceTitle = _getDisplayName(nameof(model.ViceChairmanTitle));
            //        _imex.UpsertDataList(viceTitle, $"{viceTitle} Field is required.");
            //    }

            //    if (string.IsNullOrWhiteSpace(model.Member))
            //    {
            //        var member = _getDisplayName(nameof(model.Member));
            //        _imex.UpsertDataList(member, $"{member} Field is required.");
            //    }

            //    if (string.IsNullOrWhiteSpace(model.MemberTitle))
            //    {
            //        var memberTitle = _getDisplayName(nameof(model.MemberTitle));
            //        _imex.UpsertDataList(memberTitle, $"{memberTitle} Field is required.");
            //    }

            //    if (string.IsNullOrWhiteSpace(model.Officer))
            //    {
            //        var officer = _getDisplayName(nameof(model.Officer));
            //        _imex.UpsertDataList(officer, $"{officer} Field is required.");
            //    }

            //    if (string.IsNullOrWhiteSpace(model.OfficerTitle))
            //    {
            //        var officerTitle = _getDisplayName(nameof(model.OfficerTitle));
            //        _imex.UpsertDataList(officerTitle, $"{officerTitle} Field is required.");
            //    }
            //}

            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(ItemTypeExclusion model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(ItemTypeExclusion entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

    }
}