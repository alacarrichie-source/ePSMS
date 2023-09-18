using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class InvalidValueException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Conflict;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public string Key { get; set; }

        public InvalidValueException(string message)
            : base(message: message) { }

        public InvalidValueException(string key, string message)
            : base(message: message) { this.Key = key; }
    }
}