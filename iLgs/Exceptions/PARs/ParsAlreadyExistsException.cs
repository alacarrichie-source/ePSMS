using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions.PARs
{    
    public class ParsAlreadyExistsException : Exception, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Conflict;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public ParsAlreadyExistsException()
        : base(message: "PARs already exists for this Order Item.") { }
        
    }
}