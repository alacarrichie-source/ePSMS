using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services.Description;

namespace iLgs.Exceptions
{
    public class RecordAlreadyExistsException : Exception, IException
    {
        //public HttpStatusCode StatusCode => HttpStatusCode.Conflict;
        //public string ErrorMessage => Message;

        public HttpStatusCode StatusCode()
        {
            return HttpStatusCode.Conflict;
        }

        public string ErrorMessage()
        {
            return Message;
        }

        public RecordAlreadyExistsException()
        : base(message: "Record already exists.") { }

        public RecordAlreadyExistsException(Guid id)
            : base(message: string.Format("Record with id: {0} already exists.", id)) { }

        public RecordAlreadyExistsException(string message)
            : base(message: message) { }
    }
}