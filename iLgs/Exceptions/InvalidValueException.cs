using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class InvalidValueException : Xeption
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Conflict;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public InvalidValueException(string message)
            : base(message: message) { }

        public InvalidValueException(string parameterName, object parameterValue)
            : base(message: $"Invalid data, " +
                  $"parameter name: {parameterName}, " +
                  $"parameter value: {parameterValue}.")
        { }

        public InvalidValueException()
            : base(message: "Invalid data. Please fix the errors and try again.") { }
    }
}