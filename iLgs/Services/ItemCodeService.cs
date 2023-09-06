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
    public class ItemCodeService : IItemCodeService
    {
        private readonly AppManEntities db = new AppManEntities();

        public ItemCodeService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<ItemCodeVM> GetAll()
        {
            var data = db.ItemCodes
                .Select(s => new ItemCodeVM
                {
                    Id = s.Id,                    
                    ItemTypeId = s.ItemTypeId,
                    Code = s.Code,
                    Description = s.Description,                    
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public IQueryable<ItemCodeVM> GetAllByItemTypeId(Guid? itemTypeId)
        {            
            var data = db.ItemCodes.Where(w => w.ItemTypeId == itemTypeId)
                .Select(s => new ItemCodeVM
                {
                    Id = s.Id,
                    ItemTypeId = s.ItemTypeId,
                    Code = s.Code,
                    Description = s.Description,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }            

        public async Task<ItemCode> GetByIdAsync(Guid id)
        {
            var data = await db.ItemCodes.FindAsync(id);                
            return data;
        }        

        public async Task<ItemCodeVM> CreateAsync(ItemCodeVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            ItemCode entity = new ItemCode()
            {
                Id = model.Id,
                ItemTypeId = model.ItemTypeId,
                Code = model.Code,
                Description = model.Description,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            db.ItemCodes.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }
        
        public async Task<ItemCodeVM> UpdateAsync(ItemCodeVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemCode entity = await db.ItemCodes.FindAsync(model.Id);

            entity.ItemTypeId = model.ItemTypeId;
            entity.Code = model.Code;
            entity.Description = model.Description;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.ItemCodes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemCodeVM> DeleteAsync(ItemCodeVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemCode entity = await db.ItemCodes.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.ItemCodes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.ItemCodes.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

    }
}