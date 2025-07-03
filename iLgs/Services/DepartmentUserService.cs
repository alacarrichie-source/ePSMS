using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IDepartmentUserService
    {
        IQueryable<DepartmentUserVM> GetAllByDeptId(Guid? deptId);
        ValueTask<DepartmentUserVM> CreateAsync(DepartmentUserVM model, string user, DateTime date);
        ValueTask<DepartmentUserVM> UpdateAsync(DepartmentUserVM model, string user, DateTime date);
        ValueTask<DepartmentUserVM> DeleteAsync(DepartmentUserVM model, string user, DateTime date);
    }

    public class DepartmentUserService : IDepartmentUserService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<DepartmentUserVM> _vmExceptionService;
        private readonly IExceptionService<DepartmentUser> _exceptionService;

        public DepartmentUserService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<DepartmentUserVM> vmExceptionService,
            IExceptionService<DepartmentUser> exceptionService)
        {
            _db = db;
            _exceptions = exceptions;
            _vmExceptionService = vmExceptionService;
            _exceptionService = exceptionService;
        }

        public IQueryable<DepartmentUserVM> GetAllByDeptId(Guid? deptId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.DepartmentUsers.Where(w => w.DeptId == deptId)
                .Select(s => new DepartmentUserVM
                {
                    Id = s.Id,
                    DeptId = s.DeptId,
                    UserId = s.UserId,
                    Email = s.AspNetUser.Email,
                    UserName = s.AspNetUser.UserName,
                    NameFull = s.AspNetUser.UserProfile.NameFull
                });
            return data;
        });

                
        public ValueTask<DepartmentUserVM> CreateAsync(DepartmentUserVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (model.UserId == null)
            {
                throw new InvalidValueException("{0} is Required", nameof(model.UserId));
            }

            if (_db.DepartmentUsers.Any(a => a.DeptId == model.DeptId && a.UserId == model.UserId))
            {
                throw new RecordAlreadyExistsException("User already exists!");
            }            

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            DepartmentUser entity = new DepartmentUser()
            {
                Id = model.Id,
                DeptId = model.DeptId,
                UserId = model.UserId,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.DepartmentUsers.Add(entity);
            await _db.SaveChangesAsync();            
            return model;
        });

        public ValueTask<DepartmentUserVM> DeleteAsync(DepartmentUserVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (_db.DepartmentUsers.Find(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            DepartmentUser entity = await _db.DepartmentUsers.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.DepartmentUsers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.DepartmentUsers.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();            
            return model;
        });

        public ValueTask<DepartmentUserVM> UpdateAsync(DepartmentUserVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {            
            if (_db.DepartmentUsers.Find(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (_db.DepartmentUsers.Any(a => a.DeptId == model.DeptId && a.UserId == model.UserId && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException("User already exist!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            DepartmentUser entity = await _db.DepartmentUsers.FindAsync(model.Id);

            entity.UserId = model.UserId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.DepartmentUsers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            
            return model;
        });
    }
}