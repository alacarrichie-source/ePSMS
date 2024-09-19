using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class RecordAlreadyPostedException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Forbidden;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public string Key { get; set; }

        public RecordAlreadyPostedException()
            : base(message: string.Format("Record already posted.")) { }

        public RecordAlreadyPostedException(string message)
            : base(message: message) { }        
        
        public RecordAlreadyPostedException(string key, string message)
            : base(message: message) { this.Key = key; }
    }
}