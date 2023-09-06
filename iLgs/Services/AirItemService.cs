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
    public class AirItemService : IAirItemService
    {
        private readonly AppManEntities db = new AppManEntities();

        public AirItemService(AppManEntities db)
        {
            this.db = db;
        }

        public async Task<AIRItemVM> CreateAsync(AIRItemVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            AIRItem entity = new AIRItem()
            {
                Id = model.Id,
                AirId = model.AirId,
                OrderItemId = model.OrderItemId,
                Qty = model.Qty,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            db.AIRItems.Add(entity);
            await db.SaveChangesAsync();
            
            return model;
        }

        public async Task<AIRItemVM> DeleteAsync(AIRItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRItem entity = await db.AIRItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.AIRItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.AIRItems.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<AIRItemVM> GetByIdAsync(Guid? id)
        {
            var data = await db.AIRItems.Where(w => w.Id == id)
                .Select(s => new AIRItemVM
                {
                    Id = s.Id,
                    OrderItemId = s.OrderItemId,
                    PsType = s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    PsItem = s.OrderItem.RequestItem.RisItem.ItemName,
                    OrderDescription = s.OrderItem.RequestItem.Description,
                    PsUnit = s.OrderItem.RequestItem.RisItem.Unit,
                    Qty = s.Qty,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<AIRItemVM> GetByAirId(Guid? airId)
        {
            var data = db.AIRItems.Where(w => w.AirId == airId)
                .Select(s => new AIRItemVM
                {
                    Id = s.Id,
                    OrderItemId = s.OrderItemId,
                    PsType = s.OrderItem.RequestItem.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    PsItem = s.OrderItem.RequestItem.RisItem.ItemName,
                    OrderDescription = s.OrderItem.RequestItem.Description,
                    PsUnit = s.OrderItem.RequestItem.RisItem.Unit,
                    Qty = s.Qty,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public async Task<AIRItemVM> UpdateAsync(AIRItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            AIRItem entity = await db.AIRItems.FindAsync(model.Id);

            entity.AirId = model.AirId;
            entity.OrderItemId = model.OrderItemId;
            entity.Qty = model.Qty;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.AIRItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }
    }
}