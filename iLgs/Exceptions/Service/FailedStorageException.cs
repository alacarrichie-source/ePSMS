using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions.Service
{
    public class FailedStorageException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.InternalServerError;
        }

        public string ErrorMessage()
        {
            return Message;
        }
    
        public FailedStorageException(Exception innerException)
            : base(message: "Failed post storage error occurred, contact support.", innerException: innerException)
        { }
    }
}