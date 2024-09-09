using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions.Service
{
    public class LockedException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Forbidden;
        }

        public string ErrorMessage()
        {
            return Message;
        }
    
        public LockedException(Exception innerException)
            : base(message: "Locked record exception, please try again later.", innerException: innerException) { }
    }
}