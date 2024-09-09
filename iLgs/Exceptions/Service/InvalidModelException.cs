using iLgs.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Validators
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
        

        // Override the Message property to include formatted errors
        //public override string Message
        //{
        //    get
        //    {
        //        var baseMessage = base.Message;
        //        string formattedErrors = GetFormattedErrors();

        //        if (Data.Count > 0)
        //        {
        //            return $"{baseMessage}{Environment.NewLine}{formattedErrors}";
        //        }

        //        return baseMessage;
        //    }
        //}

        //// Method to display or process the errors from the Data dictionary
        //private string GetFormattedErrors()
        //{
        //    var errorMessages = new List<string>();

        //    foreach (DictionaryEntry error in Data)
        //    {
        //        string fieldName = error.Key.ToString();
        //        List<string> fieldErrors = error.Value as List<string>;

        //        if (fieldErrors != null)
        //        {
        //            errorMessages.Add($"{fieldName}: {string.Join(", ", fieldErrors)}");
        //        }
        //    }

        //    return string.Join(Environment.NewLine, errorMessages);
        //}        

        public override string Message
        {
            get
            {
                //string formattedErrors = GetFormattedErrorsAsJson();
                if (Data.Count > 0)
                {
                    //return formattedErrors; // Returning the errors in JSON-like structure
                    return JsonConvert.SerializeObject(GetFormattedErrorsAsJson());
                }

                return base.Message;
            }
        }

        //// Format the errors as a JSON-like structure
        //private string GetFormattedErrorsAsJson()
        //{
        //    var fieldErrorsList = new List<string>();

        //    foreach (DictionaryEntry error in Data)
        //    {
        //        string fieldName = error.Key.ToString();
        //        List<string> fieldErrors = error.Value as List<string>;

        //        if (fieldErrors != null)
        //        {
        //            // Build the JSON-like structure for each field and its errors
        //            string errorMessagesJson = string.Join(", ", fieldErrors.Select(e => $"\"{e}\""));
        //            fieldErrorsList.Add($"{{ \"field\": \"{fieldName}\", \"messages\": [{errorMessagesJson}] }}");
        //        }
        //    }

        //    // Combine all fields into a JSON array format
        //    return $"[{string.Join(", ", fieldErrorsList)}]";
        //}

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