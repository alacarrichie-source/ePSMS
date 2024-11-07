using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.ParIcs
{
    public interface IParItemService
    {
        IQueryable<PARItemVM> GetAll(Guid? parId);
        Task<PARItemVM> GetVmByIdAsync(Guid? itemId);
        Task<Models.PARItem> GetByIdAsync(Guid? itemId);

        Task<PARItemVM> CreateAsync(PARItemVM model, string user, DateTime date);
        Task<PARItemVM> UpdateAsync(PARItemVM model, string user, DateTime date);
        Task<PARItemVM> DeleteAsync(PARItemVM model, string user, DateTime date);
    }

    public class ParItemService : IParItemService
    {
        private readonly AppManEntities _db;

        public ParItemService(AppManEntities db)
        {
            _db = db;
        }
        public IQueryable<PARItemVM> GetAll(Guid? parId)
        {
            var data = _db.PARItems.Where(w => w.ParId == parId)
                .Select(s => new PARItemVM
                {
                    Id = s.Id,
                    ParId = s.ParId,
                    OrderItemId = s.OrderItemId,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    Unit = s.OrderItem.RequestItem.RisItem.Unit,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    DateAcquired = s.OrderItem.Order.PoDate,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public async Task<PARItemVM> GetVmByIdAsync(Guid? itemId)
        {
            var data = await _db.PARItems.Where(w => w.Id == itemId)
                .Select(s => new PARItemVM
                {
                    Id = s.Id,
                    ParId = s.ParId,
                    OrderItemId = s.OrderItemId,
                    Qty = s.Qty,
                    Unit = s.OrderItem.RequestItem.RisItem.Unit,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    DateAcquired = s.OrderItem.Order.PoDate,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<Models.PARItem> GetByIdAsync(Guid? itemId)
        {
            return await _db.PARItems.FindAsync(itemId);
        }

        public async Task<PARItemVM> CreateAsync(PARItemVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            PARItem entity = new PARItem()
            {
                Id = model.Id,
                ParId = model.ParId,
                OrderItemId = model.OrderItemId,
                Qty = model.Qty,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PARItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<PARItemVM> UpdateAsync(PARItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PARItems.FindAsync(model.Id);

            entity.ParId = model.ParId;
            entity.OrderItemId = model.OrderItemId;
            entity.Qty = model.Qty;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PARItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<PARItemVM> DeleteAsync(PARItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PARItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PARItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PARItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        }        
    }
}