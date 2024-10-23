using ClosedXML.Excel;
using iLgs.Controllers;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.DollarRate_
{
    public interface IDollarRateService
    {
        IQueryable<DollarRate> GetAll();
        ValueTask<DollarRate> GetByIdAsync(Guid? id);
        ValueTask<Decimal?> GetRateAsync(DateTime? asOfDate);
        ValueTask<DollarRate> PostAsync(Guid id, string user, DateTime date);
        ValueTask<DollarRate> UnPostAsync(Guid id, string user, DateTime date);
        ValueTask<DollarRate> CreateAsync(DollarRate model, string user, DateTime date);
        ValueTask<DollarRate> UpdateAsync(DollarRate model, string user, DateTime date);
        ValueTask<DollarRate> DeleteAsync(DollarRate model, string user, DateTime date);        
    }

    public class DollarRateService : BaseValidator, IDollarRateService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<DollarRate> _exceptionService = new ExceptionService<DollarRate>();
        private readonly GetDisplayNameDelegate _getDisplayName;

        public DollarRateService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<DollarRate>(propertyName);
        }

        public IQueryable<DollarRate> GetAll() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.DollarRates.AsNoTracking().AsQueryable();
            return data;
        });

        public ValueTask<DollarRate> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.DollarRates.FindAsync(id);
            return data;
        });

        public async ValueTask<Decimal?> GetRateAsync(DateTime? asOfDate)        
        {
            var dollarRate = await _db.DollarRates.Where(w => w.AsOf <= asOfDate).OrderByDescending(o => o.AsOf).FirstOrDefaultAsync();
            if (dollarRate == null)
            {
                return 0;
            }
            return dollarRate.Value; 
        }
    
        public ValueTask<DollarRate> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.DollarRates.FindAsync(id);

            ValidateRecord(entity);
            ValidateIfPosted(entity);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.DollarRates.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<DollarRate> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.DollarRates.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.DollarRates.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<DollarRate> CreateAsync(DollarRate model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new DollarRate();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.DollarRates.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<DollarRate> UpdateAsync(DollarRate model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateIfNull(model);

           var entity = await _db.DollarRates.FindAsync(model.Id);
           ValidateRecord(entity);
           ValidateIfPosted(entity);
           ValidateFields(model);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.DollarRates.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<DollarRate> DeleteAsync(DollarRate model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await _db.DollarRates.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.DollarRates.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.DollarRates.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(DollarRate entity, DollarRate model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.AsOf = model.AsOf;
            entity.Value = model.Value;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        
        private void ValidateFields(DollarRate model)
        {
            if (!model.AsOf.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.AsOf)), "Field is required.");
            }

            if (!model.Value.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Value)), "Field is required.");
            }
            
            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(DollarRate model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(DollarRate entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(DollarRate entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(DollarRate entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}