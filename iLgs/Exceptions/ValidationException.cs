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

        public ValidationException(Xeption exception)
            : base(message: "Invalid input, contact support.", innerException: exception,
            data: exception.Data)
        { }

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
                        field = fieldName,
                        messages = fieldErrors
                    });
                }
            }

            return errorMessages; // Return the list of error objects
        }
    }
}