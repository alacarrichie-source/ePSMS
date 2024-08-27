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
    public interface IPsCardItemLocationService
    {
        IQueryable<PsCardItemLocation> GetAllByPsCardItemExtnId(Guid? psCardItemExtnId);
        ValueTask<PsCardItemLocation> GetByIdAsync(Guid? id);
        ValueTask<PsCardItemLocation> CreateAsync(PsCardItemLocation model, string user, DateTime date);
        ValueTask<PsCardItemLocation> UpdateAsync(PsCardItemLocation model, string user, DateTime date);
        ValueTask<PsCardItemLocation> DeleteAsync(PsCardItemLocation model, string user, DateTime date);
    }

    public class PsCardItemLocationService : IPsCardItemLocationService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<PsCardItemLocation> _exceptionService = new ExceptionService<PsCardItemLocation>();

        public PsCardItemLocationService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<PsCardItemLocation> GetAllByPsCardItemExtnId(Guid? psCardItemExtnId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.PsCardItemLocations.Where(w => w.PsCardItemExtnId == psCardItemExtnId).AsNoTracking();
            return data;
        });

        public async ValueTask<PsCardItemLocation> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItemLocations
                .Include(i => i.PsCardItemExtn)
                .Include(i => i.PsCardItemIssuance)
                .Where(w => w.Id == id)
                .FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<PsCardItemLocation> CreateAsync(PsCardItemLocation model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            PsCardItemLocation entity = new PsCardItemLocation()
            {
                Id = model.Id,
                PsCardItemExtnId = model.PsCardItemExtnId,
                TransType = model.TransType,
                TransDate = model.TransDate,
                TransId = model.TransId,
                LocationId = model.LocationId,
                LocationCode = model.LocationCode,
                Location = model.Location,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PsCardItemLocations.Add(entity);
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<PsCardItemLocation> UpdateAsync(PsCardItemLocation model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var entity = await GetByIdAsync(model.Id);

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.PsCardItemExtnId = model.PsCardItemExtnId;
            entity.TransType = model.TransType;
            entity.TransDate = model.TransDate;
            entity.TransId = model.TransId;
            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.Location = model.Location;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemLocations.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemLocation> DeleteAsync(PsCardItemLocation model, string user, DateTime date) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            PsCardItemLocation entity = await _db.PsCardItemLocations.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemLocations.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemLocations.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });


    }
}