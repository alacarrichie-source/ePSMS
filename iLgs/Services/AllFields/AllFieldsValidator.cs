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
        //void ValidateAllFields(AllField af, string category, string itemCode, InvalidModelException ex);
        //void ValidateAllFields(AllField af, string category, string itemCode, InvalidModelException ex, Enums.Module? module);

        void ValidateAllFieldsPartial(AllField af, string partialView, InvalidModelException ex);
        void ValidateAllFieldsPartial(AllField af, string partialView, InvalidModelException ex, Enums.Module? module);
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

        public void ValidateAllFields(AllField af, string category, string itemCode, InvalidModelException ex)
        {
            ValidateAllFields(af, category, itemCode, ex, null);
        }

        public void ValidateAllFields(AllField af, string category, string itemCode, InvalidModelException ex, Enums.Module? module)
        {
            var group = AllFieldsUtil.GetCategoryGroup(category, itemCode);
            if (group == CategoryGroup.DRUGS)
            {
                if (string.IsNullOrWhiteSpace(af.GenericName))
                {
                    ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.GenericName)), "Field is required.");
                }

                if (itemCode.Contains("-5.1.")) // Alcoh1ol
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
            else if (group == CategoryGroup.OTHERS || group == CategoryGroup.OTHERS_A || group == CategoryGroup.OTHERS_B)
            {
                if (group == CategoryGroup.OTHERS) // supply
                {
                    if (af.Multipliers == null)
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Multipliers)), "Field is required.");
                    }
                }

                if (module == Enums.Module.CARD || module == Enums.Module.ORDER)
                {
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Brand)), "Field is required.");
                    }
                }

                if (group == CategoryGroup.OTHERS_B) // vehicles
                {
                    if (string.IsNullOrWhiteSpace(af.Model_))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Model_)), "Field is required.");
                    }
                    else
                    {
                        if (af.Model_.IsNullOrWhiteSpaceX())
                        {
                            if (string.IsNullOrWhiteSpace(af.Weight))
                            {
                                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Weight)), "Field is required.");
                            }
                            else
                            {
                                if (af.Weight.IsNullOrWhiteSpaceX())
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
                else
                {
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

        public void ValidateAllFieldsPartial(AllField af, string partialView, InvalidModelException ex)
        {
            ValidateAllFieldsPartial(af, partialView, ex, null);
        }

        public void ValidateAllFieldsPartial(AllField af, string partialView, InvalidModelException ex, Enums.Module? module)
        {
            if (partialView == "_FieldDrugs" || partialView == "_FieldAlcohol")
            {
                if (string.IsNullOrWhiteSpace(af.GenericName))
                {
                    ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.GenericName)), "Field is required.");
                }

                if (partialView == "_FieldAlcohol")
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

                if (module == Enums.Module.CARD || module == Enums.Module.ORDER)
                {
                    if (string.IsNullOrWhiteSpace(af.Brand))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Brand)), "Field is required.");
                    }
                }
            }
            else if (partialView == "_FieldMultiple")
            {
                ValidateMultiples(af, partialView, ex, module);
            }
            else if (partialView == "_FieldMultiple_A")
            {
                ValidateMultiples(af, partialView, ex, module);
                ValidateBrand(af, partialView, ex, module);
            }
            else if (partialView == "_FieldSerial")
            {
                ValidateSerial(af, partialView, ex, module);
                ValidateMultiples(af, partialView, ex, module);
                ValidateBrand(af, partialView, ex, module);
                ValidateModel(af, partialView, ex, module);
            }
            else if (partialView == "_FieldSerial_A")
            {
                ValidatePlate(af, partialView, ex, module);
                ValidateMultiples(af, partialView, ex, module);
                ValidateBrand(af, partialView, ex, module);
                ValidateModel(af, partialView, ex, module);
            }
            else if (partialView == "_FieldSerial_B")
            {
                ValidateSerial(af, partialView, ex, module);
                ValidateMultiples(af, partialView, ex, module);
            }
            else if (partialView == "_FieldSerial_C")
            {
                ValidatePlate(af, partialView, ex, module);
                ValidateMultiples(af, partialView, ex, module);
            }
            else if (partialView == "_FieldSerial_D")
            {
                ValidateSerial(af, partialView, ex, module);
                ValidateBrand(af, partialView, ex, module);
                ValidateModel(af, partialView, ex, module);
            }
            else if (partialView == "_FieldSerial_E")
            {
                ValidatePlate(af, partialView, ex, module);                
            }
            else if (partialView == "_FieldSerial_F")
            {
                ValidateSerial(af, partialView, ex, module);
            }
            else if (partialView.Contains("Brand"))
            {
                if (partialView == "_FieldBrand")
                {
                    ValidateMultiples(af, partialView, ex, module);
                }

                ValidateBrand(af, partialView, ex, module);

                if (partialView == "_FieldBrand_B") // vehicles
                {
                    ValidateModelV(af, partialView, ex, module);                    
                }
                else
                {
                    ValidateModel(af, partialView, ex, module);                    
                }
            }
        }

        private void ValidateSerial(AllField af, string patialView, InvalidModelException ex, Enums.Module? module)
        {
            if (string.IsNullOrWhiteSpace(af.SerialNo))
            {
                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.SerialNo)), "Field is required.");
            }
            else
            {
                if (af.SerialNo.IsNullOrWhiteSpaceX())
                {
                    if (string.IsNullOrWhiteSpace(af.PropNo))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.PropNo)), "Field is required.");
                    }
                }
            }
        }

        private void ValidatePlate(AllField af, string patialView, InvalidModelException ex, Enums.Module? module)
        {
            if (string.IsNullOrWhiteSpace(af.PlateNo))
            {
                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.PlateNo)), "Field is required.");
            }
            else
            {
                if (af.PlateNo.IsNullOrWhiteSpaceX())
                {
                    if (string.IsNullOrWhiteSpace(af.BodyNo))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.BodyNo)), "Field is required.");
                    }
                    else
                    {
                        if (af.BodyNo.IsNullOrWhiteSpaceX())
                        {
                            if (string.IsNullOrWhiteSpace(af.MVFileNo))
                            {
                                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.MVFileNo)), "Field is required.");
                            }
                        }
                    }
                }
            }
        }

        private void ValidateMultiples(AllField af, string patialView, InvalidModelException ex, Enums.Module? module)
        {
            if (af.Multipliers == null)
            {
                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Multipliers)), "Field is required.");
            }

            if (module == Enums.Module.CARD || module == Enums.Module.ORDER)
            {
                if (string.IsNullOrWhiteSpace(af.Brand))
                {
                    ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Brand)), "Field is required.");
                }
            }
        }

        private void ValidateBrand(AllField af, string patialView, InvalidModelException ex, Enums.Module? module)
        {
            if (module == Enums.Module.CARD || module == Enums.Module.ORDER)
            {
                if (string.IsNullOrWhiteSpace(af.Brand))
                {
                    ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Brand)), "Field is required.");
                }
            }
        }

        private void ValidateModel(AllField af, string patialView, InvalidModelException ex, Enums.Module? module)
        {
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

        private void ValidateModelV(AllField af, string patialView, InvalidModelException ex, Enums.Module? module)
        {
            if (string.IsNullOrWhiteSpace(af.Model_))
            {
                ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Model_)), "Field is required.");
            }
            else
            {
                if (af.Model_.IsNullOrWhiteSpaceX())
                {
                    if (string.IsNullOrWhiteSpace(af.Weight))
                    {
                        ex.UpsertDataList(_getAllFieldDisplayName(nameof(af.Weight)), "Field is required.");
                    }
                    else
                    {
                        if (af.Weight.IsNullOrWhiteSpaceX())
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