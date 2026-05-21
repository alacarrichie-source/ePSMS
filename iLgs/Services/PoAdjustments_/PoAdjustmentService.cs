using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PoAdjustments_
{
    public interface IPoAdjustmentService
    {
        IQueryable<PoAdjustmentVM> GetAll();
        ValueTask<PoAdjustmentVM> GetByIdAsync(Guid? id);

        ValueTask<PoAdjustmentVM> CreateAsync(PoAdjustmentVM model, string user, DateTime date);
        ValueTask<PoAdjustmentVM> UpdateAsync(PoAdjustmentVM model, string user, DateTime date);
        ValueTask<PoAdjustmentVM> DeleteAsync(PoAdjustmentVM model, string user, DateTime date);

        ValueTask<QueryPoVM> PoDeleteAsync(QueryPoVM model, string user, DateTime date);
        Task<IEnumerable<PsCardItem>> GetValidatePoListAsync(string fund, string poNo, DateTime? poDate, Guid? deptId, bool isValidate);
    }

    public class PoAdjustmentService : BaseValidator, IPoAdjustmentService
    {
        private readonly AppManEntities _db;        
        private readonly IExceptionService<Supplier> _exceptionService;
        private readonly IExceptionService<PoAdjustmentVM> _vmExceptionService;
        private readonly IExceptionService<QueryPoVM> _poExceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public PoAdjustmentService(AppManEntities db)
        {
            _db = db;
            _exceptionService = new ExceptionService<Supplier>();
            _vmExceptionService = new ExceptionService<PoAdjustmentVM>();
            _poExceptionService = new ExceptionService<QueryPoVM>();
            _getDisplayName = Utility.GetDisplayName<Supplier>;
        }

        private static Expression<Func<PoAdjustment, PoAdjustmentVM>> Projection
        = s => new PoAdjustmentVM
        {
            Id = s.Id, 
            ForYear = s.ForYear,
            PoNo = s.PoNo,
            Remarks = s.Remarks,
            IsOk = s.IsOk,
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            UpdatedBy = s.UpdatedBy,
            UpdatedDt = s.UpdatedDt,
            IsOkay = s.IsOk.Value == true ? "Y" : "N"
        };

        public IQueryable<PoAdjustmentVM> GetAll()
        {
            var data = _db.PoAdjustments.Select(Projection).AsNoTracking();
            return data;
        }

        public async ValueTask<PoAdjustmentVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PoAdjustments.Select(Projection).FirstOrDefaultAsync(f => f.Id == id);
            return data;
        }

        public ValueTask<PoAdjustmentVM> CreateAsync(PoAdjustmentVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            ValidateFields(model, Mode.ADD);
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PoAdjustment();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.PoAdjustments.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PoAdjustmentVM> UpdateAsync(PoAdjustmentVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            var entity = await _db.PoAdjustments.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ValidateFields(model, Mode.EDIT);
            MapModelToEntityFields(entity, model, Mode.ADD);

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PoAdjustmentVM> DeleteAsync(PoAdjustmentVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PoAdjustments.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.PoAdjustments.Remove(entity);
            await _db.SaveChangesAsync();            

            return model;
        });

        private void MapModelToEntityFields(PoAdjustment entity, PoAdjustmentVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;                
            }


            entity.ForYear = model.ForYear;
            entity.PoNo = model.PoNo;
            entity.Remarks = model.Remarks;
            entity.IsOk = model.IsOk ?? false;

            entity.UpdatedDt = model.UpdatedDt;
            entity.UpdatedBy = model.UpdatedBy;

            model.IsOkay = entity.IsOk.Value == true ? "Y" : "N";
        }

        public async Task<IEnumerable<PsCardItem>> GetValidatePoListAsync(string fund, string poNo, DateTime? poDate, Guid? deptId, bool isValidate)
        {
            var poList = await _db.PsCardItems.Include(i => i.PsCard).Where(w => w.PsCard.Fund == fund && w.PoNo == poNo && w.PoDate == poDate && w.DeptId == deptId).ToListAsync();
            if (isValidate)
            {
                foreach (var po in poList)
                {
                    if (await _db.PsCardItemTransfers.Where(w => w.PsCardItem.PoNo == po.PoNo && w.PsCardItem.PoDate == po.PoDate && w.PsCardItem.DeptId == po.DeptId && w.PsCardItem.PsCard.Fund == po.PsCard.Fund).AnyAsync())
                    {
                        throw new RecordRelationshipException("Record has issuance records.");
                    }
                }
            }

            return poList;
        }

        public ValueTask<QueryPoVM> PoDeleteAsync(QueryPoVM model, string user, DateTime date) => _poExceptionService.TryCatch(async () =>
        {
            if (model is null)
            {
                throw new NullException();
            }

            var poList = await GetValidatePoListAsync(model.Fund, model.PoNo, model.PoDate, model.DeptId, false);

            foreach (var po in poList)
            {
                po.UpdatedBy = user;
                po.UpdatedDt = date;
            }
            await _db.SaveChangesAsync();

            _db.PsCardItems.RemoveRange(poList);
            await _db.SaveChangesAsync();
            
            return model;
        });

        private void ValidateIfNull(PoAdjustmentVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PoAdjustment entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateFields(PoAdjustmentVM model, Mode mode)
        {
            _imex = new InvalidModelException();

            //if (!model.ForYear.HasValue || model.ForYear == 0)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.ForYear)), "Field is required.");
            //}

            //if (string.IsNullOrWhiteSpace(model.PoNo))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Field is required.");
            //}
            //else
            //{
            //    if (mode == Mode.ADD)
            //    {
            //        //if (_db.PoAdjustments.Where(w => w.ForYear == model.ForYear && w.PoNo == model.PoNo).Any())
            //        if (_db.PoAdjustments.Where(w => w.PoNo == model.PoNo).Any())
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
            //        }
            //    }
            //    else if (mode == Mode.EDIT)
            //    {
            //        //if (_db.PoAdjustments.Where(w => w.ForYear == model.ForYear && w.PoNo == model.PoNo && w.Id != model.Id).Any())
            //        if (_db.PoAdjustments.Where(w => w.PoNo == model.PoNo && w.Id != model.Id).Any())
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
            //        }
            //    }
            //}

            if (mode == Mode.ADD)
            {
                if (_db.PoAdjustments.Where(w => w.PoNo == model.PoNo).Any())
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
                }
            }
            else if (mode == Mode.EDIT)
            {
                if (_db.PoAdjustments.Where(w => w.PoNo == model.PoNo && w.Id != model.Id).Any())
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.PoNo)), "Already Exists.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Remarks))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Remarks)), "Field is required.");
            }
            
            _imex.ThrowIfContainsErrors();
        }
    }
}