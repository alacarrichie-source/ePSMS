using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.PPMP_
{
    public interface IPPMPItemUsageService
    {
        IQueryable<PPMPItemUsageVM> GetAll(Guid? ppmpItemId);
        ValueTask<PPMPItemUsageVM> GetByIdAsync(Guid? id);
        ValueTask<PPMPItemUsageVM> GetAsync(Guid? ppmpItemId, Guid? prId);

        ValueTask<PPMPItemUsageVM> SaveAsync(PPMPItemUsageVM model, string user, DateTime date);

        ValueTask<PPMPItemUsageVM> CreateAsync(PPMPItemUsageVM model, string user, DateTime date);
        ValueTask<PPMPItemUsageVM> UpdateAsync(PPMPItemUsageVM model, string user, DateTime date);
        ValueTask<PPMPItemUsageVM> DeleteAsync(PPMPItemUsageVM model, string user, DateTime date);
    }

    internal class PPMPItemUsageService : BaseValidator, IPPMPItemUsageService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PPMPItemUsageVM> _vmExceptionService;
        private readonly ICodextnService _codextnService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IPPMPSharedService _ppmpSharedService;

        public PPMPItemUsageService(AppManEntities db)
        {
            _db = db;
            _vmExceptionService = new ExceptionService<PPMPItemUsageVM>();
            _codextnService = new CodextnService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<PPMPItemUsageVM>(propertyName);
            _ppmpSharedService = new PPMPSharedService(_db);
        }

        private Expression<Func<PPMPItemUsage, PPMPItemUsageVM>> Projection()
        {
            return s => new PPMPItemUsageVM
            {
                Id = s.Id,
                PpmpItemId = s.PpmpItemId,
                PrId = s.PrId,
                Type = s.Type,
                Reference = s.Reference,
                Qty = s.Qty,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt
            };
        }

        public ValueTask<PPMPItemUsageVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PPMPItemUsages.AsNoTracking().Where(w => w.Id == id)
                .Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<PPMPItemUsageVM> GetAsync(Guid? ppmpItemId, Guid? prId) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PPMPItemUsages.AsNoTracking().Where(w => w.PpmpItemId == ppmpItemId && w.PrId == prId)
                .Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<PPMPItemUsageVM> GetAll(Guid? ppmpItemId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PPMPItemUsages.AsNoTracking().Where(w => w.PpmpItemId == ppmpItemId)
                .Select(Projection());
            return data;
        });

        private async Task ValidateFieldsAsync(PPMPItemUsageVM model)
        {
            _imex = new InvalidModelException();

            if (string.IsNullOrWhiteSpace(model.Type))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Type)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Reference))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Reference)), "Field is required.");
            }

            if (model.Qty == null || model.Qty == 0)
            {                
                _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), "Field is required.");                
            }
            else
            {
                var itemQty = (await _db.PPMPItems.FindAsync(model.PpmpItemId)).Qty;
                var itemUsed = _db.PPMPItemUsages.Where(w => w.PpmpItemId == model.PpmpItemId).Sum(x => x.Qty) ?? 0;
                var bal = itemQty - itemUsed;
                if (model.Qty > bal)
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), $"Quantity must not exceed the quantity balance of {bal}.");
                }
            }

            _imex.ThrowIfContainsErrors();
        }

        public ValueTask<PPMPItemUsageVM> SaveAsync(PPMPItemUsageVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            if (await _db.PPMPItemUsages.AnyAsync(a => a.Id == model.Id))
            {
                return await UpdateAsync(model, user, date);
            }
            return await CreateAsync(model, user, date);
        });

        public ValueTask<PPMPItemUsageVM> CreateAsync(PPMPItemUsageVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateStatusAsync(model.PpmpItemId);
            await ValidateFieldsAsync(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new PPMPItemUsage();

            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PPMPItemUsages.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PPMPItemUsageVM> UpdateAsync(PPMPItemUsageVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateFieldsAsync(model);
            await ValidateStatusAsync(model.PpmpItemId);

            var entity = await _db.PPMPItemUsages.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            return model;
        });

        public ValueTask<PPMPItemUsageVM> DeleteAsync(PPMPItemUsageVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateStatusAsync(model.PpmpItemId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PPMPItemUsages.FirstOrDefaultAsync(f => f.Id == model.Id);
            ValidateRecord(entity, model.Id);

            _db.PPMPItemUsages.Remove(entity);            
            await _db.SaveChangesAsync();

            return model;
        });

        private void MapModelToEntityFields(PPMPItemUsage entity, PPMPItemUsageVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
                entity.PpmpItemId = model.PpmpItemId;
            }

            entity.Type = model.Type.ToUpper().Trim();
            entity.Reference = model.Reference.ToUpper().Trim();
            entity.Qty = model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateIfNull(PPMPItemUsageVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PPMPItemUsage entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private async Task ValidateStatusAsync(Guid? ppmpItemId)
        {
            var ppmpId = (await _db.PPMPItems.FirstOrDefaultAsync(f => f.Id == ppmpItemId)).PpmpId;
            await _ppmpSharedService.ValidateStatusAsync((Guid)ppmpId);
        }
    }
}