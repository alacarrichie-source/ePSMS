using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Requisition
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
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemUnitGroupVM> _vmExceptionService = new ExceptionService<RisItemUnitGroupVM>();
        private readonly IExceptionService<RisItemUnitGroup> _exceptionService = new ExceptionService<RisItemUnitGroup>();
        //private readonly IRequestService _requestService;        
        private readonly IRisItemUnitGroupValidator _validator;

        public RisItemUnitGroupService(AppManEntities db)
        {
            _db = db;
            //_requestService = new RequestService(db);
            _validator = new RisItemUnitGroupValidator(_db);            
        }        

        public ValueTask<RisItemUnitGroup> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RisItemUnitGroups.FindAsync(id);
            return data;
        });

        public IQueryable<RisItemUnitGroupVM> GetByRisId(Guid? risId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisItemUnitGroups.Where(w => w.RisId == risId)
                .Select(s => new RisItemUnitGroupVM
                {
                    Id = s.Id,
                    RisId = s.RisId,     
                    SetLotNo = s.SetLotNo,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });

        public ValueTask<RisItemUnitGroupVM> CreateAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnCreate(model);            
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.SetLotNo = SetLotNo(model.RisId);

            RisItemUnitGroup entity = new RisItemUnitGroup()
            {
                Id = model.Id,
                RisId = model.RisId,
                SetLotNo = model.SetLotNo,
                Qty = model.Qty,
                Unit = model.Unit,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RisItemUnitGroups.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupVM> DeleteAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);

            RisItemUnitGroup entity = await _db.RisItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroups.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RisItemUnitGroups.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemUnitGroupVM> UpdateAsync(RisItemUnitGroupVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnUpdate(model);

            RisItemUnitGroup entity = await _db.RisItemUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            
            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            entity.RisId = model.RisId;
            entity.SetLotNo = model.SetLotNo;
            entity.Unit = model.Unit;
            entity.Qty = model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItemUnitGroups.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();            

            return model;
        });

        private string SetLotNo(Guid? risId)
        {
            var count = _db.RisItemUnitGroups.Where(w => w.RisId == risId).Count();
            count++;
            return count.ToString();            
        }
    }
}