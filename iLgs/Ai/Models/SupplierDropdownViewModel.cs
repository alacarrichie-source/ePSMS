using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Ai.Models
{ 
    public class SupplierDropdownViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TIN { get; set; }
        public string DefaultPaymentTerms { get; set; }
    }
}