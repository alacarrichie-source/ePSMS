// AIR.Additions.cs
// Extends the auto-generated AIR partial class with new columns added by
// DB_20260904_03_AIR_Module_Initial.sql.
// DO NOT regenerate AppManModel.edmx without also preserving this file.

namespace iLgs.Models
{
    using System;

    public partial class AIR
    {
        // ----------------------------------------------------------------
        // Workflow status columns (redundant denormalized cache — optional)
        // ----------------------------------------------------------------
        public string InspectionStatus { get; set; }
        public string AcceptanceStatus { get; set; }
        public string OverallStatus { get; set; }

        // ----------------------------------------------------------------
        // Delivery / Invoice extras
        // ----------------------------------------------------------------
        public string DrNo { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public string InvoiceType { get; set; }
        public string BillingReference { get; set; }

        // ----------------------------------------------------------------
        // Inspection committee details
        // ----------------------------------------------------------------
        public string InspectionLocation { get; set; }
        public string InspectorName { get; set; }
        public string InspectorDesignation { get; set; }
        public string InspectionCommittee { get; set; }

        // ----------------------------------------------------------------
        // Acceptance tracking
        // ----------------------------------------------------------------
        public string AcceptanceStartedBy { get; set; }
        public DateTime? AcceptanceStartedDt { get; set; }
        public string AcceptedBy { get; set; }
        public string AcceptedByDesignation { get; set; }
        public string AcceptanceRemarks { get; set; }

        // ----------------------------------------------------------------
        // Withdrawal workflow
        // ----------------------------------------------------------------
        public bool WithdrawalRequested { get; set; }
        public string WithdrawalRequestedBy { get; set; }
        public DateTime? WithdrawalRequestedDt { get; set; }
        public string WithdrawalReason { get; set; }

        // ----------------------------------------------------------------
        // Revision workflow
        // ----------------------------------------------------------------
        public string RevisionComments { get; set; }
        public string ReturnedBy { get; set; }
        public DateTime? ReturnedDt { get; set; }
    }
}
