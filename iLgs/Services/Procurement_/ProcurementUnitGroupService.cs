using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Logs;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Procurement_
{
    public interface IProcurementUnitGroupService
    {
        IQueryable<ProcurementUnitGroupVM> GetByProcId(Guid? procId);
        Task<ProcurementUnitGroupVM> GetByIdAsync(Guid? id);
        ValueTask<ProcurementUnitGroupVM> CreateAsync(ProcurementUnitGroupVM model, string user, DateTime date);
        ValueTask<ProcurementUnitGroupVM> UpdateAsync(ProcurementUnitGroupVM model, string user, DateTime date);
        ValueTask<ProcurementUnitGroupVM> DeleteAsync(ProcurementUnitGroupVM model, string user, DateTime date);

        IProcurementUnitGroupDescriptionService UnitGroupDescriptionService { get; }
    }

    internal class ProcurementUnitGroupService : BaseValidator, IProcurementUnitGroupService
    {
        private readonly AppManEntities _db;
        private readonly IProcurementCommonService _procurementCommonService;
        private readonly IExceptionService<ProcurementUnitGroupVM> _exceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        private IProcurementUnitGroupDescriptionService _unitGroupDescriptionService;

        public ProcurementUnitGroupService(AppManEntities db)
        {
            _db = db;
            _procurementCommonService = new ProcurementCommonService(_db);
            _exceptionService = new ExceptionService<ProcurementUnitGroupVM>();
            _getDisplayName = propertyName => Utility.GetDisplayName<ProcurementUnitGroupVM>(propertyName);
        }

        public IProcurementUnitGroupDescriptionService UnitGroupDescriptionService { get { return _unitGroupDescriptionService = _unitGroupDescriptionService ?? new ProcurementUnitGroupDescriptionService(_db); } }

        private Expression<Func<ProcurementUnitGroup, ProcurementUnitGroupVM>> GetProjection()
        {
            return s => new ProcurementUnitGroupVM
            {
                Id = s.Id,
                ProcId = s.ProcId,
                SetLotNo = s.SetLotNo,
                Qty = s.Qty,
                Unit = s.Unit,
                UnitCost = s.UnitCost,
                TotalCost = s.TotalCost
            };
        }

        public Task<ProcurementUnitGroupVM> GetByIdAsync(Guid? id)
        {
            return _db.ProcurementUnitGroups.AsNoTracking()
                .Where(w => w.Id == id)
                .Select(GetProjection()).FirstOrDefaultAsync();
        }

        public IQueryable<ProcurementUnitGroupVM> GetByProcId(Guid? procId)
        {
            var data = _db.ProcurementUnitGroups.AsNoTracking()
                .Where(w => w.ProcId == procId)
                .Select(GetProjection());
            return data;
        }

        public ValueTask<ProcurementUnitGroupVM> CreateAsync(ProcurementUnitGroupVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await _procurementCommonService.ValidateStatusAsync(model.ProcId);            

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;
            model.TotalCost = model.Qty * model.UnitCost;

            var entity = new ProcurementUnitGroup()
            {
                Id = model.Id,
                ProcId = model.ProcId,
                SetLotNo = model.SetLotNo,
                Qty = model.Qty,
                Unit = model.Unit,
                UnitCost = model.UnitCost,
                TotalCost = model.TotalCost,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.ProcurementUnitGroups.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });        

        public ValueTask<ProcurementUnitGroupVM> UpdateAsync(ProcurementUnitGroupVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await _db.ProcurementUnitGroups.Include(i => i.ProcurementUnitGroupDescriptions).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);
            await _procurementCommonService.ValidateStatusAsync(model.ProcId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.ProcId = model.ProcId;
            entity.SetLotNo = model.SetLotNo;
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.TotalCost = model.Qty * model.UnitCost;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            var unitGroupDescriptions = entity.ProcurementUnitGroupDescriptions.ToList();
            foreach (var unitGroupDescription in unitGroupDescriptions)
            {
                var unitGroupDescriptionItems = await _db.ProcurementUnitGroupDescriptionItems
                    .Include(i => i.ProcurementItem)
                    .Where(w => w.UnitGroupDescriptionId == unitGroupDescription.Id).ToListAsync();
                foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
                {
                    var priceRate = unitGroupDescriptionItem.ProcurementItem.PriceRate ?? 0;
                    var unitCost = unitGroupDescriptionItem.ProcurementItem.UnitCost ?? 0;
                    await _procurementCommonService.UpdateProcurementItemAsync(unitGroupDescriptionItem.ProcItemId, priceRate, unitCost, user, date);
                }
            }

            return model;
        });

        public ValueTask<ProcurementUnitGroupVM> DeleteAsync(ProcurementUnitGroupVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await _db.ProcurementUnitGroups.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            ValidateRecord(entity, model.Id);
            await _procurementCommonService.ValidateStatusAsync(model.ProcId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.ProcurementUnitGroups.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private void ValidateIfNull(ProcurementUnitGroupVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(ProcurementUnitGroup entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }        

        private void ValidateFieldsOnCreateUpdate(ProcurementUnitGroupVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            
            if (string.IsNullOrWhiteSpace(model.SetLotNo))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Field is required.");
            }

            if (model.Qty == null || model.Qty == 0)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Unit))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Field is required.");
            }
            else
            {                
                if (!_db.Codextns.Any(a => a.CodeMast.Code == "UNIT-GROUP" && a.Code == model.Unit))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid value");
                }
            }

            if (model.UnitCost == null || model.UnitCost == 0)
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.UnitCost)), "Field is required.");
            }
            
            _imex.ThrowIfContainsErrors();
        }
    }
}