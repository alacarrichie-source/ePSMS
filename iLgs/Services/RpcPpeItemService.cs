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
    public interface IRpcPpeItemService
    {
        IQueryable<RpcPpeItemVM> GetByRpcId(Guid? rpcId);
        ValueTask<RpcPpeItem> GetByIdAsync(Guid? id);
        ValueTask<RpcPpeItemVM> CreateAsync(RpcPpeItemVM model, string user, DateTime date);
        ValueTask<RpcPpeItemVM> UpdateAsync(RpcPpeItemVM model, string user, DateTime date);
        ValueTask<RpcPpeItemVM> DeleteAsync(RpcPpeItemVM model, string user, DateTime date);
    }

    public class RpcPpeItemService : IRpcPpeItemService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RpcPpeItemVM> _vmExceptionService = new ExceptionService<RpcPpeItemVM>();
        private readonly IExceptionService<RpcPpeItem> _exceptionService = new ExceptionService<RpcPpeItem>();
        private readonly IOrderService _orderService;

        public RpcPpeItemService(AppManEntities db)
        {
            _db = db;
            _orderService = new OrderService(_db);
        }

        public ValueTask<RpcPpeItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RpcPpeItems.FindAsync(id);
            return data;
        });

        public IQueryable<RpcPpeItemVM> GetByRpcId(Guid? rpcId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RpcPpeItems.Where(w => w.RpcPpeId == rpcId)
                .Select(s => new RpcPpeItemVM
                {
                    Id = s.Id,
                    RpcPpeId = s.RpcPpeId,
                    Fund = s.Fund,
                    Account = s.Account,
                    Article = s.Article,
                    SubArticle = s.SubArticle,
                    Type = s.Type,
                    Brand = s.Brand,
                    Model_ = s.Model_,
                    SerialNo = s.SerialNo,
                    Others = s.Others,
                    Color = s.Color,
                    PropNo = s.PropNo,
                    OldPropNo = s.OldPropNo,
                    Cost = s.Cost,
                    AcqDate = s.AcqDate,
                    ActMode = s.ActMode,
                    Location = s.Location,
                    IssuedTo = s.IssuedTo,
                    RefNo = s.RefNo,
                    RefType = s.RefType,
                    Officer = s.Officer,
                    OldOfficer = s.OldOfficer,
                    Condition = s.Condition,
                    Remarks = s.Remarks,
                    Annex = s.Annex,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RpcPpeItemVM> CreateAsync(RpcPpeItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RpcPpeItem()
            {
                Id = model.Id,
                RpcPpeId = model.RpcPpeId,
                Fund = model.Fund,
                Account = model.Account,
                Article = model.Article,
                SubArticle = model.SubArticle,
                Type = model.Type,
                Brand = model.Brand,
                Model_ = model.Model_,
                SerialNo = model.SerialNo,
                Others = model.Others,
                Color = model.Color,
                PropNo = model.PropNo,
                OldPropNo = model.OldPropNo,
                Cost = model.Cost,
                AcqDate = model.AcqDate,
                ActMode = model.ActMode,
                Location = model.Location,
                IssuedTo = model.IssuedTo,
                RefNo = model.RefNo,
                RefType = model.RefType,
                Officer = model.Officer,
                OldOfficer = model.OldOfficer,
                Condition = model.Condition,
                Remarks = model.Remarks,
                Annex = model.Annex,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RpcPpeItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RpcPpeItemVM> DeleteAsync(RpcPpeItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RpcPpeItems.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RpcPpeItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RpcPpeItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RpcPpeItemVM> UpdateAsync(RpcPpeItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RpcPpeItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.Fund = model.Fund;
            entity.Account = model.Account;
            entity.Article = model.Article;
            entity.SubArticle = model.SubArticle;
            entity.Type = model.Type;
            entity.Brand = model.Brand;
            entity.Model_ = model.Model_;
            entity.SerialNo = model.SerialNo;
            entity.Others = model.Others;
            entity.Color = model.Color;
            entity.PropNo = model.PropNo;
            entity.OldPropNo = model.OldPropNo;
            entity.Cost = model.Cost;
            entity.AcqDate = model.AcqDate;
            entity.ActMode = model.ActMode;
            entity.Location = model.Location;
            entity.IssuedTo = model.IssuedTo;
            entity.RefNo = model.RefNo;
            entity.RefType = model.RefType;
            entity.Officer = model.Officer;
            entity.OldOfficer = model.OldOfficer;
            entity.Condition = model.Condition;
            entity.Remarks = model.Remarks;
            entity.Annex = model.Annex;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RpcPpeItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}