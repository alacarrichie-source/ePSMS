using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public class ItemTypeService : IItemTypeService
    {
        private readonly AppManEntities db = new AppManEntities();

        public ItemTypeService(AppManEntities db)
        {
            this.db = db;
        }

        public IQueryable<ItemTypeVM> GetAll()
        {
            var data = db.ItemTypes
                .Select(s => new ItemTypeVM
                {
                    Id = s.Id,                   
                    Code = s.Code,
                    Description = s.Description,
                    FormulaNo = s.FormulaNo,
                    Category = s.Category,
                    CategoryDesc = db.Codextns.Where(w => w.Code == s.Category && w.CodeMast.Code == "PS-CATEGORY").FirstOrDefault().Description,
                    GroupCode = s.GroupCode,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }
        
        public async Task<ItemType> GetByIdAsync(Guid id)
        {
            var data = await db.ItemTypes.FindAsync(id);
            return data;
        }

        public async Task<ItemTypeVM> CreateAsync(ItemTypeVM model, string user, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                throw new InvalidValueException("Code is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            if (model.FormulaNo == null || model.FormulaNo == 0)
            {
                throw new InvalidValueException("Formula Field No. is Required!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            ItemType entity = new ItemType()
            {
                Id = model.Id,
                Code = model.Code,
                Description = model.Description,
                FormulaNo = model.FormulaNo,
                Category = model.Category,
                GroupCode = model.GroupCode,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            db.ItemTypes.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemTypeVM> UpdateAsync(ItemTypeVM model, string user, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                throw new InvalidValueException("Code is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            if (model.FormulaNo == null || model.FormulaNo == 0)
            {
                throw new InvalidValueException("Formula Field No. is Required!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemType entity = await db.ItemTypes.FindAsync(model.Id);

            entity.Code = model.Code;
            entity.Description = model.Description;
            entity.FormulaNo = model.FormulaNo;
            entity.Category = model.Category;
            entity.GroupCode = model.GroupCode;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.ItemTypes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemTypeVM> DeleteAsync(ItemTypeVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemType entity = await db.ItemTypes.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.ItemTypes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.ItemTypes.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

    }
}