using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class AIR_VM
    {        
        public System.Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public Nullable<System.Guid> OrderId { get; set; }
        public string Fund { get; set; }

        [Display(Name ="AIR No.")]
        public string AIRNo { get; set; }

        [Display(Name = "Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AIRDate { get; set; }

        [Display(Name = "Invoice No.")]
        //[Required]
        public string InvoiceNo { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required]
        public Nullable<System.DateTime> InvoiceDate { get; set; }

        [Display(Name = "Date Received")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcceptedDate { get; set; }

        [Display(Name = "Complete")]
        public Nullable<bool> IsComplete { get; set; }

        [Display(Name = "Partial")]
        public Nullable<bool> IsPartial { get; set; }

        public string Custodian { get; set; }

        public string Remarks { get; set; }

        [Display(Name = "Date Inspected")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InspectedDate { get; set; }

        [Display(Name = "Inspected")]
        public Nullable<bool> IsInspected { get; set; }

        [Display(Name = "Officer/Committee")]
        public string Officer { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public string PostedBy { get; set; }
        public Nullable<System.DateTime> PostedDt { get; set; }

        // Transients

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        public string Supplier { get; set; }        

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Requisitioning Office/Dept.")]
        public string Department { get; set; }

        public string Mode { get; set; }
    }

    public class AIRItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> AirId { get; set; }

        [Display(Name = "Stock/Property No.")]
        public Nullable<System.Guid> OrderItemId { get; set; }

        public Nullable<decimal> Qty { get; set; }

        [Required]
        public string Remarks { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }      
     
        // Transients

        [Display(Name = "Stock/Property No.")]
        public string PsNo { get; set; }

        [Display(Name = "Item")]
        public string PsItem { get; set; }

        [Display(Name = "Description")]
        public string OrderDescription { get; set; }

        [Display(Name = "Unit")]
        public string PsUnit { get; set; }

        public string GridOrderItemExtns { get; set; }
        public string Mode { get; set; }
        public string PsType { get; set; }

        [Display(Name = "Area Sold/Donated")]

        public Nullable<decimal> AreaSoldDonated { get; set; }

        [Display(Name = "Construction Year")]
        public Nullable<int> ConstructionYear { get; set; }
        public string InvDist { get; set; }
    }

    public class AIRInvoiceVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> AirId { get; set; }

        [Display(Name = "Invoice No.")]
        //[Required]
        public string InvoiceNo { get; set; }

        [Display(Name = "Invoice Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required]
        public Nullable<System.DateTime> InvoiceDate { get; set; }

        public Nullable<decimal> Amount { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }
}