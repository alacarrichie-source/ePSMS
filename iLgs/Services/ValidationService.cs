using iLgs.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Services
{
    public interface IValidationService
    {
        //List<string> ValidateRequiredFields<T>(T entity);
        void ValidateEntity<T>(T entity);
    }

    public class ValidationService : IValidationService
    {
        private List<string> ValidateRequiredFields<T>(T entity)
        {
            var missingFields = new List<string>();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                var requiredAttribute = property.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

                if (requiredAttribute != null)
                {
                    var value = property.GetValue(entity);
                    if (value == null || (property.PropertyType == typeof(string) && string.IsNullOrEmpty((string)value)))
                    {
                        missingFields.Add(property.Name);
                    }
                }
            }

            return missingFields;
        }

        public void ValidateEntity<T>(T entity)
        {
            var missingFields = ValidateRequiredFields(entity);

            if (missingFields.Any())
            {
                var msg = "The following required fields are missing:\n";                
                foreach (var field in missingFields)
                {
                    msg += "\n" + field;                    
                }
                throw new InvalidValueException(msg);
            }            
        }
    }
}