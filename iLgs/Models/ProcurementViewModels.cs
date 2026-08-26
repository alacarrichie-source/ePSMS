using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Models
{
    public class ProcurementItemViewModel
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
        public int PlannedQuantity { get; set; }
        public int RequestedQuantity { get; set; }
        public int RemainingQuantity { get { return PlannedQuantity - RequestedQuantity; } }
        public decimal EstimatedUnitCost { get; set; }
        public bool IsInCart { get; set; }
    }

    public class AnnualProcurementViewModel
    {
        public int FiscalYear { get; set; }
        public Guid? Department { get; set; }
        public string Category { get; set; }
        public string SearchText { get; set; }
        public IEnumerable<SelectListItem> FiscalYears { get; set; }
        public IEnumerable<Codextn> Departments { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
        //public IEnumerable<ProcurementItemViewModel> Items { get; set; }
        public IEnumerable<PPMPItemVM> Items { get; set; }
        public int CartCount { get; set; }
    }

    public class CartItemViewModel : PPMPItemVM // ProcurementItemViewModel
    {
        public string ItemNo { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        //public decimal EstimatedAmount { get { return Quantity * (decimal)UnitCost; } }

        //public decimal EstimatedAmount {get { return Quantity * UnitCost.GetValueOrDefault(); }        
        public decimal EstimatedAmount => Quantity * (UnitCost ?? 0m);
    }

    public class CartViewModel
    {
        public IList<CartItemViewModel> Items { get; set; }
        public decimal EstimatedTotal { get { return Items.Sum(x => x.EstimatedAmount); } }
    }

    public class PurchaseRequestViewModel
    {
        [Required]
        [Display(Name = "Cash Availability")]
        public string Availability { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string AvaialbilityDesig { get; set; }

        [Required]
        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string ApprovedDesig { get; set; }

        [Required]
        public string Fund { get; set; }

        [Display(Name = "Specific")]
        public string FundSpecific { get; set; }

        [Required]
        [Display(Name = "Department")]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Required]
        public string FPP { get; set; }

        [Required]
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string RequestedDesig { get; set; }

        [Required(ErrorMessage = "Purpose is required.")]
        public string Purpose { get; set; }

        [Display(Name = "Control No.")]
        public string CtrlNo { get; set; }

        //public IList<CartItemViewModel> Items { get; set; }
        //Value cannot be null during Checkout submit on this line
        //public decimal EstimatedTotal { get { return Items.Sum(x => x.EstimatedAmount); } }

        public IList<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

        public decimal EstimatedTotal
        {
            get
            {
                return (Items ?? new List<CartItemViewModel>())
                    .Sum(x => x.EstimatedAmount);
            }
        }
    }

    public class PurchaseOrderViewModel
    {
        public PurchaseOrderViewModel()
        {
            Items = new List<PurchaseOrderItemViewModel>();
        }

        public string Id { get; set; }

        [Display(Name = "PO Number")]
        [Required(ErrorMessage = "PO Number is required.")]
        public string PoNumber { get; set; }

        [Display(Name = "PO Date")]
        [DataType(DataType.Date)]
        public DateTime? PoDate { get; set; } = DateTime.Today;

        [Display(Name = "Supplier / Contractor")]
        [Required(ErrorMessage = "Please select a Supplier.")]
        public string Supplier { get; set; }

        [Display(Name = "Supplier Address")]
        public string SupplierAddress { get; set; }

        [Display(Name = "Mode of Procurement")]
        public string ModeOfProcurement { get; set; } = "Public Bidding";

        [Display(Name = "Delivery Period")]
        public string DeliveryPeriod { get; set; } = "30 Days";

        [Display(Name = "Place of Delivery")]
        public string PlaceOfDelivery { get; set; } = "Main Agency Warehouse, Port Area, Manila";

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; } = "Check upon Delivery";

        [Display(Name = "Delivery Terms")]
        public string DeliveryTerms { get; set; } = "FOB Destination";

        public string Status { get; set; } = "Draft";
        public decimal TotalAmount { get; set; }
        public string TotalAmountInWords { get; set; }
        public int ItemCount { get; set; }
        public string SourcePrSummary { get; set; }

        public IList<PurchaseOrderItemViewModel> Items { get; set; }
    }

    public class PurchaseOrderItemViewModel
    {
        public string Id { get; set; }
        public string PurchaseOrderId { get; set; }

        [Display(Name = "Item No.")]
        public int ItemNo { get; set; }

        [Required(ErrorMessage = "Catalog Code is required.")]
        [Display(Name = "Catalog Code")]
        public string CatalogCode { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description & Specifications")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Unit of measure is required.")]
        [Display(Name = "Unit")]
        public string Unit { get; set; } = "Piece";

        [Required(ErrorMessage = "Quantity must be greater than zero.")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1.")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Unit Cost is required.")]
        [Range(0.01, 100000000.00, ErrorMessage = "Unit Cost must be positive.")]
        [Display(Name = "Unit Cost (PHP)")]
        public decimal UnitCost { get; set; }

        [Display(Name = "Total Cost (PHP)")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Source PR(s)")]
        public string SourcePrs { get; set; }

        [Display(Name = "Technical Specifications")]
        public string TechnicalSpecs { get; set; }
    }

    public class ConsolidatePrWizardViewModel
    {
        public ConsolidatePrWizardViewModel()
        {
            AvailablePrs = new List<PurchaseRequestSelectionViewModel>();
            SelectedPrIds = new List<string>();
            AssignableItems = new List<PrItemAssignmentViewModel>();
            ConsolidatedItems = new List<PurchaseOrderItemViewModel>();
        }

        public string PoNumber { get; set; }
        public DateTime PoDate { get; set; } = DateTime.Today;
        public string Supplier { get; set; }
        public string SupplierAddress { get; set; }
        public string ModeOfProcurement { get; set; } = "Public Bidding";
        public string DeliveryPeriod { get; set; } = "30 Days";
        public string PlaceOfDelivery { get; set; } = "Main Agency Warehouse, Port Area, Manila";
        public string PaymentTerms { get; set; } = "Check upon Delivery";
        public string DeliveryTerms { get; set; } = "FOB Destination";
        public string OtherTerms { get; set; } = "All items subject to final acceptance inspection by Supply Officer.";

        public IList<PurchaseRequestSelectionViewModel> AvailablePrs { get; set; }
        public IList<string> SelectedPrIds { get; set; }
        public IList<PrItemAssignmentViewModel> AssignableItems { get; set; }
        public IList<PurchaseOrderItemViewModel> ConsolidatedItems { get; set; }
    }

    public class PrItemAssignmentViewModel
    {
        public string Id { get; set; }
        public string PrNumber { get; set; }
        public string CatalogCode { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public int RequestedQty { get; set; }
        public int AssignedQty { get; set; }
        public int RemainingQty { get; set; }
        public string PoGroup { get; set; } = "PO Group 1 (Main)";
        public decimal UnitCost { get; set; }
        public string TechnicalSpecs { get; set; }
    }

    public class PurchaseRequestSelectionViewModel
    {
        public string Id { get; set; }
        public string PrNumber { get; set; }
        public DateTime? PrDate { get; set; }
        public string Department { get; set; }
        public string Purpose { get; set; }
        public string FundCluster { get; set; } = "01 - Regular Agency Fund";
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsSelected { get; set; }
    }
}