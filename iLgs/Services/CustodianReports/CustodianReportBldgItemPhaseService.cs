using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportBldgItemPhaseService
    {
        IQueryable<CustodianReportBldgItemPhasVM> GetByBldgItemId(Guid? bldgItemId);
        ValueTask<CustodianReportBldgItemPhasVM> GetByIdAsync(Guid id);
        ValueTask<CustodianReportBldgItemTransferVM> TransferItemAsync(CustodianReportBldgItemTransferVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemPhasVM> CreateAsync(CustodianReportBldgItemPhasVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemPhasVM> UpdateAsync(CustodianReportBldgItemPhasVM model, string user, DateTime date);
        ValueTask<CustodianReportBldgItemPhasVM> DeleteAsync(CustodianReportBldgItemPhasVM model, string user, DateTime date);
    }

    public class CustodianReportBldgItemPhaseService : BaseValidator, ICustodianReportBldgItemPhaseService
    {
        protected readonly AppManEntities _db;
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianReportBldgItemPhasVM> _exceptionService;
        private readonly IExceptionService<CustodianReportBldgItemTransferVM> _transferItemExceptionService;
        private readonly IUserService _userService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly ICodextnService _codextnService;

        public CustodianReportBldgItemPhaseService(AppManEntities db, 
            IAppManEntitiesFactory appManEntitiesFactory,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportBldgItemPhasVM> exceptionService,
            IExceptionService<CustodianReportBldgItemTransferVM> transferItemExceptionService,
            IUserService userService,
            ICodextnService codextnService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _transferItemExceptionService = transferItemExceptionService;
            _userService = userService;
            _getDisplayName = Utility.GetDisplayName<CustodianReportBldgItemPhasVM>;
            _codextnService = codextnService;
        }

        private Expression<Func<CustodianReportBldgItemPhas, CustodianReportBldgItemPhasVM>> Projection()
        {
            return s => new CustodianReportBldgItemPhasVM
            {
                Id = s.Id,
                BldgItemId = s.BldgItemId,                
                PhaseNo = s.PhaseNo,
                CapitalOutlay = s.CapitalOutlay,
                MOOE = s.MOOE,
                ProjectName = s.ProjectName,
                BuildingType = s.BuildingType,
                StartDate = s.StartDate,
                TargetDate = s.TargetDate,
                AcqDate = s.AcqDate,
                CompletionDate = s.CompletionDate,
                PercentComplete = s.PercentComplete,
                Status = s.Status,
                OldAmount = s.OldAmount,
                AcqCost = s.AcqCost,
                Remarks = s.Remarks,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,
                Fund = s.Fund,
                Annex = s.Annex,
                BldgItem = s.BldgItem                
            };
        }

        public ValueTask<CustodianReportBldgItemPhasVM> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportBldgItemPhases.Where(w => w.Id == id).Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReportBldgItemPhasVM> GetByBldgItemId(Guid? bldgItemId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReportBldgItemPhases.Where(w => w.BldgItemId == bldgItemId)
                .Select(Projection()).AsNoTracking();
            return data;
        });

        public virtual async ValueTask<CustodianReportBldgItemTransferVM> TransferItemAsync(CustodianReportBldgItemTransferVM model, string user, DateTime date)
        {
            if (model is null)
            {
                throw new NullException();
            }
            ValidateReportingYearEnd(model.SourceId);
            ValidateIfPosted(model.SourceId);

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                // 1. Load source with phases
                var sourceItem = await ctx.CustodianReportBldgItems
                    .Include(i => i.CustodianReportBldgItemPhases).AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == model.SourceId);

                if (sourceItem == null)
                {
                    throw new NotFoundException(model.SourceId);
                }

                // 2. Load target item
                var targetItems = ctx.CustodianReportBldgItems
                    .Include(i => i.CustodianReportBldgItemPhases)
                    .Where(p =>
                        p.ReportId == model.ReportId &&
                        p.CustodianItemNo == model.TargetCustodianItemNo);

                if (targetItems.Count() > 1)
                {
                    throw new RecordRelationshipException($"More than 1 {model.TargetCustodianItemNo} was found.");
                }

                var targetItem = targetItems.FirstOrDefault();    

                if (targetItem == null)
                {
                    throw new NotFoundException($"Custodian Item No. {model.TargetCustodianItemNo} not found.");
                }

                // 3. Extract child phases
                var sourcePhases = sourceItem.CustodianReportBldgItemPhases.ToList();

                // 4. Transfer each phase
                foreach (var phase in sourcePhases)
                {
                    //// remove from source navigation
                    //sourceItem.CustodianReportBldgItemPhases.Remove(phase);

                    //// change FK
                    //phase.BldgItemId = targetItem.Id;

                    //// add to target navigation
                    //targetItem.CustodianReportBldgItemPhases.Add(phase);

                    var fund = string.IsNullOrWhiteSpace(phase.Fund) ? phase.CustodianReportBldgItem.Fund : phase.Fund;
                    var annex = string.IsNullOrWhiteSpace(phase.Annex) ? phase.CustodianReportBldgItem.Annex : phase.Annex;
                    var bldgItem = string.IsNullOrWhiteSpace(phase.BldgItem) ? phase.CustodianReportBldgItem.BldgItem : phase.BldgItem;

                    await ctx.Database.ExecuteSqlCommandAsync("Update CustodianReportBldgItemPhases " +
                        "Set BldgItemId = {0}, BldgItem = {1}, " +
                        "Fund = {2}, Annex = {3}, UpdatedBy = {4}, UpdatedDt = {5} " +
                        "Where Id = {6}", targetItem.Id, bldgItem, fund, annex, user, date, phase.Id);
                }

                //// Save
                //await ctx.SaveChangesAsync();
            }

            return model;
        }

        public virtual async ValueTask<CustodianReportBldgItemPhasVM> CreateAsync(CustodianReportBldgItemPhasVM model, string user, DateTime date)
        {
            ValidateIfNull(model);
            ValidateReportingYearEnd(model.BldgItemId);
            ValidateIfPosted(model.BldgItemId);
            ValidateIfSubmitted(model);
            ValidateEntry(model, Mode.EDIT);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {

                var entity = new CustodianReportBldgItemPhas();
                MapModelToEntityFields(entity, model, Mode.ADD);

                ctx.CustodianReportBldgItemPhases.Add(entity);
                await ctx.SaveChangesAsync();
                await UpdateBldgAnnexAsync(ctx, model.BldgItemId);

                return model;
            }
        }

        private async Task UpdateBldgAnnexAsync(AppManEntities ctx, Guid? bldgItemId)
        {
            var annex = await ctx.CustodianReportBldgItemPhases.Where(p => p.BldgItemId == bldgItemId && p.Annex != null && p.Annex != "")
                .OrderBy(o => o.Annex)
                .Select(s => s.Annex)
                .FirstOrDefaultAsync();
                        
            var bldgItem = await ctx.CustodianReportBldgItems.FirstOrDefaultAsync(p => p.Id == bldgItemId && p.Annex != annex);
            if (bldgItem != null) // update if with changes
            {
                bldgItem.Annex = annex;
                await ctx.SaveChangesAsync();
            }
        }

        public virtual async ValueTask<CustodianReportBldgItemPhasVM> UpdateAsync(CustodianReportBldgItemPhasVM model, string user, DateTime date)
        {
            ValidateIfNull(model);
            ValidateReportingYearEnd(model.BldgItemId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportBldgItemPhases.FindAsync(model.Id);
                ValidateRecord(entity);
                ValidateIfPosted(entity.BldgItemId);
                ValidateIfSubmitted(model);
                ValidateEntry(model, Mode.EDIT);

                //ValidateUser(entity, model);

                MapModelToEntityFields(entity, model, Mode.EDIT);

                //_db.CustodianReportBldgItemPhases.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
                await UpdateBldgAnnexAsync(ctx, model.BldgItemId);

                return model;
            }
        }

        public virtual async ValueTask<CustodianReportBldgItemPhasVM> DeleteAsync(CustodianReportBldgItemPhasVM model, string user, DateTime date)
        {
            ValidateReportingYearEnd(model.BldgItemId);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportBldgItemPhases.FindAsync(model.Id);
                ValidateRecord(entity);
                ValidateIfPosted(entity.BldgItemId);
                ValidateIfSubmitted(model);

                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                //_db.CustodianReportBldgItemPhases.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                ctx.CustodianReportBldgItemPhases.Remove(entity);
                //_db.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();
                await UpdateBldgAnnexAsync(ctx, model.BldgItemId);

                return model;
            }
        }

        protected void MapModelToEntityFields(CustodianReportBldgItemPhas entity, CustodianReportBldgItemPhasVM model, Mode mode)
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
            entity.BuildingType = model.BuildingType;
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
            entity.Fund = model.Fund;
            entity.Annex = model.Annex;
            entity.BldgItem = model.BldgItem;
        }

        public void ValidateReportingYearEnd(Guid? bldgId)
        {
            var asOf = _db.CustodianReportBldgItems.Where(w => w.Id == bldgId).Select(s => s.CustodianReport.AsOf).FirstOrDefault();
            _codextnService.ValidateReportingYearEnd(asOf.Value.Year);
        }

        private void ValidateIfNull(CustodianReportBldgItemPhasVM model)
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

        private void ValidateIfSubmitted(CustodianReportBldgItemPhasVM model)
        {
            var custodianReportBldgItem = _db.CustodianReportBldgItems.FirstOrDefault(f => f.Id == model.BldgItemId);
            var submitForCount = _db.CustodianReportSubmitForCounts.FirstOrDefault(f => f.ReportId == custodianReportBldgItem.ReportId && f.LocationId == custodianReportBldgItem.LocationId && f.Status == "Submit");
            if (submitForCount != null)
            {
                var msg = $"Record already submitted for count by {submitForCount.UpdatedBy} on {submitForCount.UpdatedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateUser(CustodianReportBldgItemPhas entity, CustodianReportBldgItemPhasVM model)
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

        private void ValidateEntry(CustodianReportBldgItemPhasVM model, Mode mode)
        {
            _imex = new InvalidModelException();
            //if (string.IsNullOrWhiteSpace(model.PhaseNo))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseNo)), "Field is required.");
            //}
            //else
            //{
            //    if (!(model.CapitalOutlay > 0 || model.MOOE > 0))
            //    {
            //        _imex.UpsertDataList("Capital Outlay or MOOE Amount", "Either of the two Must have value.");
            //    }
            //}

            //if (string.IsNullOrWhiteSpace(model.PhaseNo))
            //{                
            //    if (!(model.CapitalOutlay > 0 || model.MOOE > 0))
            //    {
            //        _imex.UpsertDataList("Capital Outlay or MOOE Amount", "Either of the two Must have value.");
            //    }
            //}

            if (string.IsNullOrWhiteSpace(model.Annex))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.Annex)), "Field is required.");
            }
            
            if (!string.IsNullOrWhiteSpace(model.Annex) && model.Annex == "C")
            {
                if (string.IsNullOrWhiteSpace(model.Remarks))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Remarks)), "Field is Required for Annex C.");
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