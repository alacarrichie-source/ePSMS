using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class ErrorMessage
    {
        public string Key { get; set; }
        public string ErrorMsg { get; set; }
        public List<ErrorMessage> ErrorMessages { get; set; }
    }

    public class GuidIdVM
    {
        public Guid Id { get; set; }
    }
}