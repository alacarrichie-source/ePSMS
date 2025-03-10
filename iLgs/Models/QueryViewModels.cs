using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class QueryOrderItemsVM
    {
        public Guid Id { get; set; }
        public Guid PsStockId { get; set; }

        [Display(Name = "Item No.")]
        public string PsNo { get; set; }

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? PoDate { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "Qty")]
        public decimal? Qty { get; set; }

        public string SelectedIds { get; set; }
    }

    public class QueryPoVM
    {
        public Guid Id { get; set; }

        public string Fund { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? PoDate { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Dept. Code")]
        public string DeptCode { get; set; }

        [Display(Name = "Dept. Display")]
        public string Department { get; set; }

        [Display(Name = "Item Count")]
        public int? ItemCount { get; set; }

        [Display(Name = "PO Amount")]
        public Nullable<decimal> Amount { get; set; }

        [Display(Name = "Issued Amount")]
        public Nullable<decimal> IssueAmount { get; set; }

        [Display(Name = "Balance Amount")]
        public Nullable<decimal> BalanceAmount { get; set; }

        [Display(Name = "Past Qty")]
        public int? PastQty { get; set; }

        [Display(Name = "Past Amount")]
        public Nullable<decimal> PastAmount { get; set; }

        [Display(Name = "Current Qty")]
        public int? CurQty { get; set; }

        [Display(Name = "Current Amount")]
        public Nullable<decimal> CurAmount { get; set; }

        [Display(Name = "Future Qty")]
        public int? FutureQty { get; set; }

        [Display(Name = "Future Amount")]
        public Nullable<decimal> FutureAmount { get; set; }
    }
}