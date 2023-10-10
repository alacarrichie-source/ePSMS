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
    
    public class DepartmentUuserService : IDepartmentUserService
    {
        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<DepartmentUserVM> _vmExceptionService = new ExceptionService<DepartmentUserVM>();
        private readonly IExceptionService<DepartmentUser> _exceptionService = new ExceptionService<DepartmentUser>();

        public DepartmentUuserService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<DepartmentUserVM> GetAllByDeptId(Guid? deptId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = db.DepartmentUsers.Where(w => w.DeptId == deptId)
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
        _vmExceptionService.TryCatchAsync(async () =>
        {
            if (db.DepartmentUsers.Any(a => a.DeptId == model.DeptId && a.UserId == a.UserId))
            {
                throw new RecordAlreadyExistsException("User already exists in this Department");
            }

            if (model.UserId == null)
            {
                throw new InvalidValueException("{0} is Required", nameof(model.UserId));
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

            db.DepartmentUsers.Add(entity);
            await db.SaveChangesAsync();            
            return model;
        });

        public ValueTask<DepartmentUserVM> DeleteAsync(DepartmentUserVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            if (db.DepartmentUsers.Find(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            DepartmentUser entity = await db.DepartmentUsers.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.DepartmentUsers.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.DepartmentUsers.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();            
            return model;
        });

        public ValueTask<DepartmentUserVM> UpdateAsync(DepartmentUserVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {            
            if (db.DepartmentUsers.Find(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (db.DepartmentUsers.Any(a => a.DeptId == model.Id && a.UserId == model.UserId && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException("User already exist in this department!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            DepartmentUser entity = await db.DepartmentUsers.FindAsync(model.Id);

            entity.UserId = model.UserId;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.DepartmentUsers.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
            
            return model;
        });
    }
}