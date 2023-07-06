using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public class ParService //: IParService
    {
        private readonly AppManEntities db = new AppManEntities();
        
        public ParService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<PAR_VM> GetAllPars()
        {
            var data = db.PARs
                .Select(s => new PAR_VM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    ParNo = s.ParNo,
                    ParDate = s.ParDate,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByPosition = s.ReceivedByPosition,
                    ReceivedDate = s.ReceivedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByPosition = s.IssuedByPosition,
                    IssuedDate = s.IssuedDate,
                    PoNo = s.Order.PoNo,
                    PoDate = s.Order.PoDate,
                    Fund = s.Order.Request.RISs.Fund
                })
                .AsQueryable();
            return data;
        }

        public async Task<PAR> GetParByIdAsync(Guid parId)
        {
            return await db.PARs.FindAsync(parId);
        }

        public async Task<bool> IsParNoAsync(Guid id, string parNo)
        {
            return await db.PARs.AnyAsync(a => a.Id != id && a.ParNo == parNo);
        }

        public async Task<PAR> GetParByParNoAsync(string parNo)
        {
            return await db.PARs.Where(w => w.ParNo == parNo).FirstOrDefaultAsync();
        }

        public async Task<PAR_VM> CreateParAsync(PAR_VM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.ParNo))
            {
                model.PoNo = NextParNo((DateTime)model.ParDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PAR()
            {
                Id = model.Id,
                OrderId = model.OrderId,
                ParNo = model.ParNo,
                ParDate = model.ParDate,
                ReceivedBy = model.ReceivedBy,
                ReceivedByPosition = model.ReceivedByPosition,
                ReceivedDate = model.ReceivedDate,
                IssuedBy = model.IssuedBy,
                IssuedByPosition = model.IssuedByPosition,
                IssuedDate = model.IssuedDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };
            
            // include items during add, ORDER Items not yet in PAR Items
            var orderItems = db.OrderItems
                .Where(w => w.OrderId == model.OrderId && !w.PARItems.Any()).ToList();
            foreach (var orderItem in orderItems)
            {
                var parItem = new PARItem()
                {
                    Id = Guid.NewGuid(),
                    ParId = entity.Id,
                    OrderItemId = orderItem.Id,
                    Qty = (int)orderItem.Qty,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                
                entity.PARItems.Add(parItem);
            }

            db.PARs.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<OrderVM> UpdateAsync(OrderVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.Orders.FindAsync(model.Id);

            // if there's a change of request item
            if (entity.PrId != model.PrId)
            {
                var orderItems = db.OrderItems.Where(w => w.OrderId == model.Id);
                await orderItems.ForEachAsync(f => {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await db.SaveChangesAsync();

                db.OrderItems.RemoveRange(orderItems);
                await db.SaveChangesAsync();

                // include items during add, PR Items not yet in Order Items
                var prItemList = db.RequestItems.Include(i => i.RequestItemExtns)
                    .Where(w => w.PrId == model.PrId && !w.OrderItems.Any()).ToList();
                foreach (var prItem in prItemList)
                {
                    OrderItem orderItem = new OrderItem()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = entity.Id,
                        RequestItemId = prItem.Id,
                        Description = prItem.Description,
                        Qty = prItem.Qty,
                        UnitCost = prItem.UnitCost,
                        Amount = prItem.TotalCost,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    foreach (var prItemExtn in prItem.RequestItemExtns)
                    {
                        OrderItemExtn orderItemExtn = new OrderItemExtn()
                        {
                            Id = Guid.NewGuid(),
                            OrderItemId = orderItem.Id,
                            ItemKey = prItemExtn.ItemKey,
                            ItemValue = prItemExtn.ItemValue,
                            Sequence = prItemExtn.Sequence,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        orderItem.OrderItemExtns.Add(orderItemExtn);
                    }

                    entity.OrderItems.Add(orderItem);
                }
            }

            entity.SupplierId = model.SupplierId;
            entity.PoNo = model.PoNo;
            entity.PoDate = model.PoDate;
            entity.PoMode = model.PoMode;
            entity.PrId = model.PrId;
            entity.DeliveryPlace = model.DeliveryPlace;
            entity.DeliveryDate = model.DeliveryDate;
            entity.TermDelivery = model.TermDelivery;
            entity.TermPayment = model.TermPayment;
            entity.SignedByAuthDesignation = model.SignedByAuthDesignation;
            entity.SignedByAuthName = model.SignedByAuthName;
            entity.SignedBySuppDate = model.SignedBySuppDate;
            entity.SignedBySuppName = model.SignedBySuppName;
            entity.ResoNo = model.ResoNo;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.CertifiedCorrectDate = model.CertifiedCorrectDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<OrderVM> DeleteAsync(OrderVM model, string user, DateTime date)
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.Orders.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.Orders.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task PostAsync(Guid orderId, string user, DateTime date)
        {
            var entity = await db.Orders.FindAsync(orderId);
            entity.PostedBy = user;
            entity.PostedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task UnpostAsync(Guid orderId, string user, DateTime date)
        {
            var entity = await db.Orders.FindAsync(orderId);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.Orders.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        private string NextParNo(DateTime parDate)
        {
            string yyyy = parDate.Year.ToString().Trim();
            string mm = parDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.PARs.Where(w => w.ParDate.Value.Year == parDate.Year).OrderByDescending(o => o.ParNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.ParNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
    }
}