using iLgs.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;

namespace iLgs.Exceptions.Service
{
    public class InvalidModelException : Xeption
    {
        public InvalidModelException()
        : base(message: "Invalid model error occurred, please fix the errors and try again.")
        { }

        public InvalidModelException(Exception innerException)
        : base(message: "Invalid model error occurred, please fix the errors and try again.",
            innerException: innerException,
            data: innerException.Data)
        { }

        public InvalidModelException(DbEntityValidationException exception)
        : base(
            message: "Invalid input, contact support.",
            innerException: exception.InnerException,
            data: null) // Data is handled via AddData
        {
            AddEntityValidationErrors(exception.EntityValidationErrors);
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

        public override string Message
        {
            get
            {
                if (Data.Count > 0)
                {
                    return JsonConvert.SerializeObject(GetFormattedErrorsAsJson());
                }

                return base.Message;
            }
        }

        
        // Ensure this returns a properly formatted JSON string
        private IEnumerable<object> GetFormattedErrorsAsJson()
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
                        field = fieldName,
                        messages = fieldErrors
                    });
                }
            }

            return errorMessages; // Return a list of error objects
        }

        public Dictionary<string, List<string>> GetFormattedErrorsAsDictionary()
        {
            var errors = new Dictionary<string, List<string>>();

            foreach (DictionaryEntry error in Data)
            {
                string fieldName = error.Key.ToString();
                List<string> fieldErrors = error.Value as List<string>;

                if (fieldErrors != null)
                {
                    errors.Add(fieldName, fieldErrors);
                }
            }

            return errors;
        }
    }
}