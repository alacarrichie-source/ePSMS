using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class PoNumberAlreadyPostedException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Forbidden;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public PoNumberAlreadyPostedException(string poNo)
            : base(message: string.Format("PO Number {0} already posted. Cannot repost.", poNo)) { }
    }
}