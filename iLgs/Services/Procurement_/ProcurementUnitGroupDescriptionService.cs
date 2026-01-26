using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
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
    public interface IProcurementUnitGroupDescriptionService
    {
        IQueryable<ProcurementUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId);
        ValueTask<ProcurementUnitGroupDescriptionVM> GetByIdAsync(Guid? id);
        ValueTask<ProcurementUnitGroupDescriptionVM> CreateAsync(ProcurementUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<ProcurementUnitGroupDescriptionVM> UpdateAsync(ProcurementUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<ProcurementUnitGroupDescriptionVM> DeleteAsync(ProcurementUnitGroupDescriptionVM model, string user, DateTime date);

        IProcurementUnitGroupDescriptionItemService UnitGroupDescriptionItemService { get; }
    }

    internal class ProcurementUnitGroupDescriptionService : BaseValidator, IProcurementUnitGroupDescriptionService
    {
        private readonly AppManEntities _db;
        private readonly IProcurementCommonService _procurementCommonService;
        private readonly ILoggingService _loggingservice;
        private readonly IExceptionService<ProcurementUnitGroupDescriptionVM> _exceptionService;

        private IProcurementUnitGroupDescriptionItemService _unitGroupDescriptionService;

        public ProcurementUnitGroupDescriptionService(AppManEntities db)
        {
            _db = db;
            _procurementCommonService = new ProcurementCommonService(_db);
            _loggingservice = new LoggingService();
            _exceptionService = new ExceptionService<ProcurementUnitGroupDescriptionVM>(_loggingservice);
        }

        public IProcurementUnitGroupDescriptionItemService UnitGroupDescriptionItemService
        {
            get
            {
                return _unitGroupDescriptionService = _unitGroupDescriptionService ?? new ProcurementUnitGroupDescriptionItemService(_db);
            }
        }

        private Expression<Func<ProcurementUnitGroupDescription, ProcurementUnitGroupDescriptionVM>> GetProjection()
        {
            return s => new ProcurementUnitGroupDescriptionVM
            {
                Id = s.Id,
                UnitGroupId = s.UnitGroupId,
                Description = s.Description,
                InsertedDt = s.InsertedDt
            };
        }

        public ValueTask<ProcurementUnitGroupDescriptionVM> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.ProcurementUnitGroupDescriptions.AsNoTracking()
                .Where(w => w.Id == id)
                .Select(GetProjection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<ProcurementUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.ProcurementUnitGroupDescriptions.AsNoTracking().Where(w => w.UnitGroupId == unitGroupId).AsNoTracking()
                .Select(GetProjection());
            return data;
        });

        public ValueTask<ProcurementUnitGroupDescriptionVM> CreateAsync(ProcurementUnitGroupDescriptionVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateStatusAsync(model.UnitGroupId);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new ProcurementUnitGroupDescription()
            {
                Id = model.Id,
                UnitGroupId = model.UnitGroupId,
                Description = model.Description,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.ProcurementUnitGroupDescriptions.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ProcurementUnitGroupDescriptionVM> UpdateAsync(ProcurementUnitGroupDescriptionVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);            

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.ProcurementUnitGroupDescriptions.FindAsync(model.Id);

            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.UnitGroupId);

            entity.UnitGroupId = model.UnitGroupId;
            entity.Description = model.Description;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<ProcurementUnitGroupDescriptionVM> DeleteAsync(ProcurementUnitGroupDescriptionVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);            

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.ProcurementUnitGroupDescriptions.FindAsync(model.Id);

            ValidateRecord(entity, model.Id);
            await ValidateStatusAsync(model.UnitGroupId);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.ProcurementUnitGroupDescriptions.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        private async Task ValidateStatusAsync(Guid? unitGroupId)
        {
            var procId = (await _db.Procurements.FirstOrDefaultAsync(p => p.ProcurementUnitGroups.Any(a => a.Id == unitGroupId)))?.Id;
            await _procurementCommonService.ValidateStatusAsync(procId);
        }

        private void ValidateIfNull(ProcurementUnitGroupDescriptionVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(ProcurementUnitGroupDescription entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }
        
        private void ValidateFieldsOnCreateUpdate(ProcurementUnitGroupDescriptionVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            //if (model.LocationId == null)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.LocationId)), "Field is required.");
            //}

            //if (!string.IsNullOrWhiteSpace(model.SetLotNo) && model.SetLotNo.Contains(" "))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Space is not allowed in Set/Lot No.");
            //}

            //if (!string.IsNullOrWhiteSpace(model.SetLotNo) && model.SetLotNo.Length == 1)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Invalid Value.");
            //}

            //if (!string.IsNullOrWhiteSpace(model.SetLotNo) && (!model.SetLotAmount.HasValue || model.SetLotAmount == 0))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotAmount)), "Set/Lot Amount is Required if with Set/Lot No.");
            //}

            //if ((model.SetLotAmount.HasValue && model.SetLotAmount > 0) && string.IsNullOrWhiteSpace(model.SetLotNo))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Set/Lot No. is Required if with Set/Lot Amount");
            //}

            // validate SetLotNo and SetLotAmount, SetLotNo must only have 1 SetLotAmount
            //if (mode == Mode.ADD)
            //{
            //    //if ((model.SetLotAmount.HasValue && model.SetLotAmount > 0) && !string.IsNullOrWhiteSpace(model.SetLotNo))
            //    if (!string.IsNullOrWhiteSpace(model.SetLotNo))
            //    {
            //        if (ctx.CustodianReportItems.Any(a => a.ReportId == model.ReportId 
            //            && a.LocationCode == model.LocationCode
            //            && a.SetLotNo == model.SetLotNo && a.SetLotAmount != model.SetLotAmount))
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.SetLotNo)), "Set/Lot Amount with different value already exist for the same Set/Lot No.");
            //        }
            //    }
            //}

            _imex.ThrowIfContainsErrors();
        }
    }
}