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

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? PoDate { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Item Count")]
        public int? ItemCount { get; set; }
    }
}