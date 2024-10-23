using FluentValidation;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.Validators
{
    public interface IPropertyCardValidator
    {
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

        public PropertyCardValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<PropertyCardVM>(propertyName);
            _allFieldsValidator = new AllFieldsValidator(_db);
        }
        public void ValidateOnCreate(PropertyCardVM model)
        {
            ValidateCard(model);
            Validate(
                (Rule: IsInvalid(text: model.Fund), Parameter: _getDisplayName(nameof(PropertyCardVM.Fund))),
                (Rule: IsInvalid(text: model.PsNo), Parameter: _getDisplayName(nameof(PropertyCardVM.PsNo)))                
                );

            var ex = new InvalidModelException();
            _allFieldsValidator.ValidateAllFields(model.AllField, model.ItemTypeCode, model.ItemCode, ex, Module.CARD);
            if (_db.PsCards.Any(a => a.PsNo == model.PsNo))
            {
                ex.UpsertDataList(_getDisplayName(nameof(model.PsNo)), "Already exits.");
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
            _allFieldsValidator.ValidateAllFields(model.AllField, model.ItemTypeCode, model.ItemCode, ex, Module.CARD);
            if (_db.PsCards.Any(a => a.PsNo == model.PsNo && a.Id != model.Id))
            {
                ex.UpsertDataList(Utility.GetDisplayName<PropertyCardVM>(nameof(model.PsNo)), "Already exits.");
            }
            ex.ThrowIfContainsErrors();
        }
        
        public void ValidateOnDelete(PropertyCardVM model)
        {
            ValidateCard(model);
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