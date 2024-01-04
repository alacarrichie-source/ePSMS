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
    public interface IRpceffoppeItemService
    {
        IQueryable<RPCEFFOPPEItemVM> GetByRpcId(Guid? rpcId);
        ValueTask<RPCEFFOPPEItem> GetByIdAsync(Guid? id);
        ValueTask<RPCEFFOPPEItemVM> CreateAsync(RPCEFFOPPEItemVM model, string user, DateTime date);
        ValueTask<RPCEFFOPPEItemVM> UpdateAsync(RPCEFFOPPEItemVM model, string user, DateTime date);
        ValueTask<RPCEFFOPPEItemVM> DeleteAsync(RPCEFFOPPEItemVM model, string user, DateTime date);
    }

    public class RpceffoppeItemService : IRpceffoppeItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RPCEFFOPPEItemVM> _vmExceptionService = new ExceptionService<RPCEFFOPPEItemVM>();
        private readonly IExceptionService<RPCEFFOPPEItem> _exceptionService = new ExceptionService<RPCEFFOPPEItem>();
        private readonly IOrderService _orderService;

        public RpceffoppeItemService(AppManEntities db)
        {
            _db = db;
            _orderService = new OrderService(db);
        }

        public ValueTask<RPCEFFOPPEItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RPCEFFOPPEItems.FindAsync(id);
            return data;
        });

        public IQueryable<RPCEFFOPPEItemVM> GetByRpcId(Guid? rpcId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RPCEFFOPPEItems.Where(w => w.RpceffoppeId == rpcId)
                .Select(s => new RPCEFFOPPEItemVM
                {
                    Id = s.Id,
                    Fund = s.Fund,
                    ItemType = s.ItemType,
                    RpceffoppeId = s.RpceffoppeId,
                    Article = s.Article,
                    Description = s.Description,
                    PropertyNo = s.PropertyNo,
                    Cost = s.Cost,
                    Location = s.Location,
                    Condition = s.Condition,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RPCEFFOPPEItemVM> CreateAsync(RPCEFFOPPEItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RPCEFFOPPEItem()
            {
                Id = model.Id,
                RpceffoppeId = model.RpceffoppeId,
                Fund = model.Fund,
                ItemType = model.ItemType,
                Article = model.Article,
                Description = model.Description,
                PropertyNo = model.PropertyNo,
                Cost = model.Cost,
                Location = model.Location,
                Condition = model.Condition,
                Remarks = model.Remarks,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RPCEFFOPPEItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RPCEFFOPPEItemVM> DeleteAsync(RPCEFFOPPEItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var entity = await _db.RPCEFFOPPEItems.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCEFFOPPEItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RPCEFFOPPEItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RPCEFFOPPEItemVM> UpdateAsync(RPCEFFOPPEItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var entity = await _db.RPCEFFOPPEItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.Article = model.Article;
            entity.Fund = model.Fund;
            entity.ItemType = model.ItemType;
            entity.Description = model.Description;
            entity.PropertyNo = model.PropertyNo;
            entity.Cost = model.Cost;
            entity.Location = model.Location;
            entity.Condition = model.Condition;
            entity.Remarks = model.Remarks;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCEFFOPPEItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}