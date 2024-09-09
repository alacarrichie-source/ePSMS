using iLgs.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Validators
{
    public class BaseValidator
    {
        protected static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        protected static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        protected static dynamic IsInvalid(DateTimeOffset date) => new
        {
            Condition = date == default,
            Message = "Date is required"
        };

        protected static dynamic IsNotSame(
            Guid firstId,
            Guid secondId,
            string secondIdName) => new
            {
                Condition = firstId != secondId,
                Message = $"Id is not the same as {secondIdName}"
            };

        protected static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };


        //public static bool IsInvalid(string input) => new String.IsNullOrWhiteSpace(input);
        // public static bool IsInvalid(Guid input) => input == default;

        protected static void Validate(params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidValueException = new InvalidValueException();

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidValueException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidValueException.ThrowIfContainsErrors();
        }
    }
}