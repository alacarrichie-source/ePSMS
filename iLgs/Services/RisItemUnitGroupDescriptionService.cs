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
    public interface IRisItemUnitGroupDescriptionService
    {
        IQueryable<RisItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId);
        ValueTask<RisItemUnitGroupDescription> GetByIdAsync(Guid? id);
        ValueTask<RisItemUnitGroupDescriptionVM> CreateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<RisItemUnitGroupDescriptionVM> UpdateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<RisItemUnitGroupDescriptionVM> DeleteAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date);        
    }

    public class RisItemUnitGroupDescriptionService : IRisItemUnitGroupDescriptionService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemUnitGroupDescriptionVM> _vmExceptionService = new ExceptionService<RisItemUnitGroupDescriptionVM>();
        private readonly IExceptionService<RisItemUnitGroupDescription> _exceptionService = new ExceptionService<RisItemUnitGroupDescription>();
        
        public RisItemUnitGroupDescriptionService(AppManEntities db)
        {
            _db = db;            
        }        

        public ValueTask<RisItemUnitGroupDescription> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RisItemUnitGroupDescriptions.FindAsync(id);
            return data;
        });

        public IQueryable<RisItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisItemUnitGroupDescriptions.Where(w => w.UnitGroupId == unitGroupId)
                .Select(s => new RisItemUnitGroupDescriptionVM
                {
                    Id = s.Id,
                    UnitGroupId = s.UnitGroupId,
                    Description = s.Description,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RisItemUnitGroupDescriptionVM> CreateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            RisItemUnitGroupDescription entity = new RisItemUnitGroupDescription()
            {
                Id = model.Id,
                UnitGroupId = model.UnitGroupId,
                Description = model.Description,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RisItemUnitGroupDescriptions.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupDescriptionVM> DeleteAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItemUnitGroupDescription entity = await _db.RisItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RisItemUnitGroupDescriptions.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupDescriptionVM> UpdateAsync(RisItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisItemUnitGroupDescription entity = await _db.RisItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.Description = model.Description;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}