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
    public interface IPropertyCardService : IPsCardService
    {
        new IQueryable<PropertyCardVM> GetAll();
        ValueTask<PropertyCardVM> GetByIdAsync(Guid? id);
        ValueTask<PropertyCardVM> CreateAsync(PropertyCardVM model, string user, DateTime date);
        ValueTask<PropertyCardVM> UpdateAsync(PropertyCardVM model, string user, DateTime date);
        ValueTask<PropertyCardVM> DeleteAsync(PropertyCardVM model, string user, DateTime date);        
    }

    public class PropertyCardService : PsCardService, IPropertyCardService
    {
        private readonly IExceptionService<PropertyCardVM> _exceptionService = new ExceptionService<PropertyCardVM>();
        private readonly IExceptionService<PropertyCardVM> _vmExceptionService = new ExceptionService<PropertyCardVM>();
        private readonly IPropertyCardValidator _validator;
        
        public PropertyCardService(AppManEntities db) : base(db)
        {
            _validator = new PropertyCardValidator(db);
        }

        public new IQueryable<PropertyCardVM> GetAll() => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCards.AsNoTracking()
            .Where(w => w.ItemCode.ItemType.Category != "S")
                .Select(s => new PropertyCardVM
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

        public ValueTask<PropertyCardVM> GetByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCards.Where(w => w.Id == id).AsNoTracking()
                .Select(s => new PropertyCardVM
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

        public ValueTask<PropertyCardVM> CreateAsync(PropertyCardVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            //var result = await _validator.ValidateAsync(model, "Create");
            //if (!result.IsSuccess)
            //{
            //    return ServiceResult<PropertyCardVM>.Failure(result.Errors);
            //}

            //_allFieldService.ValidatePropertyCardAllField(model);

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

        public ValueTask<PropertyCardVM> UpdateAsync(PropertyCardVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            //var result = await _validator.ValidateAsync(model, "Update");
            //if (!result.IsSuccess)
            //{
            //    return ServiceResult<PropertyCardVM>.Failure(result.Errors);
            //}

            _validator.ValidateOnUpdate(model);

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

        public ValueTask<PropertyCardVM> DeleteAsync(PropertyCardVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _validator.ValidateOnDelete(model);

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