using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnAddCostService
    {
        IQueryable<PsCardItemExtnAddCost> GetAll(Guid? psCardItemExtnId);
        ValueTask<PsCardItemExtnAddCost> GetByIdAsync(Guid? id);

        decimal? GetTotalAddCost(Guid? psCardItemExtnId);

        ValueTask<PsCardItemExtnAddCost> CreateAsync(PsCardItemExtnAddCost model, string user, DateTime date);
        ValueTask<PsCardItemExtnAddCost> UpdateAsync(PsCardItemExtnAddCost model, string user, DateTime date);
        ValueTask<PsCardItemExtnAddCost> DeleteAsync(PsCardItemExtnAddCost model, string user, DateTime date);    
    }

    public class PsCardItemExtnAddCostService : BaseValidator, IPsCardItemExtnAddCostService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemExtnAddCost> _exceptionService = new ExceptionService<PsCardItemExtnAddCost>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public PsCardItemExtnAddCostService(AppManEntities db)
        {
            _db = db;
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnAddCost>(propertyName);
        }

        public IQueryable<PsCardItemExtnAddCost> GetAll(Guid? psCardItemExtnId)
        {
            var data = _db.PsCardItemExtnAddCosts.AsNoTracking()
                .Where(w => w.PsCardItemExtnId == psCardItemExtnId);
            return data;
        }
        
        public ValueTask<PsCardItemExtnAddCost> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtnAddCosts.AsNoTracking()
                .Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });


        public decimal? GetTotalAddCost(Guid? psCardItemExtnId)
        {
            var data = _db.PsCardItemExtnAddCosts.AsNoTracking().Where(w => w.PsCardItemExtnId == psCardItemExtnId).Sum(s => s.Amount) ?? 0;
            return data;
        }


        private void ValidateFields(PsCardItemExtnAddCost model, Mode mode)
        {
            if (string.IsNullOrWhiteSpace(model.PoNo))
            {                
                _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Field is required.");
            }
            else
            {
                var record = _db.PsCardItemExtnAddCosts.Where(a => a.PsCardItemExtnId == model.PsCardItemExtnId && a.PoNo == model.PoNo);
                if (record.Any())
                {
                    if (mode == Mode.ADD)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
                    }
                    else
                    {
                        if (record.Any(a => a.Id != model.Id))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
                        }
                    }
                }
            }

            if (!model.Effectivity.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Effectivity)), "Field is required.");
            }

            if (model.Amount == null || model.Amount == 0)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Amount)), "Field is required.");
            }
            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(PsCardItemExtnAddCost model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PsCardItemExtnAddCost entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        public ValueTask<PsCardItemExtnAddCost> CreateAsync(PsCardItemExtnAddCost model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnAddCost();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemExtnAddCosts.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });


        public ValueTask<PsCardItemExtnAddCost> UpdateAsync(PsCardItemExtnAddCost model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.EDIT);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtnAddCosts.FirstOrDefaultAsync(f => f.Id == model.Id);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemExtnAddCosts.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            
            return model;
        });

        public ValueTask<PsCardItemExtnAddCost> DeleteAsync(PsCardItemExtnAddCost model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtnAddCosts.FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemExtnAddCosts.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemExtnAddCosts.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            
            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnAddCost entity, PsCardItemExtnAddCost model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.PsCardItemExtnId = model.PsCardItemExtnId;
            entity.PoNo = model.PoNo;
            entity.Effectivity = model.Effectivity;
            entity.Amount = model.Amount;
            entity.Remarks = model.Remarks;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;            
        }
    }
}