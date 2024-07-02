using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class RequiredFieldException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Forbidden;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public RequiredFieldException(string fieldName, string key)
            : base(message: string.Format("Field {0} is required for {1}.", fieldName, key)) { }

        public RequiredFieldException(string fieldName)
            : base(message: string.Format("Field {0} is required.", fieldName)) { }
    }
}