using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Ai.Models
{
    public class PurchaseOrderGridViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public string PONumber { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime? PODate { get; set; }

        [Display(Name = "Supplier")]
        public string SupplierName { get; set; }

        public Guid? SupplierId { get; set; }

        [Display(Name = "Source PR(s)")]
        public string SourcePRs { get; set; }

        [Display(Name = "Items Count")]
        public int ItemCount { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        public string CreatedBy { get; set; }
        public string DeliveryPeriodDays { get; set; }
        public string PlaceOfDelivery { get; set; }
        public string PaymentTerms { get; set; }
        public string ModeOfProcurement { get; set; }
        public string BACResolutionNo { get; set; }

        public bool HasPOCopy { get; set; }
        public int AdditionalDocsCount { get; set; }
    }
}