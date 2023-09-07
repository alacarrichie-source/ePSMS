using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public class ItemFieldService : IItemFieldService
    {
        private readonly AppManEntities db = new AppManEntities();

        public ItemFieldService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<ItemFieldVM> GetAll()
        {
            var data = db.ItemFields
                .Select(s => new ItemFieldVM
                {
                    Id = s.Id,
                    ItemTypeId = s.ItemTypeId,
                    FieldNo = s.FieldNo,
                    FieldName = s.FieldName,                    
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public IQueryable<ItemFieldVM> GetAllbyItemTypeId(Guid? itemTypeId)
        {
            var data = db.ItemFields.Where(w => w.ItemTypeId == itemTypeId)
                .Select(s => new ItemFieldVM
                {
                    Id = s.Id,
                    ItemTypeId = s.ItemTypeId,
                    FieldNo = s.FieldNo,
                    FieldName = s.FieldName,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }

        public async Task<ItemField> GetByIdAsync(Guid id)
        {
            var data = await db.ItemFields.FindAsync(id);
            return data;
        }

        public async Task<ItemFieldVM> CreateAsync(ItemFieldVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.Id = Guid.NewGuid();

            ItemField entity = new ItemField()
            {
                Id = model.Id,
                ItemTypeId = model.ItemTypeId,
                FieldNo = model.FieldNo,
                FieldName = model.FieldName,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            db.ItemFields.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemFieldVM> UpdateAsync(ItemFieldVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemField entity = await db.ItemFields.FindAsync(model.Id);

            entity.ItemTypeId = model.ItemTypeId;
            entity.FieldNo = model.FieldNo;
            entity.FieldName = model.FieldName;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.ItemFields.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemFieldVM> DeleteAsync(ItemFieldVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemField entity = await db.ItemFields.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.ItemFields.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.ItemFields.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

    }
}