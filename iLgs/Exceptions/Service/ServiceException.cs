using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class ServiceException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.ServiceUnavailable;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public ServiceException(Exception innerException)
            : base("Service error occurred, contact support.", innerException) { }
    }
}