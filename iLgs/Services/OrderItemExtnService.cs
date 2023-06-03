using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public class OrderItemExtnService : IOrderItemExtnService
    {
        private readonly AppManEntities db = new AppManEntities();

        public OrderItemExtnService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<OrderItemExtnVM> GetAll()
        {
            var data = db.OrderItemExtns
                .Select(s => new OrderItemExtnVM
                {
                    Id = s.Id,
                    OrderItemId = s.OrderItemId,
                    ItemKey = s.ItemKey,
                    ItemValue = s.ItemValue
                }).AsQueryable();
            return data;
        }

        public IQueryable<OrderItemExtnVM> GetBatchInfo(Guid? orderItemId, Guid? psCodeId)
        {
            var data = db.Database.SqlQuery<OrderItemExtnVM>("Exec OrderItemExtnService_GetBatchInfo {0}, {1}", orderItemId, psCodeId).AsQueryable();
            return data;
        }
        
        public async Task SaveAsync(Guid orderItemId, List<OrderItemExtnVM> orderItemExtnList, string user, DateTime date)
        {
            // log updates
            var existingOrderItemExtns = db.OrderItemExtns.Where(w => w.OrderItemId == orderItemId).ToList();
            foreach (var orderItemExtn in existingOrderItemExtns)
            {
                var entity = await db.OrderItemExtns.FindAsync(orderItemExtn.Id);
                if (entity != null)
                {
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;
                    db.OrderItemExtns.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    db.OrderItemExtns.Remove(entity);
                    db.Entry(entity).State = EntityState.Deleted;
                    await db.SaveChangesAsync();
                }
            }

            foreach (var orderItemExtn in orderItemExtnList)
            {
                var entity = await db.OrderItemExtns.Where(w => w.OrderItemId == orderItemId && w.ItemKey == orderItemExtn.ItemKey).FirstOrDefaultAsync();
                if (entity == null)
                {
                    entity = new iLgs.Models.OrderItemExtn()
                    {
                        Id = Guid.NewGuid(),
                        OrderItemId = orderItemExtn.OrderItemId,
                        ItemKey = orderItemExtn.ItemKey,
                        ItemValue = orderItemExtn.ItemValue,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    db.OrderItemExtns.Add(entity);
                }
                else
                {
                    entity.ItemValue = orderItemExtn.ItemValue;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    db.OrderItemExtns.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                }
            }
            await db.SaveChangesAsync();
        }

        public Task UpdateBatchAsync(List<OrderItemExtnVM> orderExtnList, string user, DateTime date)
        {
            throw new NotImplementedException();
        }

        Task IOrderItemExtnService.SaveAsync(Guid orderItemId, List<OrderItemExtnVM> orderItemExtnList, string user, DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}