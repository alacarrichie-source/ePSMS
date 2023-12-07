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
    public interface IRpciService
    {
        IQueryable<RPCI_VM> GetAll();
        ValueTask<RPCI> GetByIdAsync(Guid? id);
        ValueTask<RPCI_VM> GetByAsAtAsync(DateTime? asAt);
        ValueTask<RPCI_VM> GenerateAsync(RPCI_VM model, string user, DateTime date);
        ValueTask<RPCI_VM> CreateAsync(RPCI_VM model, string user, DateTime date);
        ValueTask<RPCI_VM> UpdateAsync(RPCI_VM model, string user, DateTime date);
        ValueTask<RPCI_VM> DeleteAsync(RPCI_VM model, string user, DateTime date);
    }

    public class RpciService : IRpciService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RPCI_VM> _vmExceptionService = new ExceptionService<RPCI_VM>();
        private readonly IExceptionService<RPCI> _exceptionService = new ExceptionService<RPCI>();
        private readonly IOrderService _orderService;

        public RpciService(AppManEntities db)
        {
            _db = db;
            _orderService = new OrderService(db);
        }

        public ValueTask<RPCI> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RPCIs.FindAsync(id);
            return data;
        });

        public ValueTask<RPCI_VM> GetByAsAtAsync(DateTime? asAt) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RPCIs.Where(w => w.AsAt == asAt)
                .Select(s => new RPCI_VM
                {
                    Id = s.Id,
                    ItemType = s.ItemType,
                    AsAt = s.AsAt,
                    Fund = s.Fund,
                    AccountableOfficer = s.AccountableOfficer,
                    Designation = s.Designation,
                    AssumptionDt = s.AssumptionDt,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    ApprovedBy = s.ApprovedBy,
                    VerifiedBy = s.VerifiedBy,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<RPCI_VM> GetAll() =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RPCIs
                .Select(s => new RPCI_VM
                {
                    Id = s.Id,
                    ItemType = s.ItemType,
                    AsAt = s.AsAt,
                    Fund = s.Fund,
                    AccountableOfficer = s.AccountableOfficer,
                    Designation = s.Designation,
                    AssumptionDt = s.AssumptionDt,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    ApprovedBy = s.ApprovedBy,
                    VerifiedBy = s.VerifiedBy,
                    PostedBy = s.PostedBy,    
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RPCI_VM> GenerateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {

            await _db.Database.ExecuteSqlCommandAsync("Exec RPCI_Generate {0}, {1}", model.AsAt, user);
            model = await GetByAsAtAsync(model.AsAt);
            return model;
        });

        public ValueTask<RPCI_VM> CreateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {            
            var notPosted = await _orderService.GetNotPostedAsync((DateTime)model.AsAt);
            if (notPosted > 0)
            {
                throw new RecordRelationshipException(string.Format("The system found {0} that are not yet posted as of date specified! Please post before proceeding..."));
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RPCI()
            {
                Id = model.Id,
                ItemType = model.ItemType,
                AsAt = model.AsAt,
                Fund = model.Fund,
                AccountableOfficer = model.AccountableOfficer, 
                Designation = model.Designation,
                AssumptionDt = model.AssumptionDt,
                CertifiedCorrectBy = model.CertifiedCorrectBy,
                ApprovedBy = model.ApprovedBy,
                VerifiedBy = model.VerifiedBy,
                PostedBy = model.PostedBy,
                PostedDt = model.PostedDt,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RPCIs.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RPCI_VM> DeleteAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var entity = await _db.RPCIs.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }
            

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RPCIs.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RPCI_VM> UpdateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var entity = await _db.RPCIs.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }
            
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.ItemType = model.ItemType;
            entity.AsAt = model.AsAt;
            entity.Fund = model.Fund;
            entity.AccountableOfficer = model.AccountableOfficer;
            entity.Designation = model.Designation;
            entity.AssumptionDt = model.AssumptionDt;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.ApprovedBy = model.ApprovedBy;
            entity.VerifiedBy = model.VerifiedBy;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}