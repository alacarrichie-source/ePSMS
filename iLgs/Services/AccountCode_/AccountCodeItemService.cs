using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.AccountCode_
{
    public interface IAccountCodeItemService
    {
        IQueryable<AccountCodeItemVM> GetAll(Guid? ppmpId);
        IQueryable<ItemTypeAccountVM> GetAllItemTypeAccount(Guid? accountCodeId);
        ValueTask<AccountCodeItemVM> GetByIdAsync(Guid? id);
        
        ValueTask<ItemTypeAccountVM> SaveAsync(ItemTypeAccountVM model, string user, DateTime date);
    }

    internal class AccountCodeItemService : BaseValidator, IAccountCodeItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<AccountCodeItemVM> _vmExceptionService = new ExceptionService<AccountCodeItemVM>();
        private readonly IExceptionService<ItemTypeAccountVM> _itemTypeAccountExceptionService = new ExceptionService<ItemTypeAccountVM>();
        private readonly IExceptionService<PPMPUploadVM> _uploadExceptionService = new ExceptionService<PPMPUploadVM>();
        private readonly GetDisplayNameDelegate _getDisplayName = propertyName => Utility.GetDisplayName<AccountCodeItemVM>(propertyName);

        private readonly ICodextnService _codextnService;
        
        public AccountCodeItemService(AppManEntities db)
        {
            _db = db;
            _codextnService = new CodextnService(_db);            
        }

        private Expression<Func<AccountCodeItem, AccountCodeItemVM>> Projection()
        {
            return s => new AccountCodeItemVM
            {
                Id = s.Id,
                AccountCodeId = s.AccountCodeId,
                ItemTypeId = s.ItemTypeId,
                ItemCodeId = s.ItemCodeId,
                Code = s.Code,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt
            };
        }

        public ValueTask<AccountCodeItemVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.AccountCodeItems.AsNoTracking().Where(w => w.Id == id)
                .Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<AccountCodeItemVM> GetAll(Guid? accountCodeId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.AccountCodeItems.AsNoTracking().Where(w => w.AccountCodeId == accountCodeId)
                .Select(Projection());
            return data;
        });

        public IQueryable<ItemTypeAccountVM> GetAllItemTypeAccount(Guid? accountCodeId) => _itemTypeAccountExceptionService.TryCatch(() =>
        {
            var data = _db.ItemTypes.Include(i => i.AccountCodeItems)
                .Select(s => new ItemTypeAccountVM {
                    Id = s.Id,
                    Code = s.Code,
                    Description = s.Description,
                    Category = s.Category,
                    CategoryDesc = _db.Codextns.Where(w => w.Code == s.Category && w.CodeMast.Code == "PS-CATEGORY").FirstOrDefault().Description,
                    GroupCode = s.GroupCode,                    
                    AccountCodeId = accountCodeId,
                    AccountItemCode = s.AccountCodeItems.Where(w => w.AccountCodeId == accountCodeId && w.ItemTypeId == s.Id).Select(x => x.Code).FirstOrDefault()
                });
            return data;
        });

        private async Task ValidateFieldsAsync(ItemTypeAccountVM model)
        {
            _imex = new InvalidModelException();

            
            //if (Enum.TryParse(model.PsType, out Category c))
            //{
            //    if (_allFieldService.IsBrandRequired(c))
            //    {
            //        if (string.IsNullOrWhiteSpace(model.AllField.Brand))
            //        {
            //            _imex.UpsertDataList(_getDisplayName(nameof(model.AllField.Brand)), "Field is required.");
            //        }
            //    }                                   
            //}

            _imex.ThrowIfContainsErrors();
        }
        
        public ValueTask<ItemTypeAccountVM> SaveAsync(ItemTypeAccountVM model, string user, DateTime date) => _itemTypeAccountExceptionService.TryCatch(async () =>
        {
            await ValidateFieldsAsync(model);
            Mode mode;
            var entity = await _db.AccountCodeItems.Where(w => w.AccountCodeId == model.AccountCodeId && w.ItemTypeId == model.Id).FirstOrDefaultAsync(); 

            if (entity == null)
            {
                model.InsertedBy = user;
                model.InsertedDt = date;

                entity = new AccountCodeItem();
                entity.Id = Guid.NewGuid();
                entity.AccountCodeId = model.AccountCodeId;
                entity.ItemTypeId = model.Id; // From ItemType.Id
                
                entity.InsertedBy = user;
                entity.InsertedDt = date;

                mode = Mode.ADD;
            }
            else
            {                
                mode = Mode.EDIT;               
            }

            entity.Code = model.AccountItemCode?.Trim();
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;            

            if (mode == Mode.ADD)
            {
                _db.AccountCodeItems.Add(entity);
            }

            await _db.SaveChangesAsync();

            return model;
        });

        private void ValidateIfNull(ItemTypeAccountVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(AccountCodeItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }
    }
}