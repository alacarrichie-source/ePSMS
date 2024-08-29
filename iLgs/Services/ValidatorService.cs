using iLgs.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Services
{
    public static class ValidatorService
    {
        public static string ValidateModel<T>(T model) 
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);

            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            if (!isValid)
            {
                var errors = new Dictionary<string, string>();
                foreach (var validationResult in validationResults)
                {
                    foreach (var memberName in validationResult.MemberNames)
                    {
                        errors.Add(memberName, validationResult.ErrorMessage);
                    }
                }

                //return ServiceResult<MyModel>.Failure(errors, HttpStatusCode.BadRequest);
                string allErrors = string.Join("\n", errors.Select(e => $"{e.Key}: {e.Value}"));
                throw new InvalidValueException(allErrors);
            }
            return "";
        }
    }
}