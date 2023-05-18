using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class FailedServiceException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.ServiceUnavailable;
        }

        public string ErrorMessage()
        {
            return Message;
        }
    
        public FailedServiceException(Exception innerException)
            : base(" Failed service error occured, contact support.",
                  innerException)
        { }
    }
}