using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class PurchaseRequestNotYetPostedException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Forbidden;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public PurchaseRequestNotYetPostedException(string prNo)
            : base(message: string.Format("Purchase request number {0} is not yet posted.", prNo)) { }
    }
}