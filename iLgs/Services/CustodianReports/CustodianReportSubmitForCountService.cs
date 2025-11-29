using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.CustodianReports
{
    public interface ICustodianReportSubmitForCountService
    {
        IQueryable<CustodianReportSubmitForCountVM> GetAll();
        IQueryable<CustodianReportSubmitForCountVM> GetByReportYear(int? reportYear);
        IQueryable<CustodianReportSubmitForCountVM> GetByReportYearAccountGroup(int? reportYear, int? accountGroup);
        IQueryable<CustodianReportSubmitForCountVM> GetByReportId(Guid? reportId);
        ValueTask<CustodianReportSubmitForCountVM> GetByLocationAsync(Guid? reportId, Guid? locationId);
        ValueTask<bool> IsSubmitForCountAsync(Guid? reportId, Guid? locationId);

        ValueTask<CustodianReportSubmitForCountVM> DeleteAsync(CustodianReportSubmitForCountVM model, string user, DateTime date);

        ValueTask<CustodianReportSubmitForCountVM> SubmitAsync(Guid? reportId, Guid? locationId, string url, string user, DateTime date, bool isLocationRequired);
        ValueTask<CustodianReportSubmitForCountVM> UnsubmitAsync(Guid? reportId, Guid? locationId, string url, string user, DateTime date);
    }

    public class CustodianReportSubmitForCountService : BaseValidator, ICustodianReportSubmitForCountService
    {
        private readonly AppManEntities _db;
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<CustodianReportSubmitForCountVM> _vmExceptionService;
        private readonly IUserService _userService;
        private readonly IItemCodeService _itemCodeService;
        private readonly INotificationMessageService _notificationMessageService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianReportSubmitForCountService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            ICreateAndLogExceptions exceptions,
            IExceptionService<CustodianReportSubmitForCountVM> vmExceptionService,
            IUserService userService,
            IItemCodeService itemCodeService,
            INotificationMessageService notificationMessageService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _exceptions = exceptions;
            _vmExceptionService = vmExceptionService;
            _userService = userService;
            _itemCodeService = itemCodeService;
            _notificationMessageService = notificationMessageService;
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportSubmitForCountVM>(propertyName);
        }

        private Expression<Func<CustodianReportSubmitForCount, CustodianReportSubmitForCountVM>> Projection()
        {
            return s => new CustodianReportSubmitForCountVM
            {
                Id = s.Id,                               
                ReportId = s.ReportId,
                LocationId = s.LocationId,
                Status = s.Status,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,
                ReportYear = s.CustodianReport.AsOf.Value.Year,
                Department = s.CustodianReport.Codextn.Description,
                Location = s.Codextn.Description,
                LocationCode = s.Codextn.Code,
                AccountGroup = s.CustodianReport.AccountGroup
            };
        }

        public IQueryable<CustodianReportSubmitForCountVM> GetAll()
        {
            var data = _db.CustodianReportSubmitForCounts
                .Include(i => i.CustodianReport.Codextn)
                .Include(i => i.Codextn)                
                .Select(Projection()).AsNoTracking();
            return data;
        }

        public IQueryable<CustodianReportSubmitForCountVM> GetByReportYear(int? reportYear)
        {
            var data = _db.CustodianReportSubmitForCounts
                .Include(i => i.CustodianReport.Codextn)
                .Include(i => i.Codextn)
                .Where(w => w.CustodianReport.AsOf.Value.Year == reportYear)
                .Select(Projection()).AsNoTracking();
            return data;
        }

        public IQueryable<CustodianReportSubmitForCountVM> GetByReportYearAccountGroup(int? reportYear, int? accountGroup)
        {
            var data = _db.CustodianReportSubmitForCounts
                .Include(i => i.CustodianReport.Codextn)
                .Include(i => i.Codextn)
                .Where(w => w.CustodianReport.AsOf.Value.Year == reportYear && w.CustodianReport.AccountGroup == accountGroup)
                .Select(Projection()).AsNoTracking();
            return data;
        }

        public IQueryable<CustodianReportSubmitForCountVM> GetByReportId(Guid? reportId)
        {
            var data = _db.CustodianReportSubmitForCounts
                .Include(i => i.CustodianReport.Codextn)
                .Include(i => i.Codextn)
                .Where(w => w.CustodianReport.Id == reportId)
                .Select(Projection()).AsNoTracking();
            return data;
        }

        public async ValueTask<CustodianReportSubmitForCountVM> GetByLocationAsync(Guid? reportId, Guid? locationId)
        {
            var data = await _db.CustodianReportSubmitForCounts
                .Where(w => w.CustodianReport.Id == reportId && w.LocationId == locationId)
                .Select(Projection()).AsNoTracking().FirstOrDefaultAsync();
            return data;
        }

        public async ValueTask<bool> IsSubmitForCountAsync(Guid? reportId, Guid? locationId)
        {
            var data = await GetByLocationAsync(reportId, locationId);
            if (data == null)
            {
                return false;
            }
            return data.Status == "Submit";
        }        

        public ValueTask<CustodianReportSubmitForCountVM> DeleteAsync(CustodianReportSubmitForCountVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.CustodianReportSubmitForCounts.FirstOrDefaultAsync(f => f.Id == model.Id);
                ValidateRecord(entity, model.Id);

                ctx.CustodianReportSubmitForCounts.Remove(entity);
                //ctx.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();
                return model;
            }
        });        

        public ValueTask<CustodianReportSubmitForCountVM> SubmitAsync(Guid? reportId, Guid? locationId, string url, string user, DateTime date, bool isLocationRequired) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (reportId == null)
            {
                throw new NullException();
            }

            if (isLocationRequired && locationId == null)
            {
                throw new NullException("Location is Required.");
            }

            var isNew = false;
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var model = await GetByLocationAsync(reportId, locationId);
                if (model == null)
                {
                    model = new CustodianReportSubmitForCountVM()
                    {
                        Id = Guid.NewGuid(),
                        ReportId = reportId,
                        LocationId = locationId,
                        InsertedBy = user,
                        InsertedDt = date
                    };
                    isNew = true;
                }

                if (model.Status == "Submit")
                {
                    throw new RecordAlreadyExistsException("Department/Location", "Already Submitted.");
                }

                model.UpdatedBy = user;
                model.UpdatedDt = date;
                model.Status = "Submit";

                if (isNew)
                {
                    var entity = new CustodianReportSubmitForCount();
                    entity.Id = model.Id;
                    entity.ReportId = model.ReportId;
                    entity.LocationId = model.LocationId;
                    entity.Status = model.Status;
                    entity.InsertedBy = model.InsertedBy;
                    entity.InsertedDt = model.InsertedDt;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    ctx.CustodianReportSubmitForCounts.Add(entity);
                }
                else
                {
                    var entity = await ctx.CustodianReportSubmitForCounts.FindAsync(model.Id);
                    entity.Status = model.Status;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    ctx.CustodianReportSubmitForCounts.Attach(entity);
                    ctx.Entry(entity).State = EntityState.Modified;
                }
                await ctx.SaveChangesAsync();

                model = await GetByLocationAsync(reportId, locationId);

                var description = $"Source: {url}, Reporting Year-end: {model.ReportYear}, Department: {model.Department}, Location: {model.Location}";
                await _notificationMessageService.NotifyUsers("Custodian Count", "Submit", description, user, date);

                return model;
            }
        });

        public ValueTask<CustodianReportSubmitForCountVM> UnsubmitAsync(Guid? reportId, Guid? locationId, string url, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (reportId == null)
            {
                throw new NullException();
            }
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var model = await GetByLocationAsync(reportId, locationId);
                if (model == null)
                {
                    throw new NotFoundException("No submitted records found for this department/location.");
                }

                if (model.Status != "Submit")
                {
                    throw new RecordAlreadyExistsException("Department/Location", "Status is already unsubmit.");
                }

                model.UpdatedBy = user;
                model.UpdatedDt = date;
                model.Status = "Unsubmit";

                var entity = await ctx.CustodianReportSubmitForCounts.FindAsync(model.Id);
                entity.Status = model.Status;
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                ctx.CustodianReportSubmitForCounts.Attach(entity);
                ctx.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                var description = $"Source: {url}, Reporting Year-end: {model.ReportYear}, Department: {model.Department}, Location: {model.Location}";
                await _notificationMessageService.NotifyUsers("Custodian Count", "Unsubmit", description, user, date);

                return model;
            }
        });

        private void ValidateIfNull(CustodianReportSubmitForCountVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianReportSubmitForCount entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }        
    }
}