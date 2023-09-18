using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions
{
    public class RecordNotFoundException : Exception, IException
    {
         public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.NotFound;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public RecordNotFoundException(Guid? id)
            : base(message: string.Format("Couldn't find record with id: {0}.", id)) { }

        public RecordNotFoundException(string message)
            : base(message: message) { }
    }
}