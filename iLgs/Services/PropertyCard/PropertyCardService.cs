using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Items;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.PropertyCard
{
    public interface IPropertyCardService : IPsCardService
    {
        IQueryable<PropertyCardVM> GetAll();
        ValueTask<PropertyCardVM> GetByIdAsync(Guid id);
        ValueTask<PropertyCardVM> CreateAsync(PropertyCardVM model, string user, DateTime date);
        ValueTask<PropertyCardVM> UpdateAsync(PropertyCardVM model, string user, DateTime date);
        ValueTask<PropertyCardVM> DeleteAsync(PropertyCardVM model, string user, DateTime date);        
    }

    public class PropertyCardService : PsCardService, IPropertyCardService
    {
        private readonly IExceptionService<PropertyCardVM> _propCardVMexceptionService;
        private readonly IPropertyCardValidator _validator;
        private readonly IItemCodeService _itemCodeService;
        private readonly IUserService _userService;
                
        public PropertyCardService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            ICreateAndLogExceptions exceptions,
            IExceptionService<PsCardVM> vmExceptionService,
            IExceptionService<PsCard> exceptionService,
            IAllFieldService allFieldService,
            IPsCardItemService psCardItemService,
            IPsCardSharedService psCardSharedService,
            IExceptionService<PropertyCardVM> propCardVMexceptionService,
            IPropertyCardValidator validator,
            IItemCodeService itemCodeService,
            IUserService userService) : base(db, appManEntitiesFactory, exceptions, vmExceptionService, exceptionService, allFieldService, psCardSharedService, psCardItemService) //, psCardItemIssuanceService)
        {
            _propCardVMexceptionService = propCardVMexceptionService;
            _validator = validator;
            _itemCodeService = itemCodeService;
            _userService = userService;
        }

        public IQueryable<PropertyCardVM> GetAll() => _propCardVMexceptionService.TryCatch(() =>
        {            
            var data = _db.Database.SqlQuery<PropertyCardVM>("Exec Card_GetRecords 'P'").AsQueryable();
            return data;
        });

        public ValueTask<PropertyCardVM> GetByIdAsync(Guid id) => _propCardVMexceptionService.TryCatch(async () =>
        {
            var list = await _db.PsCards.Where(w => w.Id == id).AsNoTracking()
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
               }).ToListAsync();

            var data = list.Select(s => new PropertyCardVM
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
                   SubAccount = _itemCodeService.GetSubAccounts(s.ItemCodeId), 
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
        });

        public ValueTask<PropertyCardVM> CreateAsync(PropertyCardVM model, string user, DateTime date) => _propCardVMexceptionService.TryCatch(async () =>
        {
            await _validator.ValidateOnCreateAsync(model);

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

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                ctx.PsCards.Add(entity);
                await ctx.SaveChangesAsync();
            }

            return model;
        });

        public ValueTask<PropertyCardVM> UpdateAsync(PropertyCardVM model, string user, DateTime date) => _propCardVMexceptionService.TryCatch(async () =>
        {            
            await _validator.ValidateOnUpdateAsync(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.PsCards.Include(i => i.AllField).FirstOrDefaultAsync(f => f.Id == model.Id);

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

                //_db.PsCards.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                
                await ctx.SaveChangesAsync();
            }

            return model;
        });

        public ValueTask<PropertyCardVM> DeleteAsync(PropertyCardVM model, string user, DateTime date) => _propCardVMexceptionService.TryCatch(async () =>
        {
            await _validator.ValidateOnDeleteAsync(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.PsCards.FindAsync(model.Id);

                ValidateUser(entity, model);

                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                //_db.PsCards.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                ctx.PsCards.Remove(entity);
                //_db.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();
            }

            return model;
        });

        private void ValidateUser(PsCard entity, PropertyCardVM model)
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