// AIRItem.Additions.cs
// Extends the auto-generated AIRItem partial class with new columns added by
// DB_20260904_03_AIR_Module_Initial.sql.
// DO NOT regenerate AppManModel.edmx without also preserving this file.

namespace iLgs.Models
{
    public partial class AIRItem
    {
        // ----------------------------------------------------------------
        // Acceptance quantity tracking (separate from Qty = InspectedQty)
        // ----------------------------------------------------------------
        public decimal? InspectedQty { get; set; }
        public decimal? AcceptedQty { get; set; }

        // ----------------------------------------------------------------
        // Disposition (I = Inventory, D = For Distribution)
        // ----------------------------------------------------------------
        public string Disposition { get; set; }
        public string DestinationDepartment { get; set; }
        public string DestinationCustodian { get; set; }
    }
}
