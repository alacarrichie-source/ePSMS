using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class Access
    {
        public bool AllowAdd { get; set; }
        public bool AllowEdit { get; set; }
        public bool AllowDelete { get; set; }
        public bool AllowPost { get; set; }
        public bool AllowUnpost { get; set; }
        public bool IsAdmin { get; set; }
    }
}