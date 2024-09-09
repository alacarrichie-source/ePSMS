using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class NullException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.NotFound;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public NullException()
            : base(message: "Object is null") { }

        public NullException(string message)
            : base(message: message) { }

    }
}