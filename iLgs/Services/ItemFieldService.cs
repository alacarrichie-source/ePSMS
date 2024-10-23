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
        private readonly AppManEntities _db;

        public ItemFieldService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<ItemFieldVM> GetAll()
        {
            var data = _db.ItemFields
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
            var data = _db.ItemFields.Where(w => w.ItemTypeId == itemTypeId)
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
            var data = await _db.ItemFields.FindAsync(id);
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

            _db.ItemFields.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemFieldVM> UpdateAsync(ItemFieldVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemField entity = await _db.ItemFields.FindAsync(model.Id);

            entity.ItemTypeId = model.ItemTypeId;
            entity.FieldNo = model.FieldNo;
            entity.FieldName = model.FieldName;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.ItemFields.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemFieldVM> DeleteAsync(ItemFieldVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemField entity = await _db.ItemFields.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.ItemFields.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.ItemFields.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        }

    }
}