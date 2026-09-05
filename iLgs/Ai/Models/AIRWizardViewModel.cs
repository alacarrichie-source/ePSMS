using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Ai.Models
{
    public class AIRWizardViewModel
    {
        public AIRWizardViewModel()
        {
            Items = new List<AIRLineItemViewModel>();
            SupportingDocuments = new List<AIRDocumentViewModel>();
            Invoice = new AIRInvoiceViewModel();
            CurrentStep = 1;
            AirDate = DateTime.Today;
            InspectionDate = DateTime.Today;
        }

        public int CurrentStep { get; set; }
        public Guid? DraftId { get; set; }
        public string DraftNo { get; set; }
        public Guid? AirId { get; set; }
        public string AirNo { get; set; }
        public string CtrlNo { get; set; }
        public DateTime AirDate { get; set; }

        // Step 1: Selected PO information
        public Guid? OrderId { get; set; }
        public string PONumber { get; set; }
        public DateTime? PODate { get; set; }
        public string PRNumber { get; set; }
        public string DepartmentName { get; set; }
        public Guid? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string DeliveryPlace { get; set; }
        public string Purpose { get; set; }
        public decimal POTotalAmount { get; set; }
        public string Disposition { get; set; }

        // Step 2: Inspection Details
        public DateTime InspectionDate { get; set; }
        public string InspectionLocation { get; set; }
        public string InspectorName { get; set; }
        public string InspectorDesignation { get; set; }
        public string InspectionCommittee { get; set; }
        public string GeneralRemarks { get; set; }
        public List<AIRLineItemViewModel> Items { get; set; }
        public List<AIRDocumentViewModel> SupportingDocuments { get; set; }

        // Step 3: Invoice Details
        public AIRInvoiceViewModel Invoice { get; set; }

        // Step 4 / Workflow State Info
        public string RevisionComments { get; set; }
        public string ReturnedBy { get; set; }
        public DateTime? ReturnedDt { get; set; }
        public string OverallStatus { get; set; }
        public string InspectionStatus { get; set; }
        public string AcceptanceStatus { get; set; }
        public bool IsRevisionMode => !string.IsNullOrWhiteSpace(RevisionComments);
    }

    public class AIRLineItemViewModel
    {
        public AIRLineItemViewModel()
        {
            SubItems = new List<AIRSubItemViewModel>();
            Sources = new List<AIRItemSourceAllocationViewModel>();
            InventoryDetails = new List<AIRItemInventoryDetailViewModel>();
        }

        public Guid? AirItemId { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid OrderItemRequestId { get; set; }
        public string ItemNo { get; set; }
        public string PRNumber { get; set; }
        public string Department { get; set; }
        public List<AIRItemSourceAllocationViewModel> Sources { get; set; }
        public string PPMPCode { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal UnitCost { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal PreviousInspectedQty { get; set; }

        public decimal RemainingBeforeQty
        {
            get
            {
                var rem = OrderedQty - PreviousInspectedQty;
                return rem > 0 ? rem : 0;
            }
        }

        public decimal InspectNowQty { get; set; }

        public decimal TotalInspectedQty
        {
            get { return PreviousInspectedQty + InspectNowQty; }
        }

        public decimal RemainingAfterQty
        {
            get
            {
                var rem = OrderedQty - TotalInspectedQty;
                return rem > 0 ? rem : 0;
            }
        }

        public string Remarks { get; set; }
        public bool IsSetLot { get; set; }
        public List<AIRSubItemViewModel> SubItems { get; set; }

        // Inventory Extension & Disposition tracking
        public string Disposition { get; set; }
        public string ItemExtnName { get; set; }
        public string CategoryCode { get; set; }
        public List<AIRItemInventoryDetailViewModel> InventoryDetails { get; set; }

        public int RequiredInventoryCount => (int)Math.Floor(InspectNowQty);
        public int CompletedInventoryCount => InventoryDetails != null ? InventoryDetails.Count(d => d.IsCompleted) : 0;
    }

    public class AIRItemInventoryDetailViewModel
    {
        public Guid Id { get; set; }
        public Guid? AirItemId { get; set; }
        public int ContentNo { get; set; }
        public string SetLotNo { get; set; }
        public int? SetLotQtyNo { get; set; }
        public int? TContentNo { get; set; }
        public bool IsCompleted { get; set; }

        // Common / ItemExtnOther
        public string SerialNo { get; set; }
        public string Condition { get; set; }
        public string Remarks { get; set; }

        // ItemExtnVehicle
        public string ConductionNo { get; set; }
        public string EngineNo { get; set; }
        public string ChasisNo { get; set; }
        public string PlateNo { get; set; }
        public string Color { get; set; }
        public int? YearModel { get; set; }
        public string SeriesNo { get; set; }
        public string MVFileNo { get; set; }
        public string CRN { get; set; }
        public DateTime? CRDate { get; set; }
        public string OrNo { get; set; }
        public DateTime? OrDate { get; set; }
        public int? NetWeight { get; set; }
        public string InsPolicyNo { get; set; }

        // ItemExtnLand
        public string PIN { get; set; }
        public string Address { get; set; }
        public string LandMarks { get; set; }
        public string TctNo { get; set; }
        public string DRPNo { get; set; }
        public decimal? MarketValue { get; set; }
        public decimal? PricePerSqm { get; set; }

        // ItemExtnBuilding
        public string ProjectName { get; set; }
        public string BuildingType { get; set; }
        public decimal? Area { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Status { get; set; }
    }

    public class AIRItemSourceAllocationViewModel
    {
        public string PRNumber { get; set; }
        public string Department { get; set; }
        public decimal Qty { get; set; }
    }

    public class AIRSubItemViewModel
    {
        public Guid? AirSubItemId { get; set; }
        public Guid OrderSubItemId { get; set; }
        public Guid? OrderSubItemRequestId { get; set; }
        public string SubItemNo { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal ExpectedQty { get; set; }
        public decimal PreviousInspectedQty { get; set; }

        public decimal RemainingBeforeQty
        {
            get
            {
                var rem = ExpectedQty - PreviousInspectedQty;
                return rem > 0 ? rem : 0;
            }
        }

        public decimal InspectNowQty { get; set; }

        public decimal TotalInspectedQty
        {
            get { return PreviousInspectedQty + InspectNowQty; }
        }

        public decimal RemainingAfterQty
        {
            get
            {
                var rem = ExpectedQty - TotalInspectedQty;
                return rem > 0 ? rem : 0;
            }
        }

        public string Remarks { get; set; }
    }

    public class AIRInvoiceViewModel
    {
        public AIRInvoiceViewModel()
        {
            InvoiceAttachments = new List<AIRDocumentViewModel>();
            InvoiceDate = DateTime.Today;
        }

        public string DrNo { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public string InvoiceType { get; set; }
        public string BillingReference { get; set; }
        public string Remarks { get; set; }
        public List<AIRDocumentViewModel> InvoiceAttachments { get; set; }
    }

    public class AIRDocumentViewModel
    {
        public Guid Id { get; set; }
        public Guid? AirId { get; set; }
        public string DocumentType { get; set; } // "InvoiceCopy", "DeliveryReceipt", "InspectionPhoto", "TechnicalDoc"
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileSize { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedDt { get; set; }
    }

    public class AIRWizardDraftListItemViewModel
    {
        public Guid Id { get; set; }
        public string DraftNo { get; set; }
        public string PONumber { get; set; }
        public string SupplierName { get; set; }
        public int CurrentStep { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class AIRGridItemViewModel
    {
        public Guid Id { get; set; }
        public string AirNo { get; set; }
        public string CtrlNo { get; set; }
        public DateTime? AirDate { get; set; }
        public Guid? OrderId { get; set; }
        public string PONumber { get; set; }
        public DateTime? PODate { get; set; }
        public string SupplierName { get; set; }
        public string Department { get; set; }
        public string InvoiceNo { get; set; }
        public string DrNo { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalInspectedQty { get; set; }
        public string OverallStatus { get; set; }
        public string InspectionStatus { get; set; }
        public string AcceptanceStatus { get; set; }
        public string RevisionComments { get; set; }
        public string WithdrawalReason { get; set; }
        public bool WithdrawalRequested { get; set; }

        // Dynamic action permissions
        public bool CanContinue { get; set; }
        public bool CanWithdraw { get; set; }
        public bool CanRequestWithdrawal { get; set; }
        public bool CanRevise { get; set; }
        public bool CanStartAcceptance { get; set; }
        public bool CanContinueAcceptance { get; set; }
        public bool CanPost { get; set; }
        public bool CanPrint { get; set; }
    }

    public class AIRAcceptanceWizardViewModel
    {
        public AIRAcceptanceWizardViewModel()
        {
            Items = new List<AIRAcceptanceItemViewModel>();
            SupportingDocuments = new List<AIRDocumentViewModel>();
            AcceptanceDate = DateTime.Today;
        }

        public Guid AirId { get; set; }
        public string AirNo { get; set; }
        public string CtrlNo { get; set; }
        public string PONumber { get; set; }
        public DateTime? PODate { get; set; }
        public string SupplierName { get; set; }
        public string Department { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string InspectorName { get; set; }
        public string InspectionLocation { get; set; }
        public string DrNo { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal? InvoiceAmount { get; set; }

        // Acceptance Details
        public DateTime AcceptanceDate { get; set; }
        public string AcceptedBy { get; set; }
        public string AcceptedByDesignation { get; set; }
        public string DepartmentOffice { get; set; }
        public string AcceptanceRemarks { get; set; }

        public List<AIRAcceptanceItemViewModel> Items { get; set; }
        public List<AIRDocumentViewModel> SupportingDocuments { get; set; }
    }

    public class AIRAcceptanceItemViewModel
    {
        public Guid AirItemId { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal InspectedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public string Disposition { get; set; } // "Inventory" or "ForDistribution"
        public string DestinationDepartment { get; set; }
        public string DestinationCustodian { get; set; }
        public string Remarks { get; set; }
    }

    public class AIRActionRequestViewModel
    {
        public Guid AirId { get; set; }
        public string ActionType { get; set; } // "Withdraw", "RequestWithdrawal", "ReturnForRevision", "DeclineWithdrawal"
        public string Reason { get; set; }
    }

    public class AIRItemHistoryViewModel
    {
        public AIRItemHistoryViewModel()
        {
            Inspections = new List<AIRItemPastInspectionViewModel>();
        }

        public Guid OrderItemId { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal TotalPostedInspectedQty { get; set; }
        public decimal RemainingQty => OrderedQty - TotalPostedInspectedQty;
        public List<AIRItemPastInspectionViewModel> Inspections { get; set; }
    }

    public class AIRItemPastInspectionViewModel
    {
        public Guid AirId { get; set; }
        public string AirNo { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string InspectorName { get; set; }
        public decimal InspectedQty { get; set; }
        public string InvoiceNo { get; set; }
        public string Status { get; set; }
    }

    public class POInspectionCandidateViewModel
    {
        public Guid OrderId { get; set; }
        public string PONumber { get; set; }
        public DateTime? PODate { get; set; }
        public string PRNumber { get; set; }
        public string Department { get; set; }
        public string SupplierName { get; set; }
        public string Purpose { get; set; }
        public string DeliveryPlace { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalOrderedQty { get; set; }
        public decimal TotalPostedInspectedQty { get; set; }
        public decimal RemainingInspectableQty { get; set; }
        public string InspectionStatusText { get; set; } // "Not Inspected", "Partially Inspected", "Fully Inspected"
        public int InspectionProgressPercent { get; set; }
    }
}
