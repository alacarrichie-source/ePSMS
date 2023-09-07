using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RISlipVM
    {        
        public System.Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public Nullable<System.Guid> OrderId { get; set; }
        public string Fund { get; set; }
        public string Division { get; set; }
        public string Office { get; set; }
        public string FPP { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Display(Name = "RIS Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RisDate { get; set; }
        public string Purpose { get; set; }

        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }

        [Display(Name = "Requested Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RequestedDate { get; set; }

        [Display(Name = "Requested By Designation")]
        public string RequestedByDesignation { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approved Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ApprovedDate { get; set; }

        [Display(Name = "Aporoved By Designation")]
        public string ApprovedByDesignation { get; set; }

        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; }

        [Display(Name = "Issued Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Display(Name = "Issued By Designation")]
        public string IssuedByDesignation { get; set; }

        [Display(Name = "Received By")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Received Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ReceivedDate { get; set; }

        [Display(Name = "Received By Designation")]
        public string ReceivedByDesignation { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }        
    }

    public class RISlipItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisId { get; set; }
        public Nullable<System.Guid> StockItemId { get; set; }
        public Nullable<decimal> ReqQty { get; set; }
        public Nullable<decimal> IssQty { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        public Nullable<decimal> Amount { get; set; }

        public string IssRemarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients

        public string StockNo { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }        

    }

    public class RIS_VM
    {        
        public System.Guid Id { get; set; }
        public string Fund { get; set; }
        public string Division { get; set; }
        public string Office { get; set; }
        public string FPP { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Display(Name = "RIS Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RisDate { get; set; }
        public string Purpose { get; set; }

        [Display(Name = "Requested by")]
        public string RequestedBy { get; set; }

        [Display(Name = "Designation")]
        public string RequestedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> RequestedDate { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Designation")]
        public string ApprovedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> ApprovedDate { get; set; }

        [Display(Name = "Issued by")]
        public string IssuedBy { get; set; }

        [Display(Name = "Designation")]
        public string IssuedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Display(Name = "Received by")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Designation")]
        public string ReceivedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> ReceivedDate { get; set; }


        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> PostedDt { get; set; }
    }

    public class RisItemVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisId { get; set; }
        [Display(Name = "Item No.")]
        public Nullable<System.Guid> ItemCodeId { get; set; }
        public string Description { get; set; }

        [Display(Name = "Qty Req.")]
        public Nullable<int> QtyRequest { get; set; }

        [Display(Name = "Qty Iss.")]
        public Nullable<int> QtyIssue { get; set; }

        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        
        [Display(Name = "Stock/Property No.")]
        public string PsNo { get; set; } // Generic (Without Brand)        

        public string PsNoDisplay { get; set; } // For printing 

        public string Unit { get; set; }
        
        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        // Transients
        [Display(Name = "Category")]
        public string PsType { get; set; } // Used as category

        public string ItemCode { get; set; } // Code of ItemCodeId

        [Display(Name = "Item Type")]
        public string ItemType { get; set; } // Description of ItemCodeId

        public string GridRisItemExtns { get; set; }
    }

    public class RisItemExtnVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisItemId { get; set; }
        [Display(Name = "Field No.")]
        public string ItemNo { get; set; }
        [Display(Name = "Field Name")]
        public string ItemKey { get; set; }
        [Display(Name = "Field Value")]
        public string ItemValue { get; set; }
        public int? Sequence { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients
        public bool IsEnabled { get; set; }
    }
}