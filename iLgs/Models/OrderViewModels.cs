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
        public DataSourceRequest request { get; set; }

        public System.Guid Id { get; set; }
        public Nullable<System.Guid> SupplierId { get; set; }

        [Display(Name = "P.O. No.")]
        public string PoNo { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Mode of Procurement")]
        public string PoMode { get; set; }

        [Display(Name = "P.R. No./s")]
        public string PrNos { get; set; }

        [Display(Name = "Place of Delivery")]
        public string DeliveryPlace { get; set; }

        [Display(Name = "Date of Delivery")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DeliveryDate { get; set; }

        [Display(Name = "Delivery Term")]
        public string TermDelivery { get; set; }

        [Display(Name = "Payment Term")]
        public string TermPayment { get; set; }

        [Display(Name = "Signed by Supplier")]
        public string SignedBySuppName { get; set; }

        [Display(Name = "Date Signed")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> SignedBySuppDate { get; set; }

        [Display(Name = "Authorized Official")]
        public string SignedByAuthName { get; set; }

        [Display(Name = "Designation")]
        public string SignedByAuthDesignation { get; set; }

        [Display(Name = "Resolution No.")]
        public string ResoNo { get; set; }

        [Display(Name = "Certified Correct")]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Date Certified")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> CertifiedCorrectDate { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public partial class OrderItemVM
    {
        [Display(Name = "Stock/Property No.")]
        public string PsNo { get; set; }

        [Display(Name = "Unit")]
        public string PsUnit { get; set; }

        [Display(Name = "Description")]
        public string PsDescription { get; set; }

        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderId { get; set; }
        public Nullable<System.Guid> PsCodeId { get; set; }
        public Nullable<decimal> Qty { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }
}