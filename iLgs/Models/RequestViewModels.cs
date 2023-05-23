using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RequestVM
    {        
        public System.Guid Id { get; set; }

        [Required]
        public string Fund { get; set; }

        [Required]
        public string Department { get; set; }
        public string Section { get; set; }
        
        [Display(Name = "P.R. No.")]
        public string PrNo { get; set; }

        [Required]
        [Display(Name = "P.R. Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PrDate { get; set; }
        public string FPP { get; set; }
        public string Purpose { get; set; }

        [Required]
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }

        [Display(Name = "Designation")]
        public string RequestedDesig { get; set; }

        [Display(Name = "Cash Availability")]
        public string Availability { get; set; }

        [Display(Name = "Designation")]
        public string AvaialbilityDesig { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Designation")]
        public string ApprovedDesig { get; set; }

        [Display(Name = "Posted By")]
        public string SubmittedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> SubmittedDt { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }       

        public bool IsWithPO { get; set; }
    }

    public class RequestItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PrId { get; set; }

        [Display(Name = "Item No.")]
        public Nullable<System.Guid> PsCodeId { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [Display(Name = "Brand")]
        public string BrandName { get; set; }

        [Display(Name = "Other Specs")]
        public string OtherSpecs { get; set; }

        [Required]
        public Nullable<decimal> Qty { get; set; }

        [Required]
        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Required]
        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        

        // Transients

        [Display(Name = "Item No.")]
        public string PsCode { get; set; }

        [Display(Name = "Unit")]
        public string PsUnit { get; set; }

        [Display(Name = "Item")]
        public string PsItem { get; set; }
    }
}