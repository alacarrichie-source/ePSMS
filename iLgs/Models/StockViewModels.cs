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

        [Display(Name = "Stock Name")]
        public string StockName { get; set; }

        public string Description { get; set; }
        public string Brand { get; set; }
        public string Fund { get; set; }

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

        public string Office { get; set; }

        [Display(Name = "PO No.")]
        public string RefNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RefDate { get; set; }
        public string RefType { get; set; }

        [Display(Name = "Order Qty")]
        public Nullable<decimal> QtyPo { get; set; }

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

        // transient
        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public string StockNo { get; set; } // for reference of clientDetailTemmplate
    }

    public class PsStockExtnVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsStockId { get; set; }
        [Display(Name = "Field Name")]
        public string ItemKey { get; set; }
        [Display(Name = "Field Value")]
        public string ItemValue { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Field No.")]
        public string ItemCode { get; set; }
    }
}