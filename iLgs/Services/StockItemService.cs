//using iLgs.Exceptions;
//using iLgs.Models;
//using iLgs.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services
//{
//    public interface IStockItemService
//    {
//        IQueryable<StockItemVM> GetByCardId(Guid? cardId);
//        ValueTask<StockItemVM> GetByIdAsync(Guid? id);

//        ValueTask<StockItemVM> CreateAsync(StockItemVM model, string user, DateTime date);
//        ValueTask<StockItemVM> UpdateAsync(StockItemVM model, string user, DateTime date);
//        ValueTask<StockItemVM> DeleteAsync(StockItemVM model, string user, DateTime date);
//    }

//    public class StockItemService : IStockItemService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly IExceptionService<StockItemVM> _VmExceptionService = new ExceptionService<StockItemVM>();

//        public StockItemService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public ValueTask<StockItemVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
//        {
//            var data = await _db.StockItems.Where(w => w.Id == id)
//                .Select(s => new StockItemVM
//                {
//                    Id = s.Id,
//                    CardId = s.CardId,
//                    OrderItemId = s.OrderItemId,
//                    RefDate = s.RefDate,
//                    RefNo = s.RefNo,
//                    RefType = s.RefType,
//                    Qty = s.Qty,
//                    QtyIss = s.QtyIss,
//                    QtyBal = s.QtyBal,
//                    Days = s.Days,
//                    UnitCost = s.UnitCost,
//                    UnitMeas = s.UnitMeas,
//                    Remarks = s.Remarks,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt
//                }).FirstOrDefaultAsync();
//            return data;
//        });

//        public IQueryable<StockItemVM> GetByCardId(Guid? cardId) => _VmExceptionService.TryCatch(() =>
//        {
//            var data = _db.StockItems.Where(w => w.CardId == cardId)
//                .Select(s => new StockItemVM
//                {
//                    Id = s.Id,
//                    CardId = s.CardId,
//                    OrderItemId = s.OrderItemId,
//                    RefDate = s.RefDate,
//                    RefNo = s.RefNo,
//                    RefType = s.RefType,
//                    Qty = s.Qty,
//                    QtyIss = s.QtyIss,
//                    QtyBal = s.QtyBal,
//                    Days = s.Days,
//                    UnitCost = s.UnitCost,
//                    UnitMeas = s.UnitMeas,
//                    Remarks = s.Remarks,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });


//        public ValueTask<StockItemVM> CreateAsync(StockItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.UpdatedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedDt = date;

//            var entity = new StockItem
//            {
//                Id = model.Id,
//                CardId = model.CardId,
//                OrderItemId = model.OrderItemId,
//                RefDate = model.RefDate,
//                RefNo = model.RefNo,
//                RefType = model.RefType,
//                Qty = model.Qty,
//                QtyIss = model.QtyIss,
//                QtyBal = model.QtyBal,
//                Days = model.Days,
//                UnitCost = model.UnitCost,
//                UnitMeas = model.UnitMeas,
//                Remarks = model.Remarks,
//                Amount = model.Amount,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.StockItems.Add(entity);
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<StockItemVM> DeleteAsync(StockItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.StockItems.FindAsync(model.Id);

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.StockItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.StockItems.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<StockItemVM> UpdateAsync(StockItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.StockItems.FindAsync(model.Id);
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            entity.CardId = model.CardId;
//            entity.OrderItemId = model.OrderItemId;
//            entity.RefDate = model.RefDate;
//            entity.RefNo = model.RefNo;
//            entity.RefType = model.RefType;
//            entity.Qty = model.Qty;
//            entity.QtyIss = model.QtyIss;
//            entity.QtyBal = model.QtyBal;
//            entity.Days = model.Days;
//            entity.UnitCost = model.UnitCost;
//            entity.UnitMeas = model.UnitMeas;
//            entity.Remarks = model.Remarks;
//            entity.Amount = model.Amount;
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.StockItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//    }
//}