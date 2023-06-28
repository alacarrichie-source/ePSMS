using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System.Data.Entity;

namespace iLgs.Services
{
    public class OrderItemService : IOrderItemService
    {
        private readonly AppManEntities db = new AppManEntities();

        public OrderItemService(AppManEntities db)
        {
            this.db = db;
        }

        public async Task<OrderItemVM> GetByIdAsync(Guid? id)
        {
            var data = await db.OrderItems.Where(w => w.Id == id)
                .Select(s => new OrderItemVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    RequestItemId = s.RequestItemId,
                    PsCodeId = s.RequestItem.PsCodeId,
                    PsNo = s.RequestItem.RisItem.PsCode.PsNo,
                    PsUnit = s.RequestItem.RisItem.PsCode.UnitMeas,
                    PsItem = s.RequestItem.RisItem.PsCode.ItemName,
                    Description = s.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<OrderItemVM> GetByPoId(Guid? poId)
        {
            var data = db.OrderItems.Where(w => w.OrderId == poId)
                .Select(s => new OrderItemVM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    RequestItemId = s.RequestItemId,
                    PsNo = s.RequestItem.RisItem.PsCode.PsNo,
                    PsUnit = s.RequestItem.RisItem.PsCode.UnitMeas,
                    PsItem = s.RequestItem.RisItem.PsCode.ItemName,
                    Description = s.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public async Task<OrderItemVM> CreateAsync(OrderItemVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            OrderItem entity = new OrderItem()
            {
                Id = model.Id,
                OrderId = model.OrderId,
                RequestItemId = model.RequestItemId,
                Description = model.Description,
                Qty = model.Qty,
                UnitCost = model.UnitCost,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.OrderItems.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<OrderItemVM> DeleteAsync(OrderItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            OrderItem entity = await db.OrderItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.OrderItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.OrderItems.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }        

        public async Task<OrderItemVM> UpdateAsync(OrderItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            OrderItem entity = await db.OrderItems.FindAsync(model.Id);

            entity.RequestItemId = model.RequestItemId;
            entity.Description = model.Description;
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.Amount = model.Amount;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.OrderItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }
    }
}