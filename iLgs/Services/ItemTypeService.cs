using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public interface IItemTypeService
    {
        IQueryable<ItemTypeVM> GetAll();
        Task<ItemType> GetByIdAsync(Guid id);
        IQueryable<ItemType> GetRpciAccounts(string text);

        Task<ItemTypeVM> CreateAsync(ItemTypeVM model, string user, DateTime date);
        Task<ItemTypeVM> UpdateAsync(ItemTypeVM model, string user, DateTime date);
        Task<ItemTypeVM> DeleteAsync(ItemTypeVM model, string user, DateTime date);
    }

    public class ItemTypeService : IItemTypeService
    {
        private readonly AppManEntities _db = new AppManEntities();

        public ItemTypeService(AppManEntities db)
        {
            this._db = db;
        }

        public IQueryable<ItemTypeVM> GetAll()
        {
            var data = _db.ItemTypes
                .Select(s => new ItemTypeVM
                {
                    Id = s.Id,                   
                    Code = s.Code,
                    Description = s.Description,
                    FormulaNo = s.FormulaNo,
                    Category = s.Category,
                    CategoryDesc = _db.Codextns.Where(w => w.Code == s.Category && w.CodeMast.Code == "PS-CATEGORY").FirstOrDefault().Description,
                    GroupCode = s.GroupCode,
                    InsertedDt = s.InsertedDt
                });
            return data;
        }
        
        public async Task<ItemType> GetByIdAsync(Guid id)
        {
            var data = await _db.ItemTypes.FindAsync(id);
            return data;
        }

        public IQueryable<ItemType> GetRpciAccounts(string text) 
        {
            var exclude = new[] { "B", "L", "S" }; // Building // Land // Land Improvements
            var data = _db.ItemTypes.Where(w => !exclude.Contains(w.Code)).AsQueryable();
            if (!string.IsNullOrWhiteSpace(text))
            {
                data = data.Where(w => w.Description.Contains(text));
            }
            return data;
        }
        public async Task<ItemTypeVM> CreateAsync(ItemTypeVM model, string user, DateTime date)
        {

            ValidateRequired(model);

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

            _db.ItemTypes.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        private void ValidateRequired(ItemTypeVM model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                throw new InvalidValueException("Code is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                throw new InvalidValueException("Category is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.GroupCode))
            {
                throw new InvalidValueException("Group Code is Required!");
            }
        }

        public async Task<ItemTypeVM> UpdateAsync(ItemTypeVM model, string user, DateTime date)
        {
            ValidateRequired(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemType entity = await _db.ItemTypes.FindAsync(model.Id);

            entity.Code = model.Code;
            entity.Description = model.Description;
            entity.FormulaNo = model.FormulaNo;
            entity.Category = model.Category;
            entity.GroupCode = model.GroupCode;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.ItemTypes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<ItemTypeVM> DeleteAsync(ItemTypeVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ItemType entity = await _db.ItemTypes.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.ItemTypes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.ItemTypes.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        }

    }
}