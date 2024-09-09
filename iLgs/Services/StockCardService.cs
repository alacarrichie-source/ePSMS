using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IStockCardService : IPsCardService
    {
        ValueTask<StockCardVM> GetByIdAsync(Guid? id);
        ValueTask<StockCardVM> CreateAsync(StockCardVM model, string user, DateTime date);
        ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date);
        ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date);                
    }
    public class StockCardService : PsCardService, IStockCardService
    {
        private readonly IExceptionService<ServiceResult<StockCardVM>> _exceptionService = new ExceptionService<ServiceResult<StockCardVM>>();
        private readonly IExceptionService<StockCardVM> _vmExceptionService = new ExceptionService<StockCardVM>();
        private readonly IStockCardValidator _validator;        

        public StockCardService(AppManEntities db) : base (db)
        {
            _validator = new StockCardValidator(db);            
        }               

        public new IQueryable<StockCardVM> GetAll() => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCards.AsNoTracking()
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
                    FieldGroupNo = s.ItemCode.ItemType.FormulaNo,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    Amount = s.Amount,
                    FromDonation = s.FromDonation,
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<StockCardVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCards.Where(w => w.Id == id).AsNoTracking()
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
                    FieldGroupNo = s.ItemCode.ItemType.FormulaNo,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();            
            return data;
        });

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

            //entity = SetItemEntity(entity, model);

            _db.PsCards.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });
        
        public ValueTask<StockCardVM> UpdateAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            //var result = await _validator.ValidateAsync(model, "Update");
            //if (!result.IsSuccess)
            //{
            //    return ServiceResult<StockCardVM>.Failure(result.Errors);
            //}

            var entity = await _db.PsCards.FindAsync(model.Id);

            //await _allFieldService.ValidatePsCardAllField(model);
            
            model.AllField = _allFieldService.ChangeAllFieldCase(model.AllField);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

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

            model.AllField.Id = model.Id;
            model.AllField.UpdatedBy = user;
            model.AllField.UpdatedDt = date;
            entity.AllField = model.AllField;

            //entity = SetItemEntity(entity, model);

            _db.PsCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<StockCardVM> DeleteAsync(StockCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCards.FindAsync(model.Id);

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
    }
}