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
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Codes
{
    public interface ILocationBudgetService
    {
        IQueryable<LocationBudgetVM> GetAll(Guid? locationId);
        ValueTask<LocationBudget> GetByIdAsync(Guid? id);
        ValueTask<LocationBudgetVM> CreateAsync(LocationBudgetVM model, string user, DateTime date);
        ValueTask<LocationBudgetVM> UpdateAsync(LocationBudgetVM model, string user, DateTime date);
        ValueTask<LocationBudgetVM> DeleteAsync(LocationBudgetVM model, string user, DateTime date);
    }

    public class LocationBudgetService : BaseValidator, ILocationBudgetService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<LocationBudgetVM> _exceptionService = new ExceptionService<LocationBudgetVM>();
        
        public LocationBudgetService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<LocationBudgetVM>(propertyName);
        }

        private static Expression<Func<LocationBudget, LocationBudgetVM>> Projection
        = s => new LocationBudgetVM
        {
            Id = s.Id,
            LocationId = s.LocationId,
            BudgetId = s.BudgetId,
            BudgetCode = s.Codextn1.Code,
            Description = s.Codextn1.Description,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt
        };

        public async ValueTask<LocationBudget> GetByIdAsync(Guid? id) 
        {
            var data = await _db.LocationBudgets.FindAsync(id);
            return data;
        }
        
        public IQueryable<LocationBudgetVM> GetAll(Guid? locationId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.LocationBudgets.Include(i => i.Codextn1).AsNoTracking()
                .Where(w => w.LocationId == locationId)
                .Select(Projection).OrderBy(o => o.BudgetCode)
                .AsQueryable();
            return data;
        });
        
        public ValueTask<LocationBudgetVM> CreateAsync(LocationBudgetVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);
            model.Id = Guid.NewGuid();

            var entity = new LocationBudget();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.LocationBudgets.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<LocationBudgetVM> UpdateAsync(LocationBudgetVM model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateIfNull(model);           

           var entity = await _db.LocationBudgets.FindAsync(model.Id);
           ValidateRecord(entity, model.Id);
           ValidateFields(model, Mode.ADD);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.LocationBudgets.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<LocationBudgetVM> DeleteAsync(LocationBudgetVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = await _db.LocationBudgets.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.LocationBudgets.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.LocationBudgets.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(LocationBudget entity, LocationBudgetVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.LocationId = model.LocationId;
            entity.BudgetId = model.BudgetId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateIfNull(LocationBudgetVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(LocationBudget entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(LocationBudgetVM model, Mode mode)
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.LocationId)), "Field is required.");
            }
            else
            {
                if (mode == Mode.ADD)
                {
                    if (_db.LocationBudgets.Any(a => a.LocationId == model.LocationId && a.BudgetId == model.BudgetId))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.BudgetId)), "Already Exists.");
                    }
                }
                else if (mode == Mode.EDIT)
                {
                    if (_db.LocationBudgets.Any(a => a.LocationId == model.LocationId && a.BudgetId == model.BudgetId && a.Id != model.Id))
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.BudgetId)), "Already Exists.");
                    }
                }
            }

            _imex.ThrowIfContainsErrors();
        }
    }
}