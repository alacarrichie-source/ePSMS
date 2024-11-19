using ClosedXML.Excel;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<CustodianReportItemIssuance> _exceptionService = new ExceptionService<CustodianReportItemIssuance>();
        private readonly IUserService _userService;

        public CustodianReportItemIssuanceService(AppManEntities db)
        {
            _db = db;
            _userService = new UserService(_db);
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
            ValidateIfPosted(model.ReportItemId);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new CustodianReportItemIssuance();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.CustodianReportItemIssuances.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public virtual async ValueTask<CustodianReportItemIssuance> UpdateAsync(CustodianReportItemIssuance model, string user, DateTime date)
        {
            ValidateIfNull(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportItemIssuances.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity.ReportItemId);
            ValidateUser(entity, model);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.CustodianReportItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public virtual async ValueTask<CustodianReportItemIssuance> DeleteAsync(CustodianReportItemIssuance model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.CustodianReportItemIssuances.FindAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity.ReportItemId);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianReportItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianReportItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
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

        private void ValidateIfPosted(Guid? reportItemId)
        {
            var reportItem = _db.CustodianReportItems.Include(i => i.CustodianReport).Where(w => w.Id == reportItemId).SingleOrDefault();
            if (reportItem.CustodianReport.PostedDt != null)
            {
                var msg = $"Record already posted by {reportItem.CustodianReport.PostedBy} on {reportItem.CustodianReport.PostedDt}, cannot update!";
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