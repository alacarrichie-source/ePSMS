using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class ValidationException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.BadRequest;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public ValidationException(Exception innerException)
            : base("Invalid input, contact support.", innerException) { }

        
    }
}