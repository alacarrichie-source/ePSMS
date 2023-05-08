using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PsStockVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsId { get; set; }

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }

        public string Description { get; set; }
        public string Type { get; set; }

        [Display(Name = "Id No.")]
        public string IdNo { get; set; }

        public string Location { get; set; }
        public Nullable<decimal> Area { get; set; }

        [Display(Name = "TCT No.")]
        public string TctNo { get; set; }

        [Display(Name = "Brand Name")]
        public string BrandName { get; set; }

        public string Department { get; set; }

        [Display(Name = "Other Specs")]
        public string OtherSpecs { get; set; }

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }

        public string Color { get; set; }

        [Display(Name = "Engine No.")]
        public string EngineNo { get; set; }

        [Display(Name = "Chassis No.")]
        public string ChassisNo { get; set; }

        [Display(Name = "PAR No.")]
        public string ParNo { get; set; }

        [Display(Name = "Accountable Officer")]
        public string AccountableOfficer { get; set; }

        [Display(Name = "Completion Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> CompletionDate { get; set; }

        [Display(Name = "Estimated Life")]
        public Nullable<decimal> EstimatedLife { get; set; }

        public Nullable<decimal> Amount { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }                
    }

    public class PsItemVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsStockId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Display(Name = "Ref No.")]
        public string RefNo { get; set; }

        [Display(Name = "Ref Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RefDate { get; set; }
        public string RefType { get; set; }

        [Display(Name = "Receipt Qty")]
        public Nullable<decimal> Qty { get; set; }

        [Display(Name = "Issued Qty")]
        public Nullable<decimal> QtyIss { get; set; }

        [Display(Name = "Balance")]
        public Nullable<decimal> QtyBal { get; set; }

        [Display(Name = "No. of Days to Consume")]
        public Nullable<int> Days { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }
}