using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Utilities
{
    public static class Utility
    {
        public static string ToProperCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Use InvariantCulture to ensure consistent results regardless of the current culture
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

            // Convert the input to lowercase and then to title case
            return textInfo.ToTitleCase(input.ToLower());
        }
        
        public static string GetDisplayName(Type modelType, string propertyName)
        {
            // Get the property info from the main model type
            var propertyInfo = modelType.GetProperty(propertyName);
            if (propertyInfo == null) return propertyName; // Return the original name if not found

            // Check for MetadataType attribute
            var metadataTypeAttribute = modelType.GetCustomAttributes(typeof(MetadataTypeAttribute), true)
                                                   .FirstOrDefault() as MetadataTypeAttribute;

            if (metadataTypeAttribute != null)
            {
                // Get the metadata type
                var metadataType = metadataTypeAttribute.MetadataClassType;
                var metadataProperty = metadataType.GetProperty(propertyName);
                if (metadataProperty != null)
                {
                    // Retrieve the Display attribute from the metadata property
                    var displayAttribute = metadataProperty.GetCustomAttributes(typeof(DisplayAttribute), true)
                                                           .FirstOrDefault() as DisplayAttribute;
                    return displayAttribute?.Name ?? propertyName; // Return display name or original name
                }
            }

            // Check for Display attribute directly on the model property
            var displayOnModelProperty = propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), true)
                                                      .FirstOrDefault() as DisplayAttribute;
            return displayOnModelProperty?.Name ?? propertyName; // Return display name or original name
        }

        public static string GetDisplayName<T>(string propertyName)
        {
            return GetDisplayName(typeof(T), propertyName);
            //// First, check the derived model type
            //var displayName = GetDisplayNameFromType(typeof(T), propertyName);

            //if (!string.IsNullOrEmpty(displayName))
            //{
            //    return displayName;
            //}

            //// If not found, check the base type recursively
            //var baseType = typeof(T).BaseType;
            //while (baseType != null)
            //{
            //    displayName = GetDisplayNameFromType(baseType, propertyName);
            //    if (!string.IsNullOrEmpty(displayName))
            //    {
            //        return displayName;
            //    }

            //    baseType = baseType.BaseType;
            //}

            //// Check for MetadataType attribute if defined on the derived class
            //var metadataType = typeof(T).GetCustomAttributes(typeof(MetadataTypeAttribute), true)
            //                            .FirstOrDefault() as MetadataTypeAttribute;

            //if (metadataType != null)
            //{
            //    displayName = GetDisplayNameFromType(metadataType.MetadataClassType, propertyName);
            //}

            //return displayName ?? propertyName; // Return property name if no display name found
        }

        private static string GetDisplayNameFromType(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            if (property != null)
            {
                // Check for DisplayNameAttribute in the property
                var displayNameAttr = property.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                                              .FirstOrDefault() as DisplayNameAttribute;

                if (displayNameAttr != null)
                {
                    return displayNameAttr.DisplayName;
                }

                // Check for DisplayAttribute in the property
                var displayAttr = property.GetCustomAttributes(typeof(DisplayAttribute), true)
                                          .FirstOrDefault() as DisplayAttribute;

                if (displayAttr != null)
                {
                    return displayAttr.Name;
                }
            }

            return null;
        }

        public static CategoryGroup GetCategoryGroup(string itemTypeCode, string itemCode)
        {
            CategoryGroup retval = CategoryGroup.NONE;
            if (Enum.TryParse(itemTypeCode, out Category c))
            {
                if (c == CatLands())
                {
                    retval = CategoryGroup.LAND;
                }
                else if (c == CatMachineries()
                    || c == CatTransportations()
                    || c == CatFurnitures()
                    || c == CatOtherProperties()
                    || c == CatMedicals()
                    || c == CatAgriculturals()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableForms()
                    || c == CatNonAccountableForns()
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    retval = CategoryGroup.OTHERS;
                }
                else if (c == CatDrugs())
                {
                    retval = CategoryGroup.DRUGS;
                }
                else if (c == CatRepairs())
                {
                    var index = itemCode.IndexOf('-');
                    var itemNo = itemCode.Substring(index + 1);

                    retval = CategoryGroup.SERIAL;
                }
            }
            return retval;
        }
    }
}