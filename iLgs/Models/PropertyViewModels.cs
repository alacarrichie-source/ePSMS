using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PropertyVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsId { get; set; }

        [Display(Name = "Property No.")]
        public string StockNo { get; set; }

        [Display(Name = "PPE")]
        public string StockName { get; set; }

        public string Description { get; set; }
        public string Brand { get; set; }
        public string Fund { get; set; }

        [Display(Name = "Unit")]
        public string UnitMeas { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class PropertyItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsStockId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; } // with relationship to ParItems also

        public string Office { get; set; }
        public string Officer { get; set; }

        [Display(Name = "PAR No.")]
        public string RefNo { get; set; }

        [Display(Name = "Date")]
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
        public string Remarks { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        
        public Nullable<decimal> Amount { get; set; }

        [Display(Name = "Unit Meas")]
        public string UnitMeas { get; set; }

        // transient

        public string StockNo { get; set; } // for reference of clientDetailTemmplate
    }
}