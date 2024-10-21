using FluentValidation;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.AllFields
{
    public interface IAllFieldsValidator
    {
        void ValidateAllFields(AllField af, string category, string itemNo, InvalidModelException ex);
        void ValidateAllFields(AllField af, string category, string itemNo, InvalidModelException ex, Enums.Module? module);
    }

    public class AllFieldsValidator : BaseValidator, IAllFieldsValidator
    {
        private readonly AppManEntities _db;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly GetDisplayNameDelegate _getAllFieldDisplayName;

        public AllFieldsValidator(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<StockCardVM>(propertyName);
            _getAllFieldDisplayName = propertyName => Utility.GetDisplayName<AllField>(propertyName);
        }

        private void ValidateFields(AllField af, List<string> f, InvalidModelException ex)
        {
            // Loop through the selected property names in the 'f' list
            for (int i = 1; i < f.Count; i++) // Start from 1 and use f.Count for the loop condition
            {
                // Get the current and previous property names from the list 'f'
                var currentProperty = typeof(AllField).GetProperty(f[i - 1]);
                var nextProperty = typeof(AllField).GetProperty(f[i]);

                if (currentProperty == null || nextProperty == null)
                {
                    throw new InvalidOperationException("Invalid property name in the list.");
                }

                // Get the values of the current and previous properties
                var currentValue = (string)currentProperty.GetValue(af);
                var nextValue = (string)nextProperty.GetValue(af);

                if (!string.IsNullOrEmpty(currentValue) && string.IsNullOrEmpty(nextValue))
                {
                    // Get the display name of the previous property
                    var displayName = _getAllFieldDisplayName(nextProperty.Name);
                    ex.UpsertDataList(displayName, "Field is required.");
                }
            }
        }

        public void ValidateAllFields(AllField af, string category, string itemNo, InvalidModelException ex)
        {
            ValidateAllFields(af, category, itemNo, ex, null);
        }

        public void ValidateAllFields(AllField af, string category, string itemNo, InvalidModelException ex, Enums.Module? module)
        {
            var group = AllFieldsUtil.GetCategoryGroup(category, itemNo);
            if (group == CategoryGroup.DRUGS)
            {
                if (string.IsNullOrWhiteSpace(af.GenericName))
                {
                    ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.GenericName)), "Field is required.");
                }

                if (itemNo.Contains("-5.1.")) // Alcoh1ol
                {
                    if (string.IsNullOrWhiteSpace(af.DosageVolume))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.DosageVolume)), "Field is required.");
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(af.DosageStrength))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.DosageStrength)), "Field is required.");
                    }
                    if (string.IsNullOrWhiteSpace(af.DosageForm))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.DosageForm)), "Field is required.");
                    }
                }

                if (af.Multipliers == null)
                {
                    ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Multipliers)), "Field is required.");
                }

                if (module == Enums.Module.CARD)
                {
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Brand)), "Field is required.");
                    }
                }
            }
            else if (group == CategoryGroup.SERIAL_A)
            {
                List<string> f = new List<string>();
                f = new List<string>
                    {
                        "PropNo", "SerialNo"
                    };
                ValidateFields(af, f, ex);
            }
            else if (group == CategoryGroup.SERIAL_B)
            {
                List<string> f = new List<string>();
                f = new List<string>
                    {
                        "Brand", "MVFileNo", "BodyNo", "PlateNo", "PropNo", "SerialNo"
                    };
                ValidateFields(af, f, ex);
            }
            else if (group == CategoryGroup.SERIAL_C)
            {
                List<string> f = new List<string>();
                f = new List<string>
                    {
                        "Color", "Capacity", "Materials", "Weight", "Size", "Dimension", "Model_", "Brand", "PropNo", "SerialNo"
                    };
                ValidateFields(af, f, ex);
            }
            else if (group == CategoryGroup.SERIAL)
            {
                List<string> f = new List<string>();
                f = new List<string>
                    {
                        "Color", "Capacity", "Materials", "Weight", "Size", "Dimension", "Model_", "Brand", "MVFileNo", "BodyNo", "PlateNo", "PropNo", "SerialNo"
                    };
                ValidateFields(af, f, ex);

            }
            else if (group == CategoryGroup.OTHERS)
            {
                if (module == Enums.Module.CARD || module == Enums.Module.ORDER)
                {
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Brand)), "Field is required.");
                    }
                }
               
                if (string.IsNullOrWhiteSpace(af.Model_))
                {
                    ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Model_)), "Field is required.");
                }
                else
                    {
                        if (af.Model_.IsNullOrWhiteSpaceX())
                        {
                            if (string.IsNullOrWhiteSpace(af.Dimension))
                            {
                                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Dimension)), "Field is required.");
                            }
                            else
                            {
                                if (af.Dimension.IsNullOrWhiteSpaceX())
                                {
                                    if (string.IsNullOrWhiteSpace(af.Size))
                                    {
                                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Size)), "Field is required.");
                                    }
                                    else
                                    {
                                        if (af.Size.IsNullOrWhiteSpaceX())
                                        {
                                            if (string.IsNullOrWhiteSpace(af.Weight))
                                            {
                                                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Weight)), "Field is required.");
                                            }
                                            else
                                            {
                                                if (af.Weight.IsNullOrWhiteSpaceX())
                                                {
                                                    if (string.IsNullOrWhiteSpace(af.Materials))
                                                    {
                                                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Materials)), "Field is required.");
                                                    }
                                                    else
                                                    {
                                                        if (af.Materials.IsNullOrWhiteSpaceX())
                                                        {
                                                            if (string.IsNullOrWhiteSpace(af.Capacity))
                                                            {
                                                                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Capacity)), "Field is required.");
                                                            }
                                                            else
                                                            {
                                                                if (af.Capacity.IsNullOrWhiteSpaceX())
                                                                {
                                                                    if (af.Color.IsNullOrWhiteSpaceX())
                                                                    {
                                                                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Color)), "Field is required.");
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }                
            }
        }
    }

    //public class AllFieldsValidator : AbstractValidator<PsCardVM>
    //{
    //    private readonly AppManEntities _db;
    //    public AllFieldsValidator(AppManEntities db)
    //    {
    //        _db = db;

    //        RuleFor(x => x.AllField).Custom((af, context) =>
    //        {
    //            var model = context.InstanceToValidate as PsCardVM;

    //            if (Enum.TryParse(model.ItemTypeCode, out Category c))
    //            {
    //                if (c == CatDrugs())
    //                {
    //                    RuleFor(x => af.GenericName).NotEmpty().WithMessage("Generic Name is Required!");

    //                    if (model.ItemNo.StartsWith("5.1."))
    //                    {
    //                        RuleFor(x => af.DosageVolume).NotEmpty().WithMessage("Dosage Volume is Required!");
    //                    }
    //                    else
    //                    {
    //                        RuleFor(x => af.DosageStrength).NotEmpty().WithMessage("Dosage Strength is Required!");
    //                        RuleFor(x => af.DosageForm).NotEmpty().WithMessage("Dosage Form is Required!");
    //                    }
    //                }
    //                else if (IsMachineryOrOtherCategory(c))
    //                {
    //                    RuleFor(x => af.Brand).NotEmpty().WithMessage("Brand is Required!");
    //                    RuleFor(x => af.Model_).NotEmpty().WithMessage("Model is Required!");
    //                    RuleFor(x => af.Dimension).NotEmpty().WithMessage("Dimension is Required!");
    //                    RuleFor(x => af.Size).NotEmpty().WithMessage("Size is Required!");
    //                    RuleFor(x => af.Weight).NotEmpty().WithMessage("Weight is Required!");
    //                    RuleFor(x => af.Materials).NotEmpty().WithMessage("Materials is Required!");
    //                    RuleFor(x => af.Capacity).NotEmpty().WithMessage("Capacity is Required!");
    //                    RuleFor(x => af.Color).NotEmpty().WithMessage("Color is Required!");

    //                    //RuleFor(x => af.Model_)
    //                    //    .Must((m, size) => !string.IsNullOrWhiteSpace(af.Model_) || !string.IsNullOrWhiteSpace(af.Size) ||
    //                    //                       !string.IsNullOrWhiteSpace(af.Dimension) || !string.IsNullOrWhiteSpace(af.Weight) ||
    //                    //                       !string.IsNullOrWhiteSpace(af.Materials) || !string.IsNullOrWhiteSpace(af.Capacity) ||
    //                    //                       !string.IsNullOrWhiteSpace(af.Color))
    //                    //    .WithMessage("Model or Dimension or Size or Weight or Materials or Capacity or Color is Required!");
    //                }
    //            }
    //        });
    //    }

    //    private bool IsMachineryOrOtherCategory(Category c)
    //    {
    //        return c == CatMachineries()
    //            || c == CatTransportations()
    //            || c == CatFurnitures()
    //            || c == CatOtherProperties()
    //            || c == CatMedicals()
    //            || c == CatAgriculturals()
    //            || c == CatAnimalSupplies()
    //            || c == CatConstructionMaterials()
    //            || c == CatOfficeSupplies()
    //            || c == CatAccountableForms()
    //            || c == CatNonAccountableForns()
    //            || c == CatMilitaries()
    //            || c == CatOtherSupplies();
    //    }
    //}

}