using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Items;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public interface IItemTypeService
    {
        IQueryable<ItemTypeVM> GetAll();
        Task<ItemType> GetByIdAsync(Guid id);
        IQueryable<ItemType> GetRpciAccounts(string text);
        IQueryable<CustodianAccountVM> GetCustodianMainAccounts(int? accountGroup, string text);

        Task<ItemTypeVM> CreateAsync(ItemTypeVM model, string user, DateTime date);
        Task<ItemTypeVM> UpdateAsync(ItemTypeVM model, string user, DateTime date);
        Task<ItemTypeVM> DeleteAsync(ItemTypeVM model, string user, DateTime date);
    }

    public class ItemTypeService : IItemTypeService
    {
        private readonly AppManEntities _db ;
        private readonly IItemCodeService _itemCodeService;

        public ItemTypeService(AppManEntities db,
            IItemCodeService itemCodeService)
        {
            _db = db;
            _itemCodeService = itemCodeService;
        }
        
        private Expression<Func<ItemType, ItemTypeVM>> Projection(AppManEntities _db)
        {
            return s => new ItemTypeVM
            {
                Id = s.Id,
                Code = s.Code,
                Description = s.Description,
                PartialPage = s.PartialPage,
                RequiredFields = _db.Codextns.Where(w => w.Desc2 == s.PartialPage && w.CodeMast.Code == "REQUIRED-FIELDS").FirstOrDefault().Description,
                Category = s.Category,
                CategoryDesc = _db.Codextns.Where(w => w.Code == s.Category && w.CodeMast.Code == "PS-CATEGORY").FirstOrDefault().Description,
                GroupCode = s.GroupCode,
                InsertedDt = s.InsertedDt
            };
        }

        public IQueryable<ItemTypeVM> GetAll()
        {
            var data = _db.ItemTypes.Select(Projection(_db));
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

        public IQueryable<CustodianAccountVM> GetCustodianMainAccounts(int? accountGroup, string text)
        {
            var data = _db.Database.SqlQuery<CustodianAccountVM>("Exec ItemCodes_GetCustodianMainAccount {0}, {1}", accountGroup, text).AsQueryable().AsNoTracking();
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
                Id = (Guid)model.Id,
                Code = model.Code,
                Description = model.Description,
                PartialPage = model.PartialPage,
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

            ItemType entity = await _db.ItemTypes.Include(i => i.ItemCodes).FirstOrDefaultAsync(f => f.Id == model.Id);

            ValidateRecord(entity, (Guid)model.Id);
            ValidateRelationship(entity);

            entity.Code = model.Code;
            entity.Description = model.Description;
            entity.PartialPage = model.PartialPage;
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

            ValidateRecord(entity, (Guid)model.Id);
            ValidateRelationship(entity);

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

        private void ValidateRecord(ItemType entity, Guid id)
        {
            if (entity is null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateRelationship(ItemType entity)
        {
            foreach(var itemCode in entity.ItemCodes)
            {
                _itemCodeService.ValidateRelationship(itemCode.Id);
            }
        }
    }
}