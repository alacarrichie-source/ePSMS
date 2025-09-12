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

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportBldgItemPhaseService
    {
        IQueryable<CustodianReportBldgItemPhas> GetByBldgItemId(Guid? bldgItemId);
        ValueTask<CustodianReportBldgItemPhas> GetByIdAsync(Guid id);
        ValueTask<CustodianReportBldgItemPhas> CreateAsync(CustodianReportBldgItemPhas model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemPhas> UpdateAsync(CustodianReportBldgItemPhas model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemPhas> DeleteAsync(CustodianReportBldgItemPhas model, string user, DateTime date);
    }

    public class CustodianReportBldgItemPhaseService : BaseValidator, ICustodianReportBldgItemPhaseService
    {
        protected readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianReportBldgItemPhas> _exceptionService;
        private readonly IUserService _userService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianReportBldgItemPhaseService(AppManEntities db, 
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportBldgItemPhas> exceptionService,
            IUserService userService)
        {
            _db = db;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _userService = userService;
            _getDisplayName = Utility.GetDisplayName<CustodianReportBldgItemPhas>;
        }

        public ValueTask<CustodianReportBldgItemPhas> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportBldgItemPhases.Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportBldgItemPhas> GetByBldgItemId(Guid? bldgItemId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReportBldgItemPhases.AsNoTracking().Where(w => w.BldgItemId == bldgItemId).AsQueryable();
            return data;
        });

        public virtual async ValueTask<CustodianReportBldgItemPhas> CreateAsync(CustodianReportBldgItemPhas model, string user, DateTime date)
        {
            ValidateIfNull(model);
            ValidateIfPosted(model.BldgItemId);
            ValidateIfSubmitted(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new CustodianReportBldgItemPhas();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianReportBldgItemPhases.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public virtual async ValueTask<CustodianReportBldgItemPhas> UpdateAsync(CustodianReportBldgItemPhas model, string user, DateTime date)
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportBldgItemPhases.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity.BldgItemId);
            ValidateIfSubmitted(model);
            ValidateUser(entity, model);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.CustodianReportBldgItemPhases.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public virtual async ValueTask<CustodianReportBldgItemPhas> DeleteAsync(CustodianReportBldgItemPhas model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportBldgItemPhases.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity.BldgItemId);
            ValidateIfSubmitted(model);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianReportBldgItemPhases.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianReportBldgItemPhases.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        }

        protected void MapModelToEntityFields(CustodianReportBldgItemPhas entity, CustodianReportBldgItemPhas model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.BldgItemId = model.BldgItemId;
            entity.PhaseNo = model.PhaseNo;
            entity.CapitalOutlay= model.CapitalOutlay;
            entity.MOOE = model.MOOE;
            entity.ProjectName = model.ProjectName;
            entity.StartDate = model.StartDate;
            entity.TargetDate = model.TargetDate;
            entity.AcqDate = model.AcqDate;
            entity.CompletionDate = model.CompletionDate;
            entity.PercentComplete = model.PercentComplete;
            entity.Status = model.Status;
            entity.OldAmount = model.OldAmount;
            entity.AcqCost = model.AcqCost;
            entity.Remarks = model.Remarks;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateIfNull(CustodianReportBldgItemPhas model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianReportBldgItemPhas entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(Guid? bldgItemId)
        {
            var reportItem = _db.CustodianReportBldgItems.Where(w => w.Id == bldgItemId).SingleOrDefault();
            if (reportItem.PostedDt != null)
            {
                var msg = $"Record already posted by {reportItem.PostedBy} on {reportItem.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfSubmitted(CustodianReportBldgItemPhas model)
        {
            var custodianReportBldgItem = _db.CustodianReportBldgItems.FirstOrDefault(f => f.Id == model.BldgItemId);
            var submitForCount = _db.CustodianReportSubmitForCounts.FirstOrDefault(f => f.ReportId == custodianReportBldgItem.ReportId && f.LocationId == custodianReportBldgItem.LocationId && f.Status == "Submit");
            if (submitForCount != null)
            {
                var msg = $"Record already submitted for count by {submitForCount.UpdatedBy} on {submitForCount.UpdatedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateUser(CustodianReportBldgItemPhas entity, CustodianReportBldgItemPhas model)
        {
            if (entity.InsertedBy != model.UpdatedBy)
            {
                var isAdmin = _userService.IsUserNameAdmin(model.UpdatedBy);
                if (!isAdmin)
                {
                    throw new RecordLockedException($"Record can only be updated by {entity.InsertedBy} or an Admin.");
                }
            }
        }

        private void ValidateEntry(CustodianReportBldgItemPhas model, Mode mode)
        {
            if (string.IsNullOrWhiteSpace(model.PhaseNo))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseNo)), "Field is required.");
            }
            else
            {
                if (!(model.CapitalOutlay > 0 || model.MOOE > 0))
                {
                    _imex.UpsertDataList("Capital Outlay or MOOE Amount", "Either of the two Must have value.");
                }
            }

            //if (string.IsNullOrWhiteSpace(model.PhaseNo))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseNo)), "Field is required.");
            //}

            //if (!model.PhaseAmountCo.HasValue)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseAmountCo)), "Field is required.");
            //}

            _imex.ThrowIfContainsErrors();
        }
    }
}