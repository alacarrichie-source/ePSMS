using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RPCI_VM
    {        
        public System.Guid Id { get; set; }

        [Display(Name = "Item Type")]
        public string ItemType { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]

        [Display(Name = "As At")]
        public Nullable<System.DateTime> AsAt { get; set; }
        public string Fund { get; set; }

        [Display(Name = "Accountable Officer")]
        public string AccountableOfficer { get; set; }
        public string Designation { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AssumptionDt { get; set; }

        [Display(Name = "Certified correct by")]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Verrified by")]
        public string VerifiedBy { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedDt { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }      
        
        // Transients
    }

    public class RPCIItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RpciId { get; set; }
        public string Article { get; set; }
        public string Description { get; set; }

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }

        public string Unit { get; set; }

        [Display(Name = "Unit Value")]
        public Nullable<decimal> UnitValue { get; set; }

        [Display(Name = "Qty Balance")]
        public Nullable<int> QtyBalance { get; set; }

        [Display(Name = "Qty On hand")]
        public Nullable<int> QtyOnHand { get; set; }

        [Display(Name = "Qty Short/Over")]
        public Nullable<int> QtyShortOver { get; set; }

        [Display(Name = "Value Short/Over")]
        public Nullable<decimal> ValueShortOver { get; set; }

        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }
}