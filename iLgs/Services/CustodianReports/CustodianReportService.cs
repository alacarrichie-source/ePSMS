using ClosedXML.Excel;
using iLgs.Controllers;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
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
    public interface ICustodianReportService
    {
        IQueryable<CustodianReport> GetAll();
        IQueryable<CustodianReport> GetAllByAccountGroup(int? accountGroup);
        IQueryable<CustodianReport> GetAllByDepartmentAccountGroup(Guid? deptId, int? accountGroup);        
        ValueTask<CustodianReport> GetByIdAsync(Guid? id);
        ValueTask<CustodianReport> PostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReport> UnPostAsync(Guid id, string user, DateTime date);
        ValueTask<CustodianReport> CreateAsync(CustodianReport model, string user, DateTime date);
        ValueTask<CustodianReport> UpdateAsync(CustodianReport model, string user, DateTime date);
        ValueTask<CustodianReport> DeleteAsync(CustodianReport model, string user, DateTime date);
    }

    public class CustodianReportService : ICustodianReportService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<CustodianReport> _exceptionService = new ExceptionService<CustodianReport>();
        private readonly ICustodianReportValidator _validator;
        private readonly IUserService _userService;

        public CustodianReportService(AppManEntities db)
        {
            _db = db;
            _validator = new CustodianReportValidator(db);
            _userService = new UserService(db);
        }

        public ValueTask<CustodianReport> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReports.FindAsync(id);
            return data;
        });


        public ValueTask<CustodianReport> GetByAsOfAsync(DateTime? AsOf) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianReports.Where(w => w.AsOf == AsOf).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianReport> GetAll() =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReports.AsNoTracking().AsQueryable();
            return data;
        });

        public IQueryable<CustodianReport> GetAllByAccountGroup(int? accountGroup) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReports.AsNoTracking().Where(w => w.AccountGroup == accountGroup).AsQueryable();
            return data;
        });

        public IQueryable<CustodianReport> GetAllByDepartmentAccountGroup(Guid? deptId, int? accountGroup) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianReports.AsNoTracking().Where(w => w.DeptId == deptId && w.AccountGroup == accountGroup).AsQueryable();
            return data;
        });
        
        public ValueTask<CustodianReport> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnPost(id);
            var entity = await _db.CustodianReports.FindAsync(id);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReports.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<CustodianReport> UnPostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnUnpost(id);
            var entity = await _db.CustodianReports.FindAsync(id);

            //// check if in disposal
            //if(_db.CustodianReportItems.Where(w => w.Report`Id == id && w.CustodianDisposalItems.Any()).Any())
            //{
            //    throw new RecordAlreadyExistsException("Record already in disposal entry, cannot unpost!");
            //}

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.CustodianReports.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<CustodianReport> CreateAsync(CustodianReport model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {

            _validator.ValidateOnCreate(model);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new CustodianReport();
            MapModelToEntityFields(entity, model, Mode.ADD);
            
            _db.CustodianReports.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<CustodianReport> UpdateAsync(CustodianReport model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           _validator.ValidateOnUpdate(model);

           var entity = await _db.CustodianReports.FindAsync(model.Id);
           
           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.CustodianReports.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<CustodianReport> DeleteAsync(CustodianReport model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);

            var entity = await _db.CustodianReports.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianReports.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianReports.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });       

        public void MapModelToEntityFields(CustodianReport entity, CustodianReport model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            
            entity.AsOf = model.AsOf;
            entity.Fund = model.Fund;
            entity.AccountGroup = model.AccountGroup;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.ApprovedBy = model.ApprovedBy;
            entity.VerifiedBy = model.VerifiedBy;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }        
    }
}