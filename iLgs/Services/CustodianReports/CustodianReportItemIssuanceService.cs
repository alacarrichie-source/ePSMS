using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportItemIssuanceService
    {
        IQueryable<CustodianReportItemIssuance> GetByReportItemId(Guid? reportItemId);
        ValueTask<CustodianReportItemIssuance> GetByIdAsync(Guid id);
        ValueTask<CustodianReportItemIssuance> CreateAsync(CustodianReportItemIssuance model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuance> UpdateAsync(CustodianReportItemIssuance model, string user, DateTime date);
        ValueTask<CustodianReportItemIssuance> DeleteAsync(CustodianReportItemIssuance model, string user, DateTime date);        
    }

    public class CustodianReportItemIssuanceService : ICustodianReportItemIssuanceService
    {        
        protected readonly AppManEntities _db;
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly ICodextnService _codextnService;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianReportItemIssuance> _exceptionService;
        private readonly IUserService _userService;

        public CustodianReportItemIssuanceService(AppManEntities db, 
            IAppManEntitiesFactory appManEntitiesFactory,
            ICodextnService codextnService,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportItemIssuance> exceptionService,
            IUserService userService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _codextnService = codextnService;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _userService = userService;
        }

        public ValueTask<CustodianReportItemIssuance> GetByIdAsync(Guid id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReportItemIssuances.Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });
        
        public IQueryable<CustodianReportItemIssuance> GetByReportItemId(Guid? reportItemId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReportItemIssuances.AsNoTracking().Where(w => w.ReportItemId == reportItemId).AsQueryable();
            return data;
        });
        
        public virtual async ValueTask<CustodianReportItemIssuance> CreateAsync(CustodianReportItemIssuance model, string user, DateTime date)
        {
            ValidateIfNull(model);
            ValidateReportingYearEnd(model.ReportItemId);
            ValidateIfPosted(model.ReportItemId);
            ValidateIfSubmitted(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = new CustodianReportItemIssuance();
                MapModelToEntityFields(entity, model, Mode.ADD);

                ctx.CustodianReportItemIssuances.Add(entity);
                await ctx.SaveChangesAsync();

                return model;
            }
        }

        public virtual async ValueTask<CustodianReportItemIssuance> UpdateAsync(CustodianReportItemIssuance model, string user, DateTime date)
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportItemIssuances.FindAsync(model.Id);
                ValidateRecord(entity);
                ValidateReportingYearEnd(entity.ReportItemId);
                ValidateIfPosted(entity.ReportItemId);
                ValidateIfSubmitted(model);
                ValidateUser(entity, model);

                MapModelToEntityFields(entity, model, Mode.EDIT);

                //ctx.CustodianReportItemIssuances.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                return model;
            }
        }

        public virtual async ValueTask<CustodianReportItemIssuance> DeleteAsync(CustodianReportItemIssuance model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportItemIssuances.FindAsync(model.Id);
                ValidateRecord(entity);
                ValidateReportingYearEnd(entity.ReportItemId);
                ValidateIfPosted(entity.ReportItemId);
                ValidateIfSubmitted(model);

                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                //ctx.CustodianReportItemIssuances.Attach(entity);
                //ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                ctx.CustodianReportItemIssuances.Remove(entity);
                //ctx.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();
                return model;
            }
        }        

        protected void MapModelToEntityFields(CustodianReportItemIssuance entity, CustodianReportItemIssuance model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            
            entity.ReportItemId = model.ReportItemId;
            entity.RefType = model.RefType;
            entity.RefNo = model.RefNo;
            entity.IssuedTo = model.IssuedTo;
            entity.AccountableOfficer = model.AccountableOfficer;            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;            
        }        

        private void ValidateIfNull(CustodianReportItemIssuance model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianReportItemIssuance entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateReportingYearEnd(Guid? reportItemId)
        {
            var asOf = _db.CustodianReportItems.Where(w => w.Id == reportItemId).Select(s => s.CustodianReport.AsOf).FirstOrDefault();
            ValidateReportingYearEnd(asOf.Value.Year);
        }

        private void ValidateReportingYearEnd(int? year)
        {
            _codextnService.ValidateReportingYearEnd(year);            
        }

        private void ValidateIfPosted(Guid? reportItemId)
        {
            var reportItem = _db.CustodianReportItems.Where(w => w.Id == reportItemId).SingleOrDefault();
            if (reportItem.PostedDt != null)
            {
                var msg = $"Record already posted by {reportItem.PostedBy} on {reportItem.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfSubmitted(CustodianReportItemIssuance model)
        {
            var custodianReportItem = _db.CustodianReportItems.FirstOrDefault(f => f.Id == model.ReportItemId);
            var submitForCount = _db.CustodianReportSubmitForCounts.FirstOrDefault(f => f.ReportId == custodianReportItem.ReportId && f.LocationId == custodianReportItem.LocationId && f.Status == "Submit");
            if (submitForCount != null)
            {
                var msg = $"Record already submitted for count by {submitForCount.UpdatedBy} on {submitForCount.UpdatedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateUser(CustodianReportItemIssuance entity, CustodianReportItemIssuance model)
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
    }
}