using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class DependencyException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.ServiceUnavailable;
        }

        public string ErrorMessage()
        {
            return Message;
        }
    
        public DependencyException(Exception innerException)
            : base("Service dependency error occurred, contact support.", innerException) { }
    }
}