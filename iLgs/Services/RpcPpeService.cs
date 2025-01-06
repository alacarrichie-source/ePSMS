using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.PurchaseOrder;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IRpcPpeService
    {
        IQueryable<RpcPpeVM> GetAll();
        ValueTask<RpcPpe> GetByIdAsync(Guid? id);
        ValueTask<RpcPpeVM> GetByAsOfAsync(DateTime? asOf);
        ValueTask<RpcPpeVM> GenerateAsync(RpcPpeVM model, string user, DateTime date);
        ValueTask<RpcPpeVM> CreateAsync(RpcPpeVM model, string user, DateTime date);
        ValueTask<RpcPpeVM> UpdateAsync(RpcPpeVM model, string user, DateTime date);
        ValueTask<RpcPpeVM> DeleteAsync(RpcPpeVM model, string user, DateTime date);
    }

    public class RpcPpeService : IRpcPpeService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RpcPpeVM> _vmExceptionService = new ExceptionService<RpcPpeVM>();
        private readonly IExceptionService<RpcPpe> _exceptionService = new ExceptionService<RpcPpe>();
        private readonly IOrderService _orderService;

        public RpcPpeService(AppManEntities db)
        {
            _db = db;
            _orderService = new OrderService(_db);
        }

        public ValueTask<RpcPpe> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RpcPpes.FindAsync(id);
            return data;
        });

        public ValueTask<RpcPpeVM> GetByAsOfAsync(DateTime? asOf) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.RpcPpes.Where(w => w.AsOf == asOf)
                .Select(s => new RpcPpeVM
                {
                    Id = s.Id,
                    AsOf = s.AsOf,
                    Department = s.Department,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    CertifiedCorrectDesignation = s.CertifiedCorrectDesignation,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedDesignation = s.ApprovedDesignation,
                    VerifiedBy = s.VerifiedBy,
                    VerifiedDesignation = s.VerifiedDesignation,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<RpcPpeVM> GetAll() =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RpcPpes
                .Select(s => new RpcPpeVM
                {
                    Id = s.Id,
                    AsOf = s.AsOf,
                    Department = s.Department,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    CertifiedCorrectDesignation = s.CertifiedCorrectDesignation,
                    ApprovedBy = s.ApprovedBy,
                    ApprovedDesignation = s.ApprovedDesignation,
                    VerifiedBy = s.VerifiedBy,
                    VerifiedDesignation = s.VerifiedDesignation,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RpcPpeVM> GenerateAsync(RpcPpeVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {

            await _db.Database.ExecuteSqlCommandAsync("Exec RpcPpe_Generate {0}, {1}, {2}", model.AsOf, model.Department, user);
            model = await GetByAsOfAsync(model.AsOf);
            return model;
        });

        public ValueTask<RpcPpeVM> CreateAsync(RpcPpeVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            //var notPosted = await _orderService.GetNotPostedAsync((DateTime)model.AsAt);
            //if (notPosted > 0)
            //{
            //    throw new RecordRelationshipException(string.Format("The system found {0} that are not yet posted as of date specified! Please post before proceeding..."));
            //}

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RpcPpe()
            {
                Id = model.Id,
                AsOf = model.AsOf,
                Department = model.Department,
                CertifiedCorrectBy = model.CertifiedCorrectBy,
                CertifiedCorrectDesignation = model.CertifiedCorrectDesignation,
                ApprovedBy = model.ApprovedBy,
                ApprovedDesignation = model.ApprovedDesignation,
                VerifiedBy = model.VerifiedBy,
                VerifiedDesignation = model.VerifiedDesignation,
                PostedBy = model.PostedBy,
                PostedDt = model.PostedDt,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RpcPpes.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RpcPpeVM> DeleteAsync(RpcPpeVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RpcPpes.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RpcPpes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RpcPpes.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RpcPpeVM> UpdateAsync(RpcPpeVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RpcPpes.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.AsOf = model.AsOf;
            entity.Department = model.Department;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.CertifiedCorrectDesignation = model.CertifiedCorrectDesignation;
            entity.ApprovedBy = model.ApprovedBy;
            entity.ApprovedDesignation = model.ApprovedDesignation;
            entity.VerifiedBy = model.VerifiedBy;
            entity.VerifiedDesignation = model.VerifiedDesignation;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RpcPpes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}