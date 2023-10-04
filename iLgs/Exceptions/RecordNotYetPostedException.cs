using System;
using System.Net;

namespace iLgs.Exceptions
{
    public class RecordNotYetPostedException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Forbidden;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public RecordNotYetPostedException(string message)
            : base(message: message) { }
    }
}