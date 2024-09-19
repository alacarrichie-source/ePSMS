using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class RecordRelationshipException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Conflict;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public RecordRelationshipException(string message)
            : base(message: message) { }
    }
}