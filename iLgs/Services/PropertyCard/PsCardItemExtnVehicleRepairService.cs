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
    public interface IPsCardItemExtnVehicleRepairService
    {
        IQueryable<PsCardItemExtnVehicleRepair> GetAll(Guid? psCardItemExtnVehicleId);
        ValueTask<PsCardItemExtnVehicleRepair> GetByIdAsync(Guid? id);        

        ValueTask<PsCardItemExtnVehicleRepair> CreateAsync(PsCardItemExtnVehicleRepair model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicleRepair> UpdateAsync(PsCardItemExtnVehicleRepair model, string user, DateTime date);
        ValueTask<PsCardItemExtnVehicleRepair> DeleteAsync(PsCardItemExtnVehicleRepair model, string user, DateTime date);
    }

    public class PsCardItemExtnVehicleRepairService : BaseValidator, IPsCardItemExtnVehicleRepairService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemExtnVehicleRepair> _exceptionService = new ExceptionService<PsCardItemExtnVehicleRepair>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public PsCardItemExtnVehicleRepairService(AppManEntities db)
        {
            _db = db;
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<PsCardItemExtnVehicleRepair>(propertyName);
        }

        public IQueryable<PsCardItemExtnVehicleRepair> GetAll(Guid? psCardItemExtnVehicleId)
        {
            var data = _db.PsCardItemExtnVehicleRepairs.AsNoTracking()
                .Where(w => w.PsCardItemExtnVehicleId == psCardItemExtnVehicleId);
            return data;
        }

        public ValueTask<PsCardItemExtnVehicleRepair> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtnVehicleRepairs.AsNoTracking()
                .Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });
        

        private void ValidateFields(PsCardItemExtnVehicleRepair model, Mode mode)
        {
            if (string.IsNullOrWhiteSpace(model.InvoiceNo))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.InvoiceNo)), "Field is required.");
            }
            else
            {
                var record = _db.PsCardItemExtnVehicleRepairs.Where(a => a.PsCardItemExtnVehicleId == model.PsCardItemExtnVehicleId 
                    && a.InvoiceNo== model.InvoiceNo);
                if (record.Any())
                {
                    if (mode == Mode.ADD)
                    {
                        _imex.UpsertDataList(_getDisplayName(nameof(model.InvoiceNo)), "Already Exists.");
                    }
                    else
                    {
                        if (record.Any(a => a.Id != model.Id))
                        {
                            _imex.UpsertDataList(_getDisplayName(nameof(model.InvoiceNo)), "Already Exists.");
                        }
                    }
                }
            }

            if (!model.RepairDate.HasValue)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.RepairDate)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Nature))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Nature)), "Field is required.");
            }

            if (model.Amount == null || model.Amount == 0)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Amount)), "Field is required.");
            }
            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(PsCardItemExtnVehicleRepair model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PsCardItemExtnVehicleRepair entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        public ValueTask<PsCardItemExtnVehicleRepair> CreateAsync(PsCardItemExtnVehicleRepair model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PsCardItemExtnVehicleRepair();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PsCardItemExtnVehicleRepairs.Add(entity);
            await _db.SaveChangesAsync();
            await UpdateAccumulation(model.PsCardItemExtnVehicleId);

            return model;
        });


        public ValueTask<PsCardItemExtnVehicleRepair> UpdateAsync(PsCardItemExtnVehicleRepair model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.EDIT);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtnVehicleRepairs.FirstOrDefaultAsync(f => f.Id == model.Id);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.PsCardItemExtnVehicleRepairs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            await UpdateAccumulation(model.PsCardItemExtnVehicleId);

            return model;
        });

        public ValueTask<PsCardItemExtnVehicleRepair> DeleteAsync(PsCardItemExtnVehicleRepair model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtnVehicleRepairs.FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemExtnVehicleRepairs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemExtnVehicleRepairs.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            await UpdateAccumulation(model.PsCardItemExtnVehicleId);

            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnVehicleRepair entity, PsCardItemExtnVehicleRepair model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.PsCardItemExtnVehicleId = model.PsCardItemExtnVehicleId;
            entity.InvoiceNo = model.InvoiceNo;
            entity.RepairDate = model.RepairDate;            
            entity.Nature = model.Nature;
            entity.Amount = model.Amount;
            entity.AccTotal = model.AccTotal;
            entity.AccPercent = model.AccPercent;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private async ValueTask UpdateAccumulation(Guid? psCardItemExtnVehicleId)
        {
            var repairs = await _db.PsCardItemExtnVehicleRepairs
                .AsNoTracking()
                .Where(w => w.PsCardItemExtnVehicleId == psCardItemExtnVehicleId)
                .OrderBy(o => o.RepairDate).ThenBy(t => t.InsertedDt)
                .ToListAsync();

            decimal? accTotal = 0;
            foreach (var repair in repairs)
            {
                accTotal += repair.Amount;

                var entity = await _db.PsCardItemExtnVehicleRepairs.FindAsync(repair.Id);
                entity.AccTotal = accTotal;

                _db.PsCardItemExtnVehicleRepairs.Attach(entity);
                _db.Entry(entity).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
        }
    }
}