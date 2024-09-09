using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions.Service
{
    public class DuplicateKeyException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Found;
        }

        public string ErrorMessage()
        {
            return Message;
        }
   
        public DuplicateKeyException(string message)
            : base(message)
        {
        }
    }
}