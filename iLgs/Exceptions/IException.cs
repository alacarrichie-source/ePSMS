using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Exceptions
{
    public interface IException
    {
        HttpStatusCode StatusCode();
        string ErrorMessage();
    }
}
