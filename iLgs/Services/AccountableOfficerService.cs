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
    public interface IAccountableOfficerService
    {
        IQueryable<AccountableOfficerVM> GetAllByLocationId(Guid? deptId);
        ValueTask<AccountableOfficerVM> CreateAsync(AccountableOfficerVM model, string user, DateTime date);
        ValueTask<AccountableOfficerVM> UpdateAsync(AccountableOfficerVM model, string user, DateTime date);
        ValueTask<AccountableOfficerVM> DeleteAsync(AccountableOfficerVM model, string user, DateTime date);
    }

    public class AccountableOfficerService : IAccountableOfficerService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<AccountableOfficerVM> _vmExceptionService = new ExceptionService<AccountableOfficerVM>();
        private readonly IExceptionService<AccountableOfficer> _exceptionService = new ExceptionService<AccountableOfficer>();

        public AccountableOfficerService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<AccountableOfficerVM> GetAllByLocationId(Guid? locationId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.AccountableOfficers.Where(w => w.LocationId == locationId)
                .Select(s => new AccountableOfficerVM
                {
                    Id = s.Id,
                    LocationId = s.LocationId,
                    Name = s.Name,
                    Designation = s.Designation,
                    DateAssumption = s.DateAssumption
                });
            return data;
        });


        public ValueTask<AccountableOfficerVM> CreateAsync(AccountableOfficerVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (_db.AccountableOfficers.Any(a => a.LocationId == model.LocationId && a.Name == model.Name && a.Designation == model.Designation && a.DateAssumption ==  model.DateAssumption))
            {
                throw new RecordAlreadyExistsException("Name/Designation already exists in this Department");
            }

            if (model.Name == null)
            {
                throw new InvalidValueException("{0} is Required", nameof(model.Name));
            }

            if (model.Designation == null)
            {
                throw new InvalidValueException("{0} is Required", nameof(model.Designation));
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            AccountableOfficer entity = new AccountableOfficer()
            {
                Id = model.Id,
                LocationId = model.LocationId,
                Name = model.Name,
                Designation = model.Designation,
                DateAssumption = model.DateAssumption,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.AccountableOfficers.Add(entity);
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<AccountableOfficerVM> DeleteAsync(AccountableOfficerVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (_db.AccountableOfficers.Find(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AccountableOfficer entity = await _db.AccountableOfficers.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AccountableOfficers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AccountableOfficers.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<AccountableOfficerVM> UpdateAsync(AccountableOfficerVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (_db.AccountableOfficers.Find(model.Id) == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (_db.AccountableOfficers.Any(a => a.LocationId == model.LocationId && a.Name == model.Name && a.Designation == model.Designation 
                && a.DateAssumption == model.DateAssumption && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException("Name/Designation already exist in this department!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AccountableOfficer entity = await _db.AccountableOfficers.FindAsync(model.Id);

            entity.LocationId = model.LocationId;
            entity.Name = model.Name;
            entity.Designation = model.Designation;
            entity.DateAssumption = model.DateAssumption;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AccountableOfficers.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });
    }
}