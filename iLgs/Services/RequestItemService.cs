using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iLgs.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace iLgs.Services
{
    public class RequestItemService : IRequestItemService
    {
        private readonly AppManEntities db = new AppManEntities();

        public RequestItemService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<RequestItemVM> GetByPrId(Guid? prId)
        {
            var data = db.RequestItems.Where(w => w.PrId == prId)
                .Select(s => new RequestItemVM
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    RisItemId = s.RisItemId,
                    ItemCode = s.RisItem.ItemCode.Code,
                    ItemType = s.RisItem.ItemCode.Description,
                    PsType = s.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.RisItem.PsNo,
                    PsNoDisplay = s.RisItem.PsNoDisplay,
                    Unit = s.RisItem.Unit,
                    ItemName = s.RisItem.ItemName,
                    Description = s.RisItem.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public async Task<RequestItemVM> GetVmByIdAsync(Guid? id)
        {
            var data = await db.RequestItems.Where(w => w.Id == id)
                .Select(s => new RequestItemVM
                {
                    Id = s.Id,
                    PrId = s.PrId,
                    RisItemId = s.RisItemId,
                    ItemCode = s.RisItem.ItemCode.Code,
                    ItemType = s.RisItem.ItemCode.Description,
                    PsType = s.RisItem.ItemCode.ItemType.Code,
                    PsNo = s.RisItem.PsNo,
                    PsNoDisplay = s.RisItem.PsNoDisplay,
                    Unit = s.RisItem.Unit,
                    ItemName = s.RisItem.ItemName,
                    Description = s.RisItem.Description,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    InsertedDt = s.InsertedDt,
                    GridRequestItemExtns = ""
                }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<RequestItem> GetByIdAsync(Guid? id)
        {
            var data = await db.RequestItems.FindAsync(id);                
            return data;
        }        

        public async Task<RequestItemVM> CreateAsync(RequestItemVM model, string user, DateTime date)
        {
            //model.Id = Guid.NewGuid(); // Id created in controller
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RequestItem entity = new RequestItem()
            {
                Id = model.Id,
                PrId = model.PrId,
                Qty = model.Qty,
                UnitCost = model.UnitCost,
                TotalCost = model.TotalCost,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            db.RequestItems.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestItemVM> DeleteAsync(RequestItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RequestItem entity = await db.RequestItems.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RequestItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.RequestItems.Remove(entity);
            await db.SaveChangesAsync();

            return model;
        }        

        public async Task<RequestItemVM> UpdateAsync(RequestItemVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RequestItem entity = await db.RequestItems.FindAsync(model.Id);
            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.TotalCost = model.TotalCost;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.RequestItems.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }
    }
}