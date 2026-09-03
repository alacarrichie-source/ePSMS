using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Ai.Models
{    
    public class POWizardViewModel
    {
        public POWizardViewModel()
        {
            SelectedPRIds = new List<Guid>();
            POGroups = new List<POGroupDraftViewModel>();
            PRLineItems = new List<PRItemAllocationViewModel>();
        }

        public int CurrentStep { get; set; } = 1;
        public List<Guid> SelectedPRIds { get; set; }
        public List<PRItemAllocationViewModel> PRLineItems { get; set; }
        public List<POGroupDraftViewModel> POGroups { get; set; }
    }

    public class POWizardDraftListItemViewModel
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public int CurrentStep { get; set; }
        public int SelectedPRCount { get; set; }
        public string DraftName { get; set; }
    }

    public class POGroupDraftViewModel
    {
        public POGroupDraftViewModel()
        {
            Items = new List<POLineItemDraftViewModel>();
            AdditionalDocs = new List<PODocumentViewModel>();
        }

        public string GroupId { get; set; } // e.g. "grp-1"
        public string GroupName { get; set; } // e.g. "PO Group 1"
        public string PONumber { get; set; }
        public Guid? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string CtrlNo { get; set; }
        public string SupBusiness { get; set; }
        public string SupAddress { get; set; }
        public string SupTIN { get; set; }
        public string SupEmail { get; set; }
        public string SupZipCode { get; set; }
        public string SupContactNo { get; set; }
        public DateTime PODate { get; set; } = DateTime.Today;
        public string DeliveryPeriodDays { get; set; } = "Within 15 days";
        public string DeliveryDate { get; set; }
        public string PlaceOfDelivery { get; set; }
        public string TermDelivery { get; set; }
        public string PaymentTerms { get; set; } = "30 Days";
        public string ModeOfProcurement { get; set; }
        public string SignedBySuppName { get; set; }
        public DateTime? SignedBySuppDate { get; set; }
        public string SignedByAuthName { get; set; }
        public string SignedByAuthDesignation { get; set; }
        public string ResoNo { get; set; }
        public string CertifiedCorrectBy { get; set; }
        public DateTime? CertifiedCorrectDate { get; set; }

        public List<POLineItemDraftViewModel> Items { get; set; }

        // Dedicated PO Copy scan upload
        public PODocumentViewModel POCopyDoc { get; set; }

        // Additional supporting docs
        public List<PODocumentViewModel> AdditionalDocs { get; set; }
    }

    public class POLineItemDraftViewModel
    {
        public POLineItemDraftViewModel()
        {
            Allocations = new List<POItemAllocationDraftViewModel>();
            SetLotItems = new List<POSetLotItemViewModel>();
        }

        public Guid Id { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public Guid? ItemCodeId { get; set; }
        public string ItemCode { get; set; }
        public string PpmpCode { get; set; }
        public string Category { get; set; }
        public string Account { get; set; }
        public string SubAccount { get; set; }
        public string StockNo { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
        public string GSOCategory { get; set; }
        public string TechnicalDescription { get; set; }
        public Dictionary<string, string> AdditionalSpecs { get; set; } = new Dictionary<string, string>();

        //public int SourcePRItemId { get; set; }

        // PR Items contributing to this consolidated PO Item
        public List<POItemAllocationDraftViewModel> Allocations { get; set; }

        // Auto-retrieved sub-item composition
        public List<POSetLotItemViewModel> SetLotItems { get; set; }
    }

    public class POItemAllocationDraftViewModel
    {
        public Guid? RequestItemId { get; set; }
        public string PRNumber { get; set; }
        public int Quantity { get; set; }
    }

    public class PRItemAllocationViewModel
    {
        public Guid Id { get; set; }
        public string PRNumber { get; set; }
        public string Department { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public int RequestedQty { get; set; }
        public int AssignedQty { get; set; }
        public int RemainingQty { get; set; }
        public decimal UnitCost { get; set; }
        public string GSOCategory { get; set; }
        public string SuggestedSupplier { get; set; }
        public string TargetGroupId { get; set; } = "grp-1"; // Default to PO Group 1
        public List<POSetLotItemViewModel> SetLotItems { get; set; }
        public string PPMPCode { get; set; }
        public string PPMPDescription { get; set; }
    }

    public class PODocumentViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileSize { get; set; }
        public string Category { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
