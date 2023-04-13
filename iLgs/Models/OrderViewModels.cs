using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class OrderVM
    {
        //public string PoYear { get { return PoNo.Substring(0, 4); } set { PoYear = value; } }
        //public string PoMonth { get { return PoNo.Substring(5, 2); } set { PoMonth = value; } }
        //[Display(Name = "Series")]
        //public string PoSeries { get { return PoNo.Substring(8, 4); } set { PoSeries = value; } }

        public string PoYear { get; set; }
        public string PoMonth { get; set; }
        public string PoSeries { get; set; }

        public string PoNo_ { get { return PoYear + "-" + PoMonth + "-" + PoSeries; } }

        [Display(Name = "Supplier")]
        public string SupplierName { get; set; }

        [Display(Name = "Address")]
        public string SupplierAddress { get; set; }

        [Display(Name = "TIN")]
        public string SupplierTin { get; set; }

        [Display(Name = "Mode of Procurement")]
        public string PoModeDesc { get; set; }

        public bool IsIssued { get; set; }
        public DataSourceRequest Request { get; set; }

        public System.Guid Id { get; set; }

        [Display(Name = "Supplier")]
        [Required]
        public Nullable<System.Guid> SupplierId { get; set; }

        [Display(Name = "P.O. No.")]
        public string PoNo { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Mode of Procurement")]
        [Required]
        public string PoMode { get; set; }

        [Display(Name = "P.R. No./s")]
        public string PrNos { get; set; }

        [Display(Name = "Place of Delivery")]
        [Required]
        public string DeliveryPlace { get; set; }

        [Display(Name = "Date of Delivery")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DeliveryDate { get; set; }

        [Display(Name = "Delivery Term")]
        [Required]
        public string TermDelivery { get; set; }

        [Display(Name = "Payment Term")]
        [Required]
        public string TermPayment { get; set; }

        [Display(Name = "Signed by Supplier")]
        public string SignedBySuppName { get; set; }

        [Display(Name = "Date Signed")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public Nullable<System.DateTime> SignedBySuppDate { get; set; }

        [Display(Name = "Authorized Official")]
        public string SignedByAuthName { get; set; }

        [Display(Name = "Designation")]
        public string SignedByAuthDesignation { get; set; }

        [Display(Name = "Resolution No.")]
        public string ResoNo { get; set; }

        [Display(Name = "Certified Correct")]
        [Required]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Date Certified")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public Nullable<System.DateTime> CertifiedCorrectDate { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // TRANSIENTS

        public Nullable<decimal> QtyTotal { get; set; }
        public Nullable<decimal> QtyIssued { get; set; }
        public Nullable<decimal> QtyRemaining { get; set; }
        public Nullable<decimal> TotalAmount { get; set; }

    }

    public class OrderItemVM
    {
        [Display(Name = "Stock/Property No.")]
        public string PsNo { get; set; }

        [Display(Name = "Unit")]
        public string PsUnit { get; set; }

        [Display(Name = "Item")]
        public string PsItem { get; set; }

        public string Description { get; set; }

        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderId { get; set; }

        [Display(Name = "Stock/Property No.")]
        [Required]
        public Nullable<System.Guid> PsCodeId { get; set; }

        [Required]
        public Nullable<decimal> Qty { get; set; }

        [Required]
        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Required]
        public Nullable<decimal> Amount { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // TRANSIENTS

        public Nullable<decimal> QtyIssued { get; set; }
        public Nullable<decimal> QtyRemaining { get; set; }
    }
}