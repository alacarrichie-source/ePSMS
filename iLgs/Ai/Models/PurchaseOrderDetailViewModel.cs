using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Ai.Models
{    
    public class PurchaseOrderDetailViewModel
    {
        public PurchaseOrderDetailViewModel()
        {
            LineItems = new List<POLineItemDetailViewModel>();
            Documents = new List<PODocumentViewModel>();
            SourcePRNumbers = new List<string>();
        }

        public Guid Id { get; set; }
        public string PONumber { get; set; }
        public DateTime? PODate { get; set; }
        public Guid? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierTIN { get; set; }
        public string SupplierAddress { get; set; }
        public string SupplierContactPerson { get; set; }
        public string SupplierContactNumber { get; set; }
        public string DeliveryPeriodDays { get; set; }
        public string PlaceOfDelivery { get; set; }
        public string PaymentTerms { get; set; }
        public string ModeOfProcurement { get; set; }
        public string BACResolutionNo { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string PostedBy { get; set; }
        public DateTime? PostedAt { get; set; }

        public List<string> SourcePRNumbers { get; set; }
        public List<POLineItemDetailViewModel> LineItems { get; set; }
        public List<PODocumentViewModel> Documents { get; set; }
    }

    public class POLineItemDetailViewModel
    {
        public POLineItemDetailViewModel()
        {
            SetLotItems = new List<POSetLotItemViewModel>();
            AllFields = new List<POAllFieldValueViewModel>();
        }

        public Guid Id { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string ItemCode { get; set; }
        public string StockNo { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
        public string GSOCategory { get; set; }
        public string TechnicalDescription { get; set; }
        public List<POAllFieldValueViewModel> AllFields { get; set; }
        public List<POSetLotItemViewModel> SetLotItems { get; set; }
    }

    public class POAllFieldValueViewModel
    {
        public string FieldName { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public class POSetLotItemViewModel
    {
        public Guid? RequestSubItemId { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Unit { get; set; }
        public int Qty { get; set; }
        public decimal EstimatedCost { get; set; }
    }
}
