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

        [Display(Name = "P.R. No.")]
        public Nullable<System.Guid> PrId { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Mode of Procurement")]
        [Required]
        public string PoMode { get; set; }


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

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> PostedDt { get; set; }

        // TRANSIENTS


        [Display(Name = "PR. No.")]
        public string PrNo { get; set; }
        public DateTime? PrDate { get; set; }

        public Nullable<decimal> QtyTotal { get; set; }
        public Nullable<decimal> QtyIssued { get; set; }
        public Nullable<decimal> QtyRemaining { get; set; }
        public Nullable<decimal> TotalAmount { get; set; }
        public bool IsLocked { get; set; }

    }

    public class OrderItemVM
    {
        [Display(Name = "Stock/Property No.")]
        public string PsNo { get; set; }

        [Display(Name = "Unit")]
        public string PsUnit { get; set; }

        [Display(Name = "Item")]
        public string PsItem { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [Display(Name = "Brand")]
        public string BrandName { get; set; }

        [Display(Name = "Other Specs")]
        public string OtherSpecs { get; set; }

        [Display(Name = "Estimated Life")]
        public Nullable<decimal> EstimatedLife { get; set; }

        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderId { get; set; }

        [Display(Name = "Stock/Property No.")]
        [Required]
        public Nullable<System.Guid> RequestItemId { get; set; }

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
        public Nullable<System.Guid> PsCodeId { get; set; }
        public Nullable<decimal> QtyIssued { get; set; }
        public Nullable<decimal> QtyRemaining { get; set; }
        public string GridOrderItemExtns { get; set; }
    }

    public class OrderItemGroupVM
    {
        public Guid? PsCodeId { get; set; }
        public string Description { get; set; }        
    }

    public class OrderItemExtnVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Display(Name = "Code")]
        public string ItemCode { get; set; }

        [Display(Name = "Field")]
        public string ItemKey { get; set; }

        [Display(Name = "Value")]
        public string ItemValue { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }
}