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
}
