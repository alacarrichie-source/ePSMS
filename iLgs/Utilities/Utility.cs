using iLgs.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using static iLgs.Models.Enums;

namespace iLgs.Utilities
{
    public static class Utility
    {
        public static void ValidateReferences(DbEntityEntry entry)
        {
            var entityType = entry.Entity.GetType();

            foreach (var property in entityType.GetProperties())
            {
                if (property.PropertyType.IsGenericType &&
                    typeof(ICollection<>).IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition()))
                {
                    // This is a collection navigation property
                    var collection = entry.Collection(property.Name);
                    collection.Load(); // Explicitly load related data
                    if (collection.CurrentValue != null && ((ICollection<object>)collection.CurrentValue).Any())
                    {
                        throw new InvalidOperationException("Cannot modify the record because it has references in other tables.");
                    }
                }
                else if (!property.PropertyType.IsValueType && property.PropertyType != typeof(string))
                {
                    // This is a reference navigation property
                    var reference = entry.Reference(property.Name);
                    reference.Load(); // Explicitly load related data
                    if (reference.CurrentValue != null)
                    {
                        throw new InvalidOperationException("Cannot modify the record because it has references in other tables.");
                    }
                }
            }
        }

        public static string ExportDate(DateTime? date)
        {
            if (date.HasValue)
            {
                var d = (DateTime)date;
                string monthName = d.ToString("MMMM"); // "December"

                if (d.Month == 1 && (d.Day == 1 || d.Day == 2))
                {
                    return $"{d.Year}";
                }

                if ((d.Day == 1) || (d.Day == 2 && d.Month == 11))
                {
                    return $"{monthName}, {d.Year}";
                }

                return $"{monthName} {d.Day}, {d.Year}";
            }
            return string.Empty;
        }

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

        public static DateTime GetAsOfDate(int? forYear)
        {
            return new DateTime((int)forYear, 12, 31);
        }

        public static string GetFieldTrim(string fieldName, bool toProper = true)
        {
            var trim = string.Empty;
            var x = fieldName;

            if (toProper)
            {
                x = Utility.ToProperCase(fieldName);
            }

            x = x.Replace(" ", "");

            if (x.Length < 7)
            {
                trim = $"/{x}";
            }
            else
            {
                trim = $"/{x.Substring(0, 3) + x.Substring(x.Length - 3)}";
            }
            return trim;
        }

        public static string GetAfBrand(string brand)
        {
            return Utility.ToProperCase(brand).Replace(" ", "");
        }

        public static string GetAfModel(string model)
        {
            return Utility.ToProperCase(model).Replace(" ", "");
        }

        public static string GetItemNoIndex(decimal itemNo)
        {
            return GetItemNoIndex(itemNo.ToString());
        }

        public static string GetItemNoIndex(string itemNo)
        {
            return string.Join(".", itemNo.Split('.').Select(x => int.TryParse(x, out var n) ? n.ToString("D3") : "000"));
        }

        public static IQueryable<T> ApplyFilters<T>(IQueryable<T> query, FilterGroup filterGroup)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression combined = null;

            foreach (var filter in filterGroup.Filters)
            {
                var property = Expression.Property(parameter, filter.Field);
                //var constant = Expression.Constant(Convert.ChangeType(filter.Value, property.Type));
                var propertyType = property.Type;
                var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

                object convertedValue = null;

                if (filter.Value != null)
                {
                    if (targetType.IsAssignableFrom(filter.Value.GetType()))
                    {
                        convertedValue = filter.Value;
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(filter.Value, targetType);
                    }
                }

                var constant = Expression.Constant(convertedValue, propertyType);

                Expression condition = null;

                switch (filter.Operator)
                {
                    case "eq":
                        condition = Expression.Equal(property, constant);
                        break;

                    case "neq":
                        condition = Expression.NotEqual(property, constant);
                        break;

                    case "gt":
                        condition = Expression.GreaterThan(property, constant);
                        break;

                    case "gte":
                        condition = Expression.GreaterThanOrEqual(property, constant);
                        break;

                    case "lt":
                        condition = Expression.LessThan(property, constant);
                        break;

                    case "lte":
                        condition = Expression.LessThanOrEqual(property, constant);
                        break;
                }

                if (combined == null)
                {
                    combined = condition;
                }
                else
                {
                    combined = filterGroup.Logic == "and"
                        ? Expression.AndAlso(combined, condition)
                        : Expression.OrElse(combined, condition);
                }
            }

            if (combined == null)
            {
                return query;
            }

            var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);

            return query.Where(lambda);
        }

        //public static string PadNumber(object value)
        //{
        //    if (value == null)
        //        return string.Empty;

        //    return string.Join("-",
        //        value.ToString()
        //             .Split('.')
        //             .Select(part => int.Parse(part).ToString("D3")));
        //}
    }
}