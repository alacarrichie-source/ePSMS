using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class SupplierVM
    {
        public System.Guid Id { get; set; }

        [Display(Name = "Supplier Code")]
        public string Code { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        public string DTI { get; set; }

        public string SEC { get; set; }

        public string BIN { get; set; }

        [Required]
        public string TIN { get; set; }

        [Display(Name = "Contact No.")]
        public string ContactNos { get; set; }
        public string Email { get; set; }

        [Display(Name = "Corporation")]
        public Nullable<bool> IsCorp { get; set; }

        [Display(Name = "Vatable")]
        public Nullable<bool> IsVat { get; set; }

        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; }
        public string InsertedBy { get; set; }

        [Display(Name = "Inserted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [Display(Name = "Updated Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }
}