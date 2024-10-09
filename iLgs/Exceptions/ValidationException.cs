using System;
using System.Collections;
using System.Collections.Generic;
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
    }
}