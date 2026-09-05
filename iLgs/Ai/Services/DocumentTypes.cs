namespace iLgs.Ai.Services
{
    public static class DocumentTypes
    {
        public const string PurchaseRequest = "PR";
        public const string PurchaseOrder = "PO";
        public const string AcceptanceInspectionReport = "AIR";
        public const string RequisitionIssuanceSlip = "RIS";
        public const string PropertyAcknowledgementReceipt = "PAR";
        public const string InventoryCustodianSlip = "ICS";
    }

    public static class PrStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string Returned = "Returned";
        public const string Revising = "Revising";
        public const string Posted = "Posted";
        public const string Cancelled = "Cancelled";
    }

    public static class AirStatuses
    {
        public const string Draft = "Draft";
        public const string InspectionInProgress = "Inspection In Progress";
        public const string SubmittedForAcceptance = "Submitted for Acceptance";
        public const string ReturnedForRevision = "Returned for Revision";
        public const string AcceptanceInProgress = "Acceptance In Progress";
        public const string Accepted = "Accepted";
        public const string Posted = "Posted";
        public const string Cancelled = "Cancelled";
        public const string Withdrawn = "Withdrawn";
    }

    public static class AirInspectionStatuses
    {
        public const string Draft = "Draft";
        public const string InProgress = "In Progress";
        public const string Submitted = "Submitted for Acceptance";
        public const string Returned = "Returned for Revision";
        public const string Posted = "Posted";
    }

    public static class AirAcceptanceStatuses
    {
        public const string None = "None";
        public const string Pending = "Pending";
        public const string InProgress = "In Progress";
        public const string Accepted = "Accepted";
        public const string Declined = "Declined";
    }

    public static class AirDispositions
    {
        public const string Inventory = "Inventory";
        public const string ForDistribution = "ForDistribution";
    }
}
