using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace iLgs.Exceptions.Service
{
    public class NotFoundException : Xeption, IException
    {
        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.NotFound;
        }

        public string ErrorMessage()
        {
            return Message;
        }
    
        public NotFoundException(Guid id)
            : base(message: $"Couldn't find record with id: {id}.") { }
    }
}