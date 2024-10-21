using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class ValidationException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.BadRequest;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        //public ValidationException(DbEntityValidationException exception)
        //: base(
        //    message: "Invalid input, contact support.",
        //    innerException: exception.InnerException,
        //    data: null) // Data is handled via AddData
        //{
        //    AddEntityValidationErrors(exception.EntityValidationErrors);
        //}

        public ValidationException(Exception exception)
            : base(message: "Invalid input, contact support.", innerException: exception,
            data: exception.Data)
        { }

        public ValidationException(Xeption exception)
            : base(message: "Invalid input, contact support.", innerException: exception,
            data: exception.Data)
        { }

        public IEnumerable<(string Key, string Message)> GetErrorsForModelState()
        {
            var errors = new List<(string, string)>();

            foreach (DictionaryEntry error in Data)
            {
                string fieldName = error.Key.ToString();
                List<string> fieldErrors = error.Value as List<string>;

                if (fieldErrors != null)
                {
                    foreach (var fieldError in fieldErrors)
                    {
                        errors.Add((fieldName, fieldError));
                    }
                }
            }

            return errors;
        }

        public IEnumerable<object> GetFormattedErrorsAsJson()
        {
            var errorMessages = new List<object>();

            foreach (DictionaryEntry error in Data)
            {
                string fieldName = error.Key.ToString();
                List<string> fieldErrors = error.Value as List<string>;

                if (fieldErrors != null)
                {
                    errorMessages.Add(new
                    {
                        Key = fieldName,
                        Message = fieldErrors
                    });
                }
            }

            return errorMessages; // Return the list of error objects
        }

        // Method to add validation errors using AddData
        private void AddEntityValidationErrors(IEnumerable<DbEntityValidationResult> entityValidationErrors)
        {
            foreach (var validationResult in entityValidationErrors)
            {
                foreach (var validationError in validationResult.ValidationErrors)
                {
                    // Add each validation error using AddData method
                    AddData(validationError.PropertyName, validationError.ErrorMessage);
                }
            }
        }        
    }
}