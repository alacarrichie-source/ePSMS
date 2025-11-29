using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class RecordLockedException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Forbidden; // Locked not avaible
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public RecordLockedException(Xeption innerException)
            : base("Locked record exception, please try again later.", innerException) { }

        public RecordLockedException(Exception innerException)
            : base("Locked record exception, please try again later.", innerException) { }

        public RecordLockedException(string msg)
            : base(message: msg) { }
    }
}