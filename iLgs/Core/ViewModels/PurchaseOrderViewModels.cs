using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLgs.Core.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public string Id { get; set; }

        [Display(Name = "PO Number")]
        public string PONumber { get; set; }

        [Display(Name = "PO Date"), DataType(DataType.Date)]
        public DateTime? PODate { get; set; }

        [Required, Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }
        public string SupplierAddress { get; set; }
        public string SupplierTin { get; set; }

        public List<string> SourcePRs { get; set; } = new List<string>();
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // DRAFT, POSTED, CANCELLED
        public string ModeOfProcurement { get; set; }
        public string PlaceOfDelivery { get; set; }
        public string DeliveryPeriodDays { get; set; }
        public string PaymentTerms { get; set; }
        public string FundCluster { get; set; }
        public string OrsBursNo { get; set; }
        public string BacResolutionNo { get; set; }
        public string CreatedBy { get; set; }
        public bool IsLiveUpdated { get; set; }

        public List<PurchaseOrderItemViewModel> Items { get; set; } = new List<PurchaseOrderItemViewModel>();
    }

    public class PurchaseOrderItemViewModel
    {
        public string Id { get; set; }
        public string PurchaseOrderId { get; set; }
        public string PrItemId { get; set; }
        public string SourcePrNo { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string TechDescription { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class PurchaseRequestViewModel
    {
        public string Id { get; set; }
        public string PRNumber { get; set; }
        public DateTime? PRDate { get; set; }
        public string Department { get; set; }
        public string ModeOfProcurement { get; set; }
        public string Status { get; set; } // APPROVED, PO_GENERATED, CANCELLED
        public int ItemCount { get; set; }
        public decimal TotalEstimatedAmount { get; set; }
        public List<PurchaseRequestItemViewModel> Items { get; set; } = new List<PurchaseRequestItemViewModel>();
    }

    public class PurchaseRequestItemViewModel
    {
        public string Id { get; set; }
        public string PurchaseRequestId { get; set; }
        public string PRNumber { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string TechDescription { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string SuggestedSupplierId { get; set; }
        public string Status { get; set; } // PENDING, ALLOCATED, ORDERED
    }

    public class SupplierViewModel
    {
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Address { get; set; }
        public string TIN { get; set; }
        public bool IsActive { get; set; } = true;
        public int ActiveOrdersCount { get; set; }
    }

    public class CreatePOWizardInputModel
    {
        [Required]
        public List<string> SelectedPRIds { get; set; } = new List<string>();

        [Required]
        public List<POGroupInputModel> POGroups { get; set; } = new List<POGroupInputModel>();
    }

    public class POGroupInputModel
    {
        public string SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string PlaceOfDelivery { get; set; }
        public string DeliveryPeriodDays { get; set; }
        public string PaymentTerms { get; set; }
        public string FundCluster { get; set; }
        public string OrsBursNo { get; set; }
        public string BacResolutionNo { get; set; }
        public List<AllocatedItemInputModel> AssignedItems { get; set; } = new List<AllocatedItemInputModel>();
    }

    public class AllocatedItemInputModel
    {
        public string PrItemId { get; set; }
        public string PrNumber { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public int AssignQty { get; set; }
        public decimal UnitCost { get; set; }
    }
}