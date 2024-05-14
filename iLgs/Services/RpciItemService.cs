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
    public interface IRpciItemService
    {
        IQueryable<RPCIItemVM> GetByRpciId(Guid? rpciId);
        ValueTask<RPCIItem> GetByIdAsync(Guid? id);
        ValueTask<RPCIItemVM> CreateAsync(RPCIItemVM model, string user, DateTime date);
        ValueTask<RPCIItemVM> UpdateAsync(RPCIItemVM model, string user, DateTime date);
        ValueTask<RPCIItemVM> DeleteAsync(RPCIItemVM model, string user, DateTime date);
    }

    public class RpciItemService : IRpciItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RPCIItemVM> _vmExceptionService = new ExceptionService<RPCIItemVM>();
        private readonly IExceptionService<RPCIItem> _exceptionService = new ExceptionService<RPCIItem>();
        private readonly IOrderService _orderService;

        public RpciItemService(AppManEntities db)
        {
            _db = db;
            _orderService = new OrderService(db);
        }

        public ValueTask<RPCIItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RPCIItems.FindAsync(id);
            return data;
        });

        public IQueryable<RPCIItemVM> GetByRpciId(Guid? rpciId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RPCIItems.Where(w => w.RpciId == rpciId)
                .Select(s => new RPCIItemVM
                {
                    Id = s.Id,
                    RpciId = s.RpciId,
                    ItemType = s.ItemType,
                    Fund = s.Fund,
                    Article = s.Article,                                        
                    Description = s.Description,
                    Brand = s.Brand,
                    RefNo = s.RefNo,
                    RefDate = s.RefDate,
                    OldStockNo = s.OldStockNo,
                    StockNo = s.StockNo,
                    Unit = s.Unit,
                    UnitValue = s.UnitValue,
                    QtyBalance = s.QtyBalance,
                    QtyOnHand = s.QtyOnHand,
                    QtyShortOver = s.QtyShortOver,
                    ValueShortOver = s.ValueShortOver,
                    Remarks = s.Remarks,
                    AirNo = s.AirNo,
                    AirDate = s.AirDate,
                    LocationId = s.LocationId,
                    OfficerId = s.OfficerId,
                    Model_ = s.Model_,
                    Location = s.Codextn.Description,
                    Officer = s.AccountableOfficer.Name,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<RPCIItemVM> CreateAsync(RPCIItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RPCIItem()
            {
                Id = model.Id,
                RpciId = model.RpciId,
                ItemType = model.ItemType,                
                Fund = model.Fund,
                Article = model.Article,
                Description = model.Description,
                Brand = model.Brand,
                RefNo = model.RefNo,
                RefDate = model.RefDate,        
                OldStockNo = model.OldStockNo,
                StockNo = model.StockNo,
                Unit = model.Unit,
                UnitValue = model.UnitValue,
                QtyBalance = model.QtyBalance,
                QtyOnHand = model.QtyOnHand,
                QtyShortOver = model.QtyShortOver,
                ValueShortOver = model.ValueShortOver,
                Remarks = model.Remarks,
                AirNo = model.AirNo,
                AirDate = model.AirDate,
                LocationId = model.LocationId,
                OfficerId = model.OfficerId,
                Model_ = model.Model_,                
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RPCIItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });        

        public ValueTask<RPCIItemVM> UpdateAsync(RPCIItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var entity = await _db.RPCIItems.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.RpciId = model.RpciId;            
            entity.ItemType = model.ItemType;
            entity.Fund = model.Fund;
            entity.Article = model.Article;
            entity.Description = model.Description;
            entity.Brand = model.Brand;
            entity.RefNo = model.RefNo;
            entity.RefDate = model.RefDate;
            entity.OldStockNo = model.OldStockNo;
            entity.StockNo = model.StockNo;
            entity.Unit = model.Unit;
            entity.UnitValue = model.UnitValue;
            entity.QtyBalance = model.QtyBalance;
            entity.QtyOnHand = model.QtyOnHand;
            entity.QtyShortOver = model.QtyShortOver;
            entity.ValueShortOver = model.ValueShortOver;
            entity.Remarks = model.Remarks;
            entity.AirNo = model.AirNo;
            entity.AirDate = model.AirDate;
            entity.LocationId = model.LocationId;
            entity.OfficerId = model.OfficerId;
            entity.Model_ = model.Model_;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<RPCIItemVM> DeleteAsync(RPCIItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var entity = await _db.RPCIItems.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RPCIItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });
    }
}