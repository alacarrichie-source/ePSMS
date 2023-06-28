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
    public class AirService : IAirService
    {
        private readonly AppManEntities db = new AppManEntities();

        public AirService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<AIR_VM> GetAll()
        {
            var data = db.AIRs
                .Select(s => new AIR_VM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    PoNo = s.Order.PoNo,
                    Supplier = s.Order.Supplier.BusinessName,
                    PoDate = s.Order.PoDate,
                    Department = s.Order.DeliveryPlace,
                    Fund = s.Fund,
                    AIRNo = s.AIRNo,
                    AIRDate = s.AIRDate,
                    InvoiceNo = s.InvoiceNo,
                    InvoiceDate = s.InvoiceDate,
                    AcceptedDate = s.AcceptedDate,
                    IsComplete = s.IsComplete,
                    IsPartial = s.IsPartial,
                    Custodian = s.Custodian,
                    InspectedDate = s.InspectedDate,
                    IsInspected = s.IsInspected,
                    Officer = s.Officer,
                    Remarks = s.Remarks
                });                
            return data;
        }

        public async Task<bool> GetAnyAirNoAsync(Guid airId, string airNo)
        {
            return await db.AIRs.AnyAsync(a => a.Id != airId && a.AIRNo == airNo);
        }

        public async Task<AIR> GetByIdAsync(Guid id)
        {
            return await db.AIRs.FindAsync(id);
        }

        public async Task<AIR> GetByAirNoAsync(string airNo)
        {
            return await db.AIRs.Where(w => w.AIRNo == airNo).FirstOrDefaultAsync();
        }

        public async Task<bool> IsPostedAsync(Guid airId)
        {
            var entity = await db.AIRs.FindAsync(airId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        //public async Task<bool> IsPrPostedAsync(Guid risId)
        //{
        //    var pr = await db.Requests.Where(a => a.RisId == risId).FirstOrDefaultAsync();
        //    if (pr != null)
        //    {
        //        return !string.IsNullOrWhiteSpace(pr.SubmittedBy);
        //    }
        //    return false;
        //}

        public async Task PostAsync(Guid airId, string user, DateTime date)
        {
            var entity = await db.AIRs.FindAsync(airId);
            if (entity != null)
            {
                entity.PostedBy = user;
                entity.PostedDt = date;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.AIRs.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task UnpostAsync(Guid airId, string user, DateTime date)
        {
            var entity = await db.AIRs.FindAsync(airId);
            if (entity != null)
            {
                entity.PostedBy = null;
                entity.PostedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                db.AIRs.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task<AIR_VM> CreateAsync(AIR_VM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.AIRNo))
            {
                model.AIRNo = NextAirNo((DateTime)model.AIRDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new AIR()
            {
                Id = model.Id,
                Fund = model.Fund,
                AIRNo = model.AIRNo,
                AIRDate = model.AIRDate,
                OrderId = model.OrderId,
                InvoiceNo = model.InvoiceNo ?? "",
                InvoiceDate = model.InvoiceDate,
                AcceptedDate = model.AcceptedDate,
                IsComplete = model.IsComplete,
                IsPartial = model.IsPartial,
                Custodian = model.Custodian ?? "",
                InspectedDate = model.InspectedDate,
                IsInspected = model.IsInspected,
                Officer = model.Officer ?? "",
                Remarks = model.Remarks ?? "",
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            // include items during add
            var orderItems = db.OrderItems.Where(w => w.OrderId == model.OrderId).ToList();
            foreach (var orderItem in orderItems)
            {

                AIRItem airItem = new AIRItem()
                {
                    Id = Guid.NewGuid(),
                    AirId = entity.Id,
                    OrderItemId = orderItem.Id,
                    Qty = orderItem.Qty,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                entity.AIRItems.Add(airItem);
            }

            db.AIRs.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<AIR_VM> DeleteAsync(AIR_VM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.AIRs.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.AIRs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.AIRs.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<AIR_VM> UpdateAsync(AIR_VM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.AIRs.FindAsync(model.Id);

            // if there's a change of Order item
            if (entity.OrderId != model.OrderId)
            {
                var airItems = db.AIRItems.Where(w => w.AirId == model.Id);
                await airItems.ForEachAsync(f => {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await db.SaveChangesAsync();

                db.AIRItems.RemoveRange(airItems);
                await db.SaveChangesAsync();

                // include items during add
                var orderItems = db.OrderItems.Where(w => w.OrderId == model.OrderId).ToList();
                foreach (var orderItem in orderItems)
                {
                    AIRItem airItem = new AIRItem()
                    {
                        Id = Guid.NewGuid(),
                        AirId = entity.Id,
                        OrderItemId = orderItem.Id,
                        Qty = orderItem.Qty,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    entity.AIRItems.Add(airItem);
                }
            }

            entity.Fund = model.Fund;
            entity.AIRNo = model.AIRNo;
            entity.AIRDate = model.AIRDate;
            entity.OrderId = model.OrderId;
            entity.InvoiceNo = model.InvoiceNo ?? "";
            entity.InvoiceDate = model.InvoiceDate;
            entity.AcceptedDate = model.AcceptedDate;
            entity.IsComplete = model.IsComplete;
            entity.IsPartial = model.IsPartial;
            entity.Custodian = model.Custodian ?? "";
            entity.InspectedDate = model.InspectedDate;
            entity.IsInspected = model.IsInspected;
            entity.Officer = model.Officer ?? "";
            entity.Remarks = model.Remarks ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.AIRs.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        private string NextAirNo(DateTime date)
        {
            string yyyy = date.Year.ToString().Trim();
            string mm = date.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = db.AIRs.Where(w => w.AIRDate.Value.Year == date.Year).OrderByDescending(o => o.AIRNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.AIRNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
    }
}