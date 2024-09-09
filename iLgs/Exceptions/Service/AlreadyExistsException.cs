using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions.Service
{
    public class AlreadyExistsException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Found;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public AlreadyExistsException(Exception innerException)
            : base(message: "Record with the same id already exists.", innerException: innerException) { }
    }
}