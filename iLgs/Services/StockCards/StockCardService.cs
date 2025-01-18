using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Items;
using iLgs.Services.PropertyCard;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.StockCards
{
    public interface IStockCardService : IPsCardService
    {
        new IQueryable<StockCardVM> GetAll(string userName);
        StockCardVM GetById(Guid? id);
        ValueTask<StockCardVM> CreateAsync(StockCardVM model, string user, DateTime date);
        ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date);
        ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date);                
    }
    public class StockCardService : PsCardService, IStockCardService
    {
        private readonly IExceptionService<StockCardVM> _vmExceptionService = new ExceptionService<StockCardVM>();
        private readonly IStockCardValidator _validator;
        private readonly IItemCodeService _itemCodeService;
        private readonly IUserService _userService;

        public StockCardService(AppManEntities db) : base (db)
        {
            _validator = new StockCardValidator(db);
            _itemCodeService = new ItemCodeService(db);
            _userService = new UserService(_db);
        }               

        public new IQueryable<StockCardVM> GetAll(string userName)
        {            
            var data = _db.Database.SqlQuery<StockCardVM>("Exec Card_GetRecords 'S', {0}", userName).AsQueryable();
            return data;
        }

        public StockCardVM GetById(Guid? id) 
        {
            var data = _db.PsCards.Where(w => w.Id == id).AsNoTracking()
                .Where(w => w.ItemCode.ItemType.Category == "S")
                .Select(s => new StockCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    PartialPage = s.ItemCode.PartialPage == null ? s.ItemCode.ItemType.PartialPage : s.ItemCode.PartialPage,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                }).ToList()
                .Select(s => new StockCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.Item,
                    ItemNo = s.ItemNo,
                    ItemCode = s.ItemCode,
                    ItemType = s.ItemType,
                    ItemTypeCode = s.ItemTypeCode,
                    PartialPage = s.PartialPage,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _itemCodeService.GetSubAccounts(s.ItemCodeId), // Call the service on the in-memory data
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefault();            
            return data;
        }

        public ValueTask<StockCardVM> CreateAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnCreate(model);

            model.Description = "Please see attachment.";
            model.AllField = _allFieldService.ChangeAllFieldCase(model.AllField);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PsCard
            {
                Id = model.Id,
                ItemCodeId = model.ItemCodeId,
                SubAccountCode = model.SubAccountCode,
                Fund = model.Fund,
                Description = model.Description,
                Unit = model.Unit,
                CardCategory = model.CardCategory,
                PsNo = model.PsNo,
                PsName = model.PsName,
                PrevPsNo = model.PrevPsNo,
                FromDonation = model.FromDonation,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            model.AllField.Id = model.Id;
            model.AllField.InsertedBy = user;
            model.AllField.InsertedDt = date;
            model.AllField.UpdatedBy = user;
            model.AllField.UpdatedDt = date;
            entity.AllField = model.AllField;

            _db.PsCards.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });
        
        public ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {            
            _validator.ValidateOnUpdate(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCards.FindAsync(model.Id);

            ValidateUser(entity, model);

            model.AllField = _allFieldService.ChangeAllFieldCase(model.AllField);            
            entity.ItemCodeId = model.ItemCodeId;
            entity.SubAccountCode = model.SubAccountCode;
            entity.Fund = model.Fund;
            entity.Description = model.Description;
            entity.Unit = model.Unit;
            entity.CardCategory = model.CardCategory;
            entity.PsNo = model.PsNo;
            entity.PsName = model.PsName;
            entity.PrevPsNo = model.PrevPsNo;
            entity.FromDonation = model.FromDonation;
            entity.Amount = model.Amount;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;
            entity.AllField = model.AllField;

            _db.PsCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;

            //_db.AllFields.Attach(model.AllField);
            //_db.Entry(model.AllField).State = EntityState.Modified;

            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            var entity = await _db.PsCards.FindAsync(model.Id);

            ValidateUser(entity, model);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCards.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            
            return model;
        });

        private void ValidateUser(PsCard entity, StockCardVM model)
        {
            if (entity.InsertedBy != model.UpdatedBy)
            {
                var isAdmin = _userService.IsUserNameAdmin(model.UpdatedBy);
                if (!isAdmin)
                {
                    throw new RecordLockedException($"Record can only be updated by {entity.InsertedBy} or an Admin.");
                }
            }
        }
    }
}