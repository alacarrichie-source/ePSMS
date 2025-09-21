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

    public class BreadCrumbItemVM
    {
        public string Text { get; set; }
        public string Href { get; set; }
        public string Icon { get; set; }
        public bool IsRoot { get; set; }
    }
}