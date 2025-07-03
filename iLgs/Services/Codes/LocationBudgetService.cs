using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Codes
{
    public interface ILocationBudgetService
    {
        IQueryable<LocationBudgetVM> GetAll(Guid? locationId);
        IQueryable<LocationBudgetVM> GetAll(string locationName);
        ValueTask<LocationBudget> GetByIdAsync(Guid? id);
        bool IsValidBudgetCode(string locationName, string budgetCode);
        bool IsValidBudgetCode(Guid? locationId, string budgetCode);
        ValueTask<LocationBudgetVM> CreateAsync(LocationBudgetVM model, string user, DateTime date);
        ValueTask<LocationBudgetVM> UpdateAsync(LocationBudgetVM model, string user, DateTime date);
        ValueTask<LocationBudgetVM> DeleteAsync(LocationBudgetVM model, string user, DateTime date);
    }

    public class LocationBudgetService : BaseValidator, ILocationBudgetService
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<LocationBudgetVM> _exceptionService;
        
        public LocationBudgetService(AppManEntities db, 
            ICreateAndLogExceptions createAndLogExceptions, 
            IExceptionService<LocationBudgetVM> exceptionService)
        {
            _db = db;            
            _getDisplayName = Utility.GetDisplayName<LocationBudgetVM>;
            _exceptions = createAndLogExceptions;
            _exceptionService = exceptionService;
        }

        private static Expression<Func<LocationBudget, LocationBudgetVM>> Projection
        = s => new LocationBudgetVM
        {
            Id = s.Id,
            LocationId = s.LocationId,
            BudgetId = s.BudgetId,
            BudgetCode = s.Codextn1.Code,
            Description = s.Codextn1.Description,
            Fund = s.Codextn1.Desc3,
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

        public IQueryable<LocationBudgetVM> GetAll(string locationName) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.LocationBudgets.Include(i => i.Codextn1).AsNoTracking()
                .Where(w => w.Codextn.Description == locationName)
                .Select(Projection).OrderBy(o => o.BudgetCode)
                .AsQueryable();
            return data;
        });

        public bool IsValidBudgetCode(string locationName, string budgetCode)
        {
            return _db.LocationBudgets
                .Where(w => w.Codextn.Description == locationName && w.Codextn1.Code == budgetCode).Any();                
        }

        public bool IsValidBudgetCode(Guid? locationId, string budgetCode)
        {
            return _db.LocationBudgets
                .Where(w => w.Codextn.Id == locationId && w.Codextn1.Code == budgetCode).Any();
        }

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