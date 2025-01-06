using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestItemUnitGroupDescriptionService
    {
        IQueryable<RequestItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId);
        ValueTask<RequestItemUnitGroupDescription> GetByIdAsync(Guid? id);
        ValueTask<RequestItemUnitGroupDescriptionVM> CreateAsync(RequestItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<RequestItemUnitGroupDescriptionVM> UpdateAsync(RequestItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<RequestItemUnitGroupDescriptionVM> DeleteAsync(RequestItemUnitGroupDescriptionVM model, string user, DateTime date);
    }

    public class RequestItemUnitGroupDescriptionService : IRequestItemUnitGroupDescriptionService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RequestItemUnitGroupDescriptionVM> _vmExceptionService = new ExceptionService<RequestItemUnitGroupDescriptionVM>();
        private readonly IExceptionService<RequestItemUnitGroupDescription> _exceptionService = new ExceptionService<RequestItemUnitGroupDescription>();

        public RequestItemUnitGroupDescriptionService(AppManEntities db)
        {
            _db = db;
        }


        public ValueTask<RequestItemUnitGroupDescription> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RequestItemUnitGroupDescriptions.FindAsync(id);
            return data;
        });

        public IQueryable<RequestItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RequestItemUnitGroupDescriptions.Where(w => w.RequestItemUnitGroupId == unitGroupId)
                .Select(s => new RequestItemUnitGroupDescriptionVM
                {
                    Id = s.Id,
                    RequestItemUnitGroupId = s.RequestItemUnitGroupId,
                    RisItemUnitGroupDescriptionId = s.RisItemUnitGroupDescriptionId,
                    RisItemUnitGroupDescription = s.RisItemUnitGroupDescription,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RequestItemUnitGroupDescriptionVM> CreateAsync(RequestItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RequestItemUnitGroupDescription()
            {
                Id = model.Id,
                RequestItemUnitGroupId = model.RequestItemUnitGroupId,
                RisItemUnitGroupDescriptionId = model.RisItemUnitGroupDescriptionId,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RequestItemUnitGroupDescriptions.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestItemUnitGroupDescriptionVM> DeleteAsync(RequestItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.RequestItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RequestItemUnitGroupDescriptions.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestItemUnitGroupDescriptionVM> UpdateAsync(RequestItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.RequestItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.RequestItemUnitGroupId = model.RequestItemUnitGroupId;
            entity.RisItemUnitGroupDescriptionId = model.RisItemUnitGroupDescriptionId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}