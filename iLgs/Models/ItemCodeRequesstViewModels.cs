using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class ItemCodeRequestVM
    {
        public System.Guid Id { get; set; }

        [Required]
        [Display(Name = "Requesting Department")]
        public Nullable<System.Guid> DepartmentId { get; set; }

        [Required]
        public string Description { get; set; }
        public string Remarks { get; set; }

        [Required]
        [Display(Name = "Estimated Cost")]
        public Nullable<decimal> EstCost { get; set; }

        [Display(Name = "Consumable")]
        public Nullable<bool> IsConsumable { get; set; }

        [Display(Name = "For Distribution")]
        public Nullable<bool> IsForDistribution { get; set; }

        [Display(Name = "Incorporated")]
        public Nullable<bool> IsIncorporated { get; set; }
        public string Status { get; set; }

        [Display(Name = "Inserted By")]
        public string InsertedBy { get; set; }

        [Display(Name = "Inserted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [Display(Name = "Updated Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transient
        [Display(Name = "Requesting Department")]
        public string Department { get; set; }
    }
}