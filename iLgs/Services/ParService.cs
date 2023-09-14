using iLgs.Exceptions;
using iLgs.Exceptions.PARs;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public class ParService : IParService
    {
        private readonly AppManEntities db = new AppManEntities();
        
        public ParService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<PAR_VM> GetAll()
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
                    Fund = s.Order.Request.RISs.Fund,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
                })
                .AsQueryable();
            return data;
        }
        public IQueryable<PARAcknowledgementVM> GetAcknowledgedOrderItems(Guid? orderItemId)
        {
            var data = db.PARItems
                .Where(w => w.OrderItemId == orderItemId)
                .Select(s => new PARAcknowledgementVM
                {
                    ParId = s.PAR.Id,
                    ParItemId = s.Id,
                    OrderId = s.OrderItem.OrderId,
                    OrderItemId = s.OrderItemId,
                    ParNo = s.PAR.ParNo,
                    ParDate = s.PAR.ParDate,
                    ReceivedBy = s.PAR.ReceivedBy,
                    ReceivedByPosition = s.PAR.ReceivedByPosition,
                    ReceivedDate = s.PAR.ReceivedDate,
                    IssuedBy = s.PAR.IssuedBy,
                    IssuedByPosition = s.PAR.IssuedByPosition,
                    IssuedDate = s.PAR.IssuedDate,
                    PostedBy = s.PAR.PostedBy,
                    PostedDt = s.PAR.PostedDt,
                    Unit = s.OrderItem.RequestItem.RisItem.Unit,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    DateAcquired = s.OrderItem.Order.PoDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    SerialNo = s.SerialNo
                })
                .AsQueryable();
            return data;
        }

        public async Task<PARAcknowledgementVM> GetAcknowledgedOrderItemByItemId(Guid? parItemId)
        {
            var data = await db.PARItems
                .Where(w => w.Id == parItemId)
                .Select(s => new PARAcknowledgementVM
                {
                    ParId = s.PAR.Id,
                    ParItemId = s.Id,
                    OrderId = s.OrderItem.OrderId,
                    OrderItemId = s.OrderItemId,
                    ParNo = s.PAR.ParNo,
                    ParDate = s.PAR.ParDate,
                    ReceivedBy = s.PAR.ReceivedBy,
                    ReceivedByPosition = s.PAR.ReceivedByPosition,
                    ReceivedDate = s.PAR.ReceivedDate,
                    IssuedBy = s.PAR.IssuedBy,
                    IssuedByPosition = s.PAR.IssuedByPosition,
                    IssuedDate = s.PAR.IssuedDate,
                    PostedBy = s.PAR.PostedBy,
                    PostedDt = s.PAR.PostedDt,
                    Unit = s.OrderItem.RequestItem.RisItem.Unit,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    DateAcquired = s.OrderItem.Order.PoDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    SerialNo = s.SerialNo
                })
                .FirstOrDefaultAsync();
            return data;
        }

        public async Task<PAR> GetByIdAsync(Guid parId)
        {
            return await db.PARs.FindAsync(parId);
        }

        public async Task<PAR> GetByParNoAsync(string parNo)
        {
            return await db.PARs.Where(w => w.ParNo == parNo).FirstOrDefaultAsync();
        }

        public async Task<int?> GetRemainingQty(Guid? orderItemId, Guid? parItemId)
        {
            var orderItem = await db.OrderItems.Where(w => w.Id == orderItemId)
                .Select(s => new { Remaining = s.Qty - s.PARItems.Where(w => w.Id != parItemId).Sum(x => x.Qty) }).FirstOrDefaultAsync();
            if (orderItem == null)
            {
                return null;
            }
            return (int?)orderItem.Remaining;
        }

        public async Task<bool> IsAnyParNoAsync(Guid parId, string parNo)
        {
            return await db.PARs.AnyAsync(a => a.Id != parId && a.ParNo == parNo);
        }
        public async Task<bool> IsPostedAsync(Guid parId)
        {
            var entity = await db.PARs.FindAsync(parId);
            if (entity != null)
            {
                return !string.IsNullOrWhiteSpace(entity.PostedBy);
            }
            return false;
        }
        

        public async Task<PAR_VM> CreateAsync(PAR_VM model, string user, DateTime date)
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
                ReceivedBy = model.ReceivedBy ?? "",
                ReceivedByPosition = model.ReceivedByPosition ?? "",
                ReceivedDate = model.ReceivedDate,
                IssuedBy = model.IssuedBy ?? "",
                IssuedByPosition = model.IssuedByPosition ?? "",
                IssuedDate = model.IssuedDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };
            
            // include items during add, ORDER Items not yet in PAR Items
            var orderItems = await db.OrderItems
                .Where(w => w.OrderId == model.OrderId && !w.PARItems.Any()).ToListAsync();
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

        public async Task<PAR_VM> UpdateAsync(PAR_VM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.PARs.FindAsync(model.Id);

            // if there's a change of item
            if (entity.OrderId != model.OrderId)
            {
                var items = db.PARItems.Where(w => w.ParId == model.Id);
                await items.ForEachAsync(f => {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await db.SaveChangesAsync();

                db.PARItems.RemoveRange(items);
                await db.SaveChangesAsync();

                // include items during add, ORDER Items not yet in PAR Items
                var orderItems = await db.OrderItems
                    .Where(w => w.OrderId == model.OrderId && !w.PARItems.Any()).ToListAsync();
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
            }

            entity.OrderId = model.OrderId;
            entity.ParNo = model.ParNo;
            entity.ParDate = model.ParDate;
            entity.ReceivedBy = model.ReceivedBy ?? "";
            entity.ReceivedByPosition = model.ReceivedByPosition ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.IssuedBy = model.IssuedBy ?? "";
            entity.IssuedByPosition = model.IssuedByPosition ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.PARs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<PAR_VM> DeleteAsync(PAR_VM model, string user, DateTime date)
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.PARs.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.PARs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.PARs.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task PostAsync(Guid orderId, string user, DateTime date)
        {
            var entity = await db.PARs.FindAsync(orderId);
            entity.PostedBy = user;
            entity.PostedDt = date;

            db.PARs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task UnpostAsync(Guid orderId, string user, DateTime date)
        {
            var entity = await db.PARs.FindAsync(orderId);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.PARs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task GeneratePAR(GenerateParVM model, string user, DateTime date)
        {
            var orderItem = await db.OrderItems.FindAsync(model.OrderItemId);
            if (orderItem == null)
            {
                throw new RecordNotFoundException((Guid)model.OrderItemId);
            }

            if (db.PARItems.Any(a => a.OrderItemId == model.OrderItemId))
            {
                throw new ParsAlreadyExistsException();
            }

            // generate par per qty
            for (var qty = 0; qty < orderItem.Qty; ++qty)
            {
                var par = new PAR()
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderItem.OrderId,
                    ParNo = NextParNo((DateTime)model.ParDate),
                    ParDate = model.ParDate,
                    ReceivedBy = "",
                    ReceivedByPosition = "",
                    ReceivedDate = null,
                    IssuedBy = "",
                    IssuedByPosition = "",
                    IssuedDate = null,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date,
                    PostedBy = "",
                    PostedDt = null
                };
                db.PARs.Add(par);
                db.Entry(par).State = EntityState.Added;
                await db.SaveChangesAsync();

                var parItem = new PARItem()
                {
                    Id = Guid.NewGuid(),
                    ParId = par.Id,
                    OrderItemId = model.OrderItemId,
                    Qty = 1,
                    Amount = orderItem.UnitCost,
                    SerialNo = "",
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                db.PARItems.Add(parItem);
                db.Entry(parItem).State = EntityState.Added;
                await db.SaveChangesAsync();
            }

            //var entity = await db.PARs.FindAsync(orderId);
            //entity.PostedBy = user;
            //entity.PostedDt = date;

            //db.PARs.Attach(entity);
            //db.Entry(entity).State = EntityState.Modified;
            //await db.SaveChangesAsync();
        }

        public async Task<PARAcknowledgementVM> CreateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date)
        {
            //model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.ParNo))
            {
                model.ParNo = NextParNo((DateTime)model.ParDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PAR()
            {
                Id = model.ParId,
                OrderId = model.OrderId,
                ParNo = model.ParNo,
                ParDate = model.ParDate,
                ReceivedBy = model.ReceivedBy ?? "",
                ReceivedByPosition = model.ReceivedByPosition ?? "",
                ReceivedDate = model.ReceivedDate,
                IssuedBy = model.IssuedBy ?? "",
                IssuedByPosition = model.IssuedByPosition ?? "",
                IssuedDate = model.IssuedDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            var parItem = new PARItem()
            {
                Id = Guid.NewGuid(),
                ParId = entity.Id,
                OrderItemId = model.OrderItemId,
                Qty = model.Qty,
                Amount = model.Amount,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            entity.PARItems.Add(parItem);

            db.PARs.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }
        public async Task<PARAcknowledgementVM> UpdateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.PARs.Where(w => w.Id == model.ParId).FirstOrDefaultAsync();
            
            entity.ParNo = model.ParNo;
            entity.ParDate = model.ParDate;
            entity.ReceivedBy = model.ReceivedBy ?? "";
            entity.ReceivedByPosition = model.ReceivedByPosition ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.IssuedBy = model.IssuedBy ?? "";
            entity.IssuedByPosition = model.IssuedByPosition ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            var parItem = await db.PARItems.FirstOrDefaultAsync(f => f.Id == model.ParItemId);
            if (parItem != null)
            {
                parItem.Qty = model.Qty;
                parItem.Amount = model.Amount;
                parItem.SerialNo = model.SerialNo ?? "";
                parItem.UpdatedBy = model.UpdatedBy;
                parItem.UpdatedDt = model.UpdatedDt;

                db.PARItems.Attach(parItem);
                db.Entry(parItem).State = EntityState.Modified;
            }

            db.PARs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }
        public async Task<PARAcknowledgementVM> DeleteAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            PARItem entity = await db.PARItems.FindAsync(model.ParItemId);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.PARItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.PARItems.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            var par = await db.PARs.Where(w => w.Id == model.ParId && !w.PARItems.Any()).FirstOrDefaultAsync();
            if (par != null)
            {
                par.UpdatedBy = model.UpdatedBy;
                par.UpdatedDt = model.UpdatedDt;

                db.PARs.Attach(par);
                db.Entry(par).State = EntityState.Modified;
                await db.SaveChangesAsync();

                db.PARs.Remove(par);
                db.Entry(par).State = EntityState.Deleted;
                await db.SaveChangesAsync();
            }

            return model;
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