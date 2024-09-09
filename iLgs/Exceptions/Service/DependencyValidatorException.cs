using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class DependencyValidationException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.ExpectationFailed;
        }

        public string ErrorMessage()
        {
            return Message;
        }
    
        public DependencyValidationException(Exception innerException)
            : base(message: "Dependency validation error occurred, please try again.", innerException: innerException)
        { }
    }
}