using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;

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

        //public static string GetDisplayName(Type modelType, string propertyName)
        //{
        //    // Get the property
        //    var property = modelType.GetProperty(propertyName);
        //    if (property == null) return propertyName;

        //    // Check if the Display attribute is applied
        //    var displayAttribute = property.GetCustomAttributes(typeof(DisplayAttribute), true)
        //                                   .Cast<DisplayAttribute>()
        //                                   .FirstOrDefault();

        //    // Return the display name if available, otherwise the property name
        //    return displayAttribute?.Name ?? propertyName;
        //}        

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
    }
}