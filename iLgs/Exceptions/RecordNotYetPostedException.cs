using System;
using System.Net;

namespace iLgs.Exceptions
{
    public class RecordNotYetPostedException : Xeption, IException
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

        public RecordNotYetPostedException()
            : base(message: string.Format("Record not yet posted.")) { }

        public RecordNotYetPostedException(string message)
            : base(message: message) { }

        public RecordNotYetPostedException(string key, string message)
            : base(message: message) { this.Key = key; }
    }
}