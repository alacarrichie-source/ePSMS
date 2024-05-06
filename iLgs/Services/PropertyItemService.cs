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
//    public interface IPropertyItemService
//    {
//        IQueryable<PropertyItemVM> GetByPsStockId(Guid? psStockId);
//        ValueTask<PropertyItemVM> GetByIdAsync(Guid? id);
        
//        ValueTask<PropertyItemVM> CreateAsync(PropertyItemVM model, string user, DateTime date);
//        ValueTask<PropertyItemVM> UpdateAsync(PropertyItemVM model, string user, DateTime date);
//        ValueTask<PropertyItemVM> DeleteAsync(PropertyItemVM model, string user, DateTime date);        
//    }

//    public class PropertyItemService : IPropertyItemService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly IExceptionService<PropertyItemVM> _VmExceptionService = new ExceptionService<PropertyItemVM>();
        
//        public PropertyItemService(AppManEntities db)
//        {
//            _db = db;        
//        }

//        public ValueTask<PropertyItemVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
//        {
//            var data = await _db.PsItems.Where(w => w.Id == id)
//                .Select(s => new PropertyItemVM
//                {
//                    Id = s.Id,
//                    Office = s.Office,
//                    Officer = s.Officer,
//                    RefNo = s.RefNo,
//                    RefDate = s.RefDate,
//                    RefType = s.RefType,
//                    UnitMeas = s.UnitMeas,
//                    UnitCost = s.UnitCost,
//                    QtyPo = s.QtyPo,
//                    Qty = s.Qty,
//                    QtyIss = s.QtyIss,
//                    QtyBal = s.QtyBal,
//                    Days = s.Days,
//                    StockNo = s.PsStock.StockNo,
//                    Remarks = s.Remarks,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt
//                }).FirstOrDefaultAsync();
//            return data;
//        });

//        public IQueryable<PropertyItemVM> GetByPsStockId(Guid? psStockId) => _VmExceptionService.TryCatch(() =>
//        {
//            var data = _db.PsItems.Where(w => w.PsStockId == psStockId)
//                .Select(s => new PropertyItemVM
//                {
//                    Id = s.Id,
//                    Office = s.Office,
//                    Officer = s.Officer,
//                    RefNo = s.RefNo,
//                    RefDate = s.RefDate,
//                    RefType = s.RefType,
//                    UnitMeas = s.UnitMeas,
//                    UnitCost = s.UnitCost,
//                    QtyPo = s.QtyPo,
//                    Qty = s.Qty,
//                    QtyIss = s.QtyIss,
//                    QtyBal = s.QtyBal,
//                    Days = s.Days,
//                    StockNo = s.PsStock.StockNo,
//                    Remarks = s.Remarks,
//                    Amount = s.Amount,
//                    InsertedDt = s.InsertedDt
//                });
//            return data;
//        });

        
//        public ValueTask<PropertyItemVM> CreateAsync(PropertyItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {            
//            model.Id = Guid.NewGuid();
//            model.InsertedBy = user;
//            model.UpdatedBy = user;
//            model.InsertedDt = date;
//            model.UpdatedDt = date;
            
//            var entity = new PsItem()
//            {
//                Id = model.Id,
//                PsStockId = model.PsStockId,
//                OrderItemId = model.OrderItemId,
//                Office = model.Office,
//                Officer = model.Officer,
//                RefNo = model.RefNo,
//                RefDate = model.RefDate,
//                RefType = model.RefType,
//                UnitMeas = model.UnitMeas,
//                UnitCost = model.UnitCost,
//                QtyPo = model.QtyPo,
//                Qty = model.Qty,
//                QtyIss = model.QtyIss,
//                QtyBal = model.QtyBal,
//                Days = model.Days,
//                Remarks = model.Remarks,
//                Amount = model.Amount,
//                InsertedBy = model.InsertedBy,
//                InsertedDt = model.InsertedDt,
//                UpdatedBy = model.UpdatedBy,
//                UpdatedDt = model.UpdatedDt
//            };

//            _db.PsItems.Add(entity);
//            await _db.SaveChangesAsync();
            
//            return model;
//        });

//        public ValueTask<PropertyItemVM> DeleteAsync(PropertyItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {            
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;
            
//            var entity = await _db.PsItems.FindAsync(model.Id);

//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.PsItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            _db.PsItems.Remove(entity);
//            _db.Entry(entity).State = EntityState.Deleted;
//            await _db.SaveChangesAsync();

//            return model;
//        });

//        public ValueTask<PropertyItemVM> UpdateAsync(PropertyItemVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
//        {            
//            model.UpdatedBy = user;
//            model.UpdatedDt = date;

//            var entity = await _db.PsItems.FindAsync(model.Id);
//            if (entity == null)
//            {
//                throw new RecordNotFoundException(model.Id);
//            }

//            entity.OrderItemId = model.OrderItemId;
//            entity.Office = model.Office;
//            entity.Officer = model.Officer;
//            entity.RefNo = model.RefNo;
//            entity.RefDate = model.RefDate;
//            entity.RefType = model.RefType;
//            entity.UnitMeas = model.UnitMeas;
//            entity.UnitCost = model.UnitCost;
//            entity.QtyPo = model.QtyPo;
//            entity.Qty = model.Qty;
//            entity.QtyIss = model.QtyIss;
//            entity.QtyBal = model.QtyBal;
//            entity.Days = model.Days;
//            entity.Remarks = model.Remarks;
//            entity.Amount = model.Amount;
//            entity.UpdatedBy = model.UpdatedBy;
//            entity.UpdatedDt = model.UpdatedDt;

//            _db.PsItems.Attach(entity);
//            _db.Entry(entity).State = EntityState.Modified;
//            await _db.SaveChangesAsync();

//            return model;
//        });
        
//    }
//}