using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    /// <summary>
    /// Kept in Session["PoWizard"] for the lifetime of the 5-step Create Purchase Order wizard.
    /// Mirrors the state the original static HTML pages implied (selected PRs, per-item
    /// assignment/grouping, consolidated grouping, header terms, uploaded doc names).
    /// </summary>
    [Serializable]
    public class PurchaseOrderWizardState
    {
        public List<string> SelectedPurchaseRequestIds { get; set; } = new List<string>();

        /// <summary>Key: PurchaseRequestItemId, Value: assignment rows (an item can be split across PO groups).</summary>
        public List<ItemAssignment> Assignments { get; set; } = new List<ItemAssignment>();

        public List<PoGroupHeader> Groups { get; set; } = new List<PoGroupHeader>();

        public int ActiveGroupNumber { get; set; } = 1;

        public List<UploadedDocument> Documents { get; set; } = new List<UploadedDocument>();

        public PoGroupHeader ActiveGroup =>
            Groups.FirstOrDefault(g => g.GroupNumber == ActiveGroupNumber);
    }

    [Serializable]
    public class ItemAssignment
    {
        public Guid PurchaseRequestItemId { get; set; }
        public int GroupNumber { get; set; }
        public decimal AssignedQty { get; set; }
    }

    [Serializable]
    public class PoGroupHeader
    {
        public int GroupNumber { get; set; }
        public string Label { get; set; }

        public Guid? SupplierId { get; set; }
        public DateTime PoDate { get; set; } = DateTime.Today;
        public string ModeOfProcurement { get; set; }
        public string DeliveryPeriod { get; set; }
        public string PlaceOfDelivery { get; set; }
        public string PaymentTerms { get; set; }
        public string DeliveryTerms { get; set; }
        public string OtherTerms { get; set; }

        /// <summary>Per-consolidated-item free-text technical specs, keyed by CatalogCode.</summary>
        public Dictionary<string, string> AdditionalInfoByCatalogCode { get; set; } = new Dictionary<string, string>();
    }

    [Serializable]
    public class UploadedDocument
    {
        public string Category { get; set; }
        public string FileName { get; set; }
        public string StoredPath { get; set; }
    }
}
