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
    public class RisIssuedService : IRisIssuedService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisIssuedVM> _vmExceptionService = new ExceptionService<RisIssuedVM>();
        private readonly IExceptionService<RisIssued> _exceptionService = new ExceptionService<RisIssued>();
        
        public RisIssuedService(AppManEntities db)
        {
            _db = db;        
        }

        public ValueTask<RisIssuedVM> GetVmByIdAsync(Guid? id) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisIssueds.Where(w => w.Id == id)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt                    
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<RisIssued> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisIssueds.FindAsync(id);
            return data;
        });

        public IQueryable<RisIssuedVM> GetByRisItemId(Guid? risItemId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItemId == risItemId)
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });

        public IQueryable<RisIssuedVM> GetByPoNoStockNo(string poNo, string stockNo) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.Order.PoNo == poNo && o.StockNo.Contains(stockNo))))
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });

        public IQueryable<RisIssuedVM> GetByStockNo(string stockNo) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RisIssueds.Where(w => w.RisItem.RequestItems.Any(a => a.OrderItems.Any(o => o.StockNo == stockNo)))
                .Select(s => new RisIssuedVM
                {
                    Id = s.Id,
                    RisItemId = s.RisItemId,
                    OrderItemId = s.OrderItemId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedDate = s.IssuedDate,
                    IssuedBy = s.IssuedBy,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                    //Department = s.RisItem.RISs.Office
                });
            return data;
        });

        public ValueTask<RisIssuedVM> CreateAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var totalQtyIssued = _db.RisItems.Find(model.RisItemId)?.QtyRequest ?? 0;
            var qtyIssued = _db.RisIssueds.Where(w => w.RisItemId == model.RisItemId).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            var rsmiDate = _db.RSMIs.Max(m => m.Date);
            if (rsmiDate != null && rsmiDate > model.IssuedDate)
            {
                throw new InvalidValueException(string.Format("Date issued must be after the last RSMI date on {0}", rsmiDate.Value.ToShortDateString()));
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            RisIssued entity = new RisIssued()
            {
                Id = model.Id,
                RisItemId = model.RisItemId,
                OrderItemId = model.OrderItemId,
                IssuedDate = model.IssuedDate,
                IssuedBy = model.IssuedBy,
                Qty = model.Qty,
                Amount = model.Amount,
                IssuedTo = model.IssuedTo,
                Officer = model.Officer,
                Location = model.Location,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RisIssueds.Add(entity);
            await _db.SaveChangesAsync();
            await UpdateRisStockItems(model.RisItemId);
            await UpdateStockItemIssuance(entity, user, date);
            return model;
        });

        public ValueTask<RisIssuedVM> DeleteAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {            
            if (_db.RSMIs.Any(a => a.Date == model.IssuedDate))
            {
                throw new RecordRelationshipException("Date Issued is already in RSMI, Cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisIssued entity = await _db.RisIssueds.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisIssueds.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();


            await DeleteStockItemIssuance(entity, user, date);

            _db.RisIssueds.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            await UpdateRisStockItems(model.RisItemId);
            
            return model;
        });

        public ValueTask<RisIssuedVM> UpdateAsync(RisIssuedVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatchAsync(async () =>
        {
            var totalQtyIssued = _db.RisItems.Find(model.RisItemId)?.QtyRequest ?? 0;
            var qtyIssued = _db.RisIssueds.Where(w => w.RisItemId == model.RisItemId && w.Id != model.Id).Sum(s => s.Qty) ?? 0;
            var qtyBalance = totalQtyIssued - qtyIssued;
            if (model.Qty > qtyBalance)
            {
                throw new InvalidValueException(string.Format("Quantity must not exceed the remaing balance of {0}", qtyBalance));
            }

            var rsmiDate = _db.RSMIs.Max(m => m.Date);
            if (rsmiDate != null && rsmiDate >= model.IssuedDate)
            {
                throw new InvalidValueException(string.Format("Date issued must be after the last RSMI date on {0}", rsmiDate.Value.ToShortDateString()));
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RisIssued entity = await _db.RisIssueds.FindAsync(model.Id);

            entity.RisItemId = model.RisItemId;
            entity.OrderItemId = model.OrderItemId;
            entity.IssuedDate = model.IssuedDate;
            entity.IssuedBy = model.IssuedBy;
            entity.Qty = model.Qty;
            entity.Amount = model.Amount;
            entity.IssuedTo = model.IssuedTo;
            entity.Officer = model.Officer;
            entity.Location = model.Location;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisIssueds.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();                        
            await UpdateRisStockItems(model.RisItemId);
            await UpdateStockItemIssuance(entity, user, date);
            return model;
        });

        private async ValueTask UpdateRisStockItems(Guid? risItemId)
        {
            var qtyReceived = await _db.AIRItems.Where(w => w.OrderItem.RequestItem.RisItem.Id == risItemId).SumAsync(s => s.Qty) ?? 0;
            var qtyIssued = await _db.RisIssueds.Where(w => w.RisItemId == risItemId).SumAsync(s => s.Qty) ?? 0;
            var orderItems = await _db.OrderItems.Where(w => w.RequestItem.RisItem.Id == risItemId).ToListAsync();
            foreach (var orderItem in orderItems)
            {
                var stockItems = _db.StockItems.Where(w => w.OrderItemId == orderItem.Id);
                await stockItems.ForEachAsync(f => { f.QtyIss = (int?)qtyIssued; f.Qty = (int?)qtyReceived; f.QtyBal = (int)qtyReceived - qtyIssued; });
            }
            var risItem = await _db.RisItems.FindAsync(risItemId);
            risItem.QtyIssue = qtyIssued;
            _db.RisItems.Attach(risItem);
            _db.Entry(risItem).State = EntityState.Modified;

            await _db.SaveChangesAsync();            
        }

        private async ValueTask UpdateStockItemIssuance(RisIssued risIssued, string user, DateTime? date)
        {
            var orderItem = await _db.OrderItems.FindAsync(risIssued.OrderItemId);
            var stockItemIssuance = await _db.StockItemIssuances.FirstOrDefaultAsync(f => f.RisIssuedId == risIssued.Id);
            if (stockItemIssuance == null)
            {
                var stockItem = await _db.StockItems.Where(w => w.OrderItemId == risIssued.OrderItemId).FirstOrDefaultAsync();
                stockItemIssuance = new StockItemIssuance()
                {
                    Id = Guid.NewGuid(),
                    StockItemId = stockItem.Id,
                    RisIssuedId = risIssued.Id,
                    Location = risIssued.Location,
                    IssuedTo = risIssued.IssuedTo,
                    Officer = risIssued.Officer,
                    IssuedDate = risIssued.IssuedDate,
                    IssuedBy = risIssued.IssuedBy,                    
                    Qty = risIssued.Qty,
                    Amount = risIssued.Qty * orderItem.UnitCost,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                _db.StockItemIssuances.Add(stockItemIssuance);
                _db.Entry(stockItemIssuance).State = EntityState.Added;
            }
            else
            {

                stockItemIssuance.IssuedDate = risIssued.IssuedDate;
                stockItemIssuance.IssuedTo = risIssued.IssuedTo;
                stockItemIssuance.Qty = risIssued.Qty;
                stockItemIssuance.Amount = risIssued.Amount;
                stockItemIssuance.Location = risIssued.Location;
                stockItemIssuance.Officer = risIssued.Officer;
                stockItemIssuance.UpdatedBy = user;
                stockItemIssuance.UpdatedDt = date;
                _db.StockItemIssuances.Attach(stockItemIssuance);
                _db.Entry(stockItemIssuance).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
        }

        private async ValueTask DeleteStockItemIssuance(RisIssued model, string user, DateTime? date)
        {
            var entity = await _db.StockItemIssuances.FirstOrDefaultAsync(f => f.RisIssuedId == model.Id);
            if (entity == null)
            {
                return;
            }
            
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.StockItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.StockItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();            
        }
    }
}