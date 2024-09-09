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
    public interface IRisItemUnitGroupService
    {
        IQueryable<RisItemUnitGroupVM> GetByRisId(Guid? risId);
        ValueTask<RisItemUnitGroup> GetByIdAsync(Guid? id);
        ValueTask<RisItemUnitGroupVM> CreateAsync(RisItemUnitGroupVM model, string user, DateTime date);
        ValueTask<RisItemUnitGroupVM> UpdateAsync(RisItemUnitGroupVM model, string user, DateTime date);
        ValueTask<RisItemUnitGroupVM> DeleteAsync(RisItemUnitGroupVM model, string user, DateTime date);
    }

    public class RisItemUnitGroupService : IRisItemUnitGroupService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemUnitGroupVM> _vmExceptionService = new ExceptionService<RisItemUnitGroupVM>();
        private readonly IExceptionService<RisItemUnitGroup> _exceptionService = new ExceptionService<RisItemUnitGroup>();
        private readonly IRisService _risService;
        private readonly IRequestService _requestService;

        public RisItemUnitGroupService(AppManEntities db)
        {
            this.db = db;
            _risService = new RisService(db);
            _requestService = new RequestService(db);
        }
        
        public ValueTask<RisItemUnitGroup> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await db.RisItemUnitGroups.FindAsync(id);
            return data;
        });

        public IQueryable<RisItemUnitGroupVM> GetByRisId(Guid? risId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = db.RisItemUnitGroups.Where(w => w.RisId == risId)
                .Select(s => new RisItemUnitGroupVM
                {
                    Id = s.Id,
                    RisId = s.RisId,     
                    Qty = s.Qty,
                    Unit = s.Unit,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });

        public ValueTask<RisItemUnitGroupVM> CreateAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (await _risService.IsPostedAsync((Guid)model.RisId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            RisItemUnitGroup entity = new RisItemUnitGroup()
            {
                Id = model.Id,
                RisId = model.RisId,
                Qty = model.Qty,
                Unit = model.Unit,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.RisItemUnitGroups.Add(entity);
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupVM> DeleteAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            RisItemUnitGroup entity = await db.RisItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }
            
            if (await _risService.IsPostedAsync((Guid)model.RisId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }
            
            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisItemUnitGroups.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RisItemUnitGroups.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupVM> UpdateAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            RisItemUnitGroup entity = await db.RisItemUnitGroups.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (await _risService.IsPostedAsync((Guid)model.RisId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            entity.RisId = model.RisId;
            entity.Unit = model.Unit;
            entity.Qty = model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RisItemUnitGroups.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return model;
        });        
    }
}