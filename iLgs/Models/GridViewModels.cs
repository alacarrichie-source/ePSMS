using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    // ---- Step 0: Index / Purchase Orders list grid row ----
    public class PurchaseOrderListItemVm
    {
        public Guid PurchaseOrderId { get; set; }
        public string PoNumber { get; set; }
        public DateTime? PoDate { get; set; }
        public string SupplierName { get; set; }
        public string SourcePrs { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
    }

    // ---- Step 1: Select PRs grid row ----
    public class SelectablePurchaseRequestVm
    {
        public Guid PurchaseRequestId { get; set; }
        public bool Selected { get; set; }
        public string PrNumber { get; set; }
        public DateTime? PrDate { get; set; }
        public string Department { get; set; }
        public string Purpose { get; set; }
        public string FundCluster { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }

    // ---- Step 2: Assign Items grid row ----
    public class UnassignedItemVm
    {
        public Guid PurchaseRequestItemId { get; set; }
        public string PrNumber { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal? RequestedQty { get; set; }
        public decimal? AssignQty { get; set; }
        public decimal? RemainingQty { get; set; }
        public int? GroupNumber { get; set; }
    }

    // ---- Step 3: Consolidated item row (grouped by CatalogCode within a PO group) ----
    public class ConsolidatedItemVm
    {
        public string CatalogCode { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal? ConsolidatedQty { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? Total => ConsolidatedQty * UnitCost;
        public List<AllocationSourceVm> Sources { get; set; } = new List<AllocationSourceVm>();
    }

    public class AllocationSourceVm
    {
        public string PrNumber { get; set; }
        public decimal Qty { get; set; }
    }

    // ---- Step 4: Item row shown alongside header form for technical specs ----
    public class PoDetailItemVm
    {
        public string CatalogCode { get; set; }
        public string Description { get; set; }
        public string SourcePrs { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal UnitCost { get; set; }
        public string AdditionalInformation { get; set; }
    }
}
