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
    public interface IStockItemIssuanceService
    {
        IQueryable<StockItemIssuanceVM> GetByStockItemId(Guid? stockItemId);
        ValueTask<StockItemIssuanceVM> GetByIdAsync(Guid? id);

        ValueTask<StockItemIssuanceVM> CreateAsync(StockItemIssuanceVM model, string user, DateTime date);
        ValueTask<StockItemIssuanceVM> UpdateAsync(StockItemIssuanceVM model, string user, DateTime date);
        ValueTask<StockItemIssuanceVM> DeleteAsync(StockItemIssuanceVM model, string user, DateTime date);
    }

    public class StockItemIssuanceService : IStockItemIssuanceService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<StockItemIssuanceVM> _VmExceptionService = new ExceptionService<StockItemIssuanceVM>();

        public StockItemIssuanceService(AppManEntities db)
        {
            _db = db;
        }

        public ValueTask<StockItemIssuanceVM> GetByIdAsync(Guid? id) => _VmExceptionService.TryCatch(async () =>
        {
            var data = await _db.StockItemIssuances.Where(w => w.Id == id)
                .Select(s => new StockItemIssuanceVM
                {
                    Id = s.Id,
                    StockItemId = s.StockItemId,
                    RisIssuedId = s.RisIssuedId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedBy = s.IssuedBy,
                    IssuedDate = s.IssuedDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<StockItemIssuanceVM> GetByStockItemId(Guid? stockItemId) => _VmExceptionService.TryCatch(() =>
        {
            var data = _db.StockItemIssuances.Where(w => w.StockItemId == stockItemId)
                .Select(s => new StockItemIssuanceVM
                {
                    Id = s.Id,
                    StockItemId = s.StockItemId,
                    RisIssuedId = s.RisIssuedId,
                    Location = s.Location,
                    Officer = s.Officer,
                    IssuedTo = s.IssuedTo,
                    IssuedBy = s.IssuedBy,
                    IssuedDate = s.IssuedDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });


        public ValueTask<StockItemIssuanceVM> CreateAsync(StockItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var stockItem = await _db.StockItems.FindAsync(model.StockItemId);

            var entity = new StockItemIssuance
            {
                Id = model.Id,
                StockItemId = model.StockItemId,
                RisIssuedId = model.RisIssuedId,
                Location = model.Location,
                Officer = model.Officer,
                IssuedTo = model.IssuedTo,
                IssuedBy = model.IssuedBy,
                IssuedDate = model.IssuedDate,
                Qty = model.Qty,
                Amount = stockItem.UnitCost * model.Qty,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.StockItemIssuances.Add(entity);
            await _db.SaveChangesAsync();

            await UpdateStockItems(model.StockItemId, user, date);

            return model;
        });

        public ValueTask<StockItemIssuanceVM> DeleteAsync(StockItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.StockItemIssuances.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.StockItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.StockItemIssuances.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            await UpdateStockItems(model.StockItemId, user, date);

            return model;
        });

        public ValueTask<StockItemIssuanceVM> UpdateAsync(StockItemIssuanceVM model, string user, DateTime date) => _VmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.StockItemIssuances.Include(i => i.StockItem).Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            entity.StockItemId = model.StockItemId;
            entity.RisIssuedId = model.RisIssuedId;
            entity.Location = model.Location;
            entity.Officer = model.Officer;
            entity.IssuedTo = model.IssuedTo;
            entity.IssuedBy = model.IssuedBy;
            entity.IssuedDate = model.IssuedDate;
            entity.Qty = model.Qty;
            entity.Amount = entity.StockItem.UnitCost * model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.StockItemIssuances.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await UpdateStockItems(model.StockItemId, user, date);

            return model;
        });

        public async Task UpdateStockItems(Guid? stockItemId, string user, DateTime date)
        {
            var entity = await _db.StockItems.Include(i => i.StockItemIssuances).Where(w => w.Id == stockItemId).FirstOrDefaultAsync();
            var qtyIss = entity.StockItemIssuances.Sum(s => s.Qty);

            entity.QtyIss = qtyIss;
            entity.QtyBal = entity.Qty - qtyIss;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.StockItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

    }
}