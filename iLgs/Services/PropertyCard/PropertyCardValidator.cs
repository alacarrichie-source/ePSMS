using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPropertyCardValidator
    {
        Task<bool> IsPsNoAlreadyExistsAsync(PropertyCardVM model, Mode mode);
        Task ValidateOnCreateAsync(PropertyCardVM model);
        Task ValidateOnUpdateAsync(PropertyCardVM model);
        Task ValidateOnDeleteAsync(PropertyCardVM model);
    }

    public class PropertyCardValidator : BaseValidator, IPropertyCardValidator
    {
        //private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly AppManEntities _db;
        private readonly IAllFieldsValidator _allFieldsValidator;
        private readonly IItemCodeService _itemCodeService;

        public PropertyCardValidator(AppManEntities db,
            IItemCodeService itemCodeService,
            IAllFieldsValidator allFieldsValidator)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PropertyCardVM>(propertyName);
            _allFieldsValidator = allFieldsValidator;
            _itemCodeService = itemCodeService;
        }
        public async Task ValidateOnCreateAsync(PropertyCardVM model)
        {
            ValidateCard(model);
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(PropertyCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(PropertyCardVM.PsNo)))                
                );

            var ex = new InvalidModelException();
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);
            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            if (await IsPsNoAlreadyExistsAsync(model, Mode.ADD))
            {
                ex.UpsertDataList(Utility.GetDisplayName<PropertyCardVM>(nameof(model.PsNo)), "Already exists.");
            }

            ex.ThrowIfContainsErrors();
        }


        public async Task ValidateOnUpdateAsync(PropertyCardVM model)
        {
            ValidateCard(model);
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(PropertyCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(PropertyCardVM.PsNo)))
                );

            var ex = new InvalidModelException();
            //var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            //string partialView = AllFieldsUtil.GetPartialView(itemCode);
            string partialView = await _itemCodeService.GetPartialViewAsync(model.ItemCodeId);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            if (await IsPsNoAlreadyExistsAsync(model, Mode.EDIT))
            {
                ex.UpsertDataList(Utility.GetDisplayName<PropertyCardVM>(nameof(model.PsNo)), "Already exists.");
            }
            ex.ThrowIfContainsErrors();
        }

        public async Task<bool> IsPsNoAlreadyExistsAsync(PropertyCardVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                return await _db.PsCards.AnyAsync(a => a.PsNo == model.PsNo && a.Fund == model.Fund);
            }
            return await _db.PsCards.AnyAsync(a => a.PsNo == model.PsNo && a.Fund == model.Fund && a.Id != model.Id);
        }

        public async Task ValidateOnDeleteAsync(PropertyCardVM model)
        {
            ValidateCard(model);
            if (await _db.PsCardItems.AnyAsync(a => a.PsCardId == model.Id))
            {
                throw new RecordRelationshipException("Cannot delete card with items, please delete the items first.");
            }
        }

        private static void ValidateCard(PropertyCardVM card)
        {
            if (card is null)
            {
                throw new NullException();
            }
        }        
    }
}