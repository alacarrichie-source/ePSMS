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
        IQueryable<RPCIItem> GetRpciXls(DateTime? asOf, Guid? id);
        ValueTask<RPCI> GetByIdAsync(Guid? id);
        ValueTask<RPCI_VM> GetByAsOfAsync(DateTime? AsOf);
        ValueTask<RPCI_VM> GenerateAsync(RPCI_VM model, string user, DateTime date);
        ValueTask<RPCI> PostAsync(Guid? id, string user, DateTime date);
        ValueTask<RPCI> UnPostAsync(Guid? id, string user, DateTime date);
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
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RPCIs.FindAsync(id);
            return data;
        });

        public IQueryable<RPCIItem> GetRpciXls(DateTime? asOf, Guid? id) 
        {
            var data = _db.RPCIItems.Include(i => i.RPCI).Where(w => w.RPCI.AsOf == asOf && (id == null || w.RPCI.Id == id))
                .OrderBy(o => o.RPCI.Fund).ThenBy(o => o.PoNo);
                  
            return data;
        }

        public ValueTask<RPCI_VM> GetByAsOfAsync(DateTime? AsOf) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.RPCIs.Where(w => w.AsOf == AsOf)
                .Select(s => new RPCI_VM
                {
                    Id = s.Id,
                    AsOf = s.AsOf,
                    Fund = s.Fund,
                    FromDonation = s.FromDonation,
                    InvDist = s.InvDist,
                    ItemTypeId = s.ItemTypeId,
                    Account = s.Account,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    DeptId = s.DeptId,
                    Department = s.Department,
                    ApprovedBy = s.ApprovedBy,
                    VerifiedBy = s.VerifiedBy,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt,
                    InvDistDesc = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    AcqMode = s.FromDonation == true ? "From Donation" : "Purchase"
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
                    AsOf = s.AsOf,
                    Fund = s.Fund,
                    FromDonation = s.FromDonation,
                    InvDist = s.InvDist,
                    ItemTypeId = s.ItemTypeId,
                    Account = s.Account,
                    DeptId = s.DeptId,
                    Department = s.Department,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    ApprovedBy = s.ApprovedBy,
                    VerifiedBy = s.VerifiedBy,
                    PostedBy = s.PostedBy,    
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt,
                    InvDistDesc = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    AcqMode = s.FromDonation == true ? "From Donation" : "Purchase"
                });
            return data;
        });

        public ValueTask<RPCI_VM> GenerateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (model.DeptId != null) {                 
                if (await _db.RPCIs.AnyAsync(a => a.AsOf == model.AsOf && a.Fund == model.Fund && a.FromDonation == model.FromDonation
                     && a.InvDist == model.InvDist && a.ItemTypeId == model.ItemTypeId
                     && a.Account == model.Account && a.DeptId == model.DeptId))
                {
                    throw new RecordAlreadyExistsException();
                }
            }

            await _db.Database.ExecuteSqlCommandAsync("Exec RPCI_Generate {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", 
                model.AsOf, model.Fund, model.FromDonation, model.InvDist, model.ItemTypeId, model.Account, model.DeptId, user);
            model = await GetByAsOfAsync(model.AsOf);
            return model;
        });

        public ValueTask<RPCI> PostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}");
            }

            if (string.IsNullOrWhiteSpace(entity.CertifiedCorrectBy))
            {
                throw new InvalidValueException("Certified Correct by is Required!");
            }

            if (string.IsNullOrWhiteSpace(entity.ApprovedBy))
            {
                throw new InvalidValueException("Aporoved by is Required!");
            }

            if (string.IsNullOrWhiteSpace(entity.VerifiedBy))
            {
                throw new InvalidValueException("Verified by is Required!");
            }
            

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<RPCI> UnPostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
                                    
            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<RPCI_VM> CreateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {            
            var notPosted = await _orderService.GetNotPostedAsync((DateTime)model.AsOf);
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
                AsOf = model.AsOf,
                Fund = model.Fund,
                FromDonation = model.FromDonation,
                InvDist = model.InvDist,
                ItemTypeId = model.ItemTypeId,
                Account = model.Account,                
                DeptId = model.DeptId,
                Department = model.Department,
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
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot delete!");
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
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.AsOf = model.AsOf;
            entity.Fund = model.Fund;
            entity.FromDonation = model.FromDonation;
            entity.InvDist = model.InvDist;
            entity.ItemTypeId = model.ItemTypeId;
            entity.Account = model.Account;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
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