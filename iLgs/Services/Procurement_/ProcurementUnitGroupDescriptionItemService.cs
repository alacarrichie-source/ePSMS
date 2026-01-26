using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Logs;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.Procurement_
{
    public interface IProcurementUnitGroupDescriptionItemService
    {
        IQueryable<ProcurementUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        Task<ProcurementUnitGroupDescriptionItem> GetByIdAsync(Guid? id);
        ValueTask<ProcurementUnitGroupDescriptionItemVM> CreateAsync(ProcurementUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<ProcurementUnitGroupDescriptionItemVM> UpdateAsync(ProcurementUnitGroupDescriptionItemVM model, string user, DateTime date);
        ValueTask<ProcurementUnitGroupDescriptionItemVM> DeleteAsync(ProcurementUnitGroupDescriptionItemVM model, string user, DateTime date);
    }

    internal class ProcurementUnitGroupDescriptionItemService : IProcurementUnitGroupDescriptionItemService
    {
        private readonly AppManEntities _db;
        private readonly IProcurementCommonService _procurementCommonService;
        private readonly ILoggingService _loggingservice;
        private readonly IExceptionService<ProcurementUnitGroupDescriptionItem> _exceptionService;
        private readonly IExceptionService<ProcurementUnitGroupDescriptionItemVM> _vmExceptionService;

        public ProcurementUnitGroupDescriptionItemService(AppManEntities db)
        {
            _db = db;
            _procurementCommonService = new ProcurementCommonService(_db);
            _loggingservice = new LoggingService();
            _exceptionService = new ExceptionService<ProcurementUnitGroupDescriptionItem>(_loggingservice);
            _vmExceptionService = new ExceptionService<ProcurementUnitGroupDescriptionItemVM>(_loggingservice);
        }

        private Expression<Func<ProcurementUnitGroupDescriptionItem, ProcurementUnitGroupDescriptionItemVM>> GetProjection()
        {
            return s => new ProcurementUnitGroupDescriptionItemVM
            {
                Id = s.Id,
                UnitGroupDescriptionId = s.UnitGroupDescriptionId,
                ProcItemId = s.ProcItemId,
                Category = s.ProcurementItem.ItemCode.ItemType.Category,
                PsNo = s.ProcurementItem.PsNo,
                ItemName = s.ProcurementItem.ItemName,
                Description = s.ProcurementItem.Description,
                Unit = s.ProcurementItem.Unit,
                QtyRequest = s.ProcurementItem.Qty,
                PriceRate = s.ProcurementItem.PriceRate,
                UnitCost = s.ProcurementItem.UnitCost,
                TotalCost = s.ProcurementItem.Amount,
                GroupUnitCost = s.ProcurementUnitGroupDescription.ProcurementUnitGroup.UnitCost,
                GroupTotalCost = s.ProcurementUnitGroupDescription.ProcurementUnitGroup.TotalCost,
                GroupQty = s.ProcurementUnitGroupDescription.ProcurementUnitGroup.Qty,
                InsertedDt = s.InsertedDt
            };
        }

        public Task<ProcurementUnitGroupDescriptionItem> GetByIdAsync(Guid? id)
        {
            return _db.ProcurementUnitGroupDescriptionItems.FindAsync(id);
        }

        public IQueryable<ProcurementUnitGroupDescriptionItemVM> GetByUnitGroupDescriptionId(Guid? unitGroupDescriptionId) 
        {
            var data = _db.ProcurementUnitGroupDescriptionItems.AsNoTracking()
                .Where(w => w.UnitGroupDescriptionId == unitGroupDescriptionId)
                .Select(GetProjection());
            return data;
        }

        public ValueTask<ProcurementUnitGroupDescriptionItemVM> CreateAsync(ProcurementUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateStatusAsync(model.UnitGroupDescriptionId);
            
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new ProcurementUnitGroupDescriptionItem()
            {
                Id = model.Id,
                UnitGroupDescriptionId = model.UnitGroupDescriptionId,
                ProcItemId = model.ProcItemId,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.ProcurementUnitGroupDescriptionItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });        
        
        public ValueTask<ProcurementUnitGroupDescriptionItemVM> UpdateAsync(ProcurementUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.ProcurementUnitGroupDescriptionItems.FindAsync(model.Id);

            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.UnitGroupDescriptionId);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();
            await _procurementCommonService.UpdateProcurementItemAsync(model.ProcItemId, model.PriceRate ?? 0, model.UnitCost ?? 0, user, date);            

            return model;
        });

        public ValueTask<ProcurementUnitGroupDescriptionItemVM> DeleteAsync(ProcurementUnitGroupDescriptionItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await _db.ProcurementUnitGroupDescriptionItems.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.UnitGroupDescriptionId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.ProcurementUnitGroupDescriptionItems.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private async Task ValidateStatusAsync(Guid? unitGroupDescriptionId)
        {
            var procId = (await _db.Procurements.FirstOrDefaultAsync(p => p.ProcurementUnitGroups.Any(a => a.ProcurementUnitGroupDescriptions.Any(b => b.Id == unitGroupDescriptionId))))?.Id;
            await _procurementCommonService.ValidateStatusAsync(procId);
        }

        private void ValidateIfNull(ProcurementUnitGroupDescriptionItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(ProcurementUnitGroupDescriptionItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }
    }
}