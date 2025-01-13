using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Items;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System.Linq;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPropertyCardValidator
    {
        bool IsPsNoAlreadyExists(PropertyCardVM model, Mode mode);
        void ValidateOnCreate(PropertyCardVM model);
        void ValidateOnUpdate(PropertyCardVM model);
        void ValidateOnDelete(PropertyCardVM model);
    }

    public class PropertyCardValidator : BaseValidator, IPropertyCardValidator
    {
        private delegate string GetDisplayNameDelegate(string propertyName);
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly AppManEntities _db;
        private readonly IAllFieldsValidator _allFieldsValidator;
        private readonly IItemCodeService _itemCodeService;

        public PropertyCardValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PropertyCardVM>(propertyName);
            _allFieldsValidator = new AllFieldsValidator(_db);
            _itemCodeService = new ItemCodeService(_db);
        }
        public void ValidateOnCreate(PropertyCardVM model)
        {
            ValidateCard(model);
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(PropertyCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(PropertyCardVM.PsNo)))                
                );

            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            if (IsPsNoAlreadyExists(model, Mode.ADD))
            {
                ex.UpsertDataList(Utility.GetDisplayName<PropertyCardVM>(nameof(model.PsNo)), "Already exists.");
            }

            ex.ThrowIfContainsErrors();
        }


        public void ValidateOnUpdate(PropertyCardVM model)
        {
            ValidateCard(model);
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(PropertyCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(PropertyCardVM.PsNo)))
                );

            var ex = new InvalidModelException();
            var itemCode = _itemCodeService.GetById(model.ItemCodeId);
            string partialView = AllFieldsUtil.GetPartialView(itemCode);
            _allFieldsValidator.ValidateAllFieldsPartial(model.AllField, partialView, ex, Module.CARD);

            if (IsPsNoAlreadyExists(model, Mode.EDIT))
            {
                ex.UpsertDataList(Utility.GetDisplayName<PropertyCardVM>(nameof(model.PsNo)), "Already exists.");
            }
            ex.ThrowIfContainsErrors();
        }

        public bool IsPsNoAlreadyExists(PropertyCardVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                return _db.PsCards.Any(a => a.PsNo == model.PsNo && a.Fund == model.Fund);
            }
            return _db.PsCards.Any(a => a.PsNo == model.PsNo && a.Fund == model.Fund && a.Id != model.Id);
        }

        public void ValidateOnDelete(PropertyCardVM model)
        {
            ValidateCard(model);
            if (_db.PsCardItems.Any(a => a.PsCardId == model.Id))
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