using System;
using System.Collections.Generic;

namespace iLgs.Models
{
    public class PropertyCardWorkspaceVM
    {
        public PropertyCardVM Card { get; set; }
        public PropertyCardPositionVM Position { get; set; }
    }

    public class PropertyCardPositionVM
    {
        public decimal Received { get; set; }
        public decimal TransferIn { get; set; }
        public decimal Issued { get; set; }
        public decimal TransferOut { get; set; }
        public decimal Balance { get; set; }
        public decimal BalanceValue { get; set; }
        public int AcquisitionCount { get; set; }
        public int UnitCount { get; set; }
        public string LatestPo { get; set; }
        public DateTime? LatestPoDate { get; set; }
        public DateTime? LatestAcquisitionDate { get; set; }
    }

    public class PropertyCardUnitChoiceVM
    {
        public Guid Id { get; set; }
        public Guid? AcquisitionId { get; set; }
        public string Label { get; set; }
        public string PoNo { get; set; }
        public string PropNo { get; set; }
        public string CustItemNo { get; set; }
        public string Location { get; set; }
        public string Condition { get; set; }        
    }

    public class PropertyCardHistoryVM
    {
        public Guid Id { get; set; }
        public string PropNo { get; set; }
        public string PoNo { get; set; }
        public string Remarks { get; set; }
        public Guid? TransferId { get; set; }
        public Guid? IssuanceId { get; set; }
        public Guid? IcsParId { get; set; }
        public DateTime? InsertedDt { get; set; }
        public string InsertedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
    }

    public class PropertyCardUnitVM
    {
        public Guid Id { get; set; }
        public Guid PsCardItemId { get; set; }
        public Guid? PsCardSubItemId { get; set; }
        public Guid? LocationId { get; set; }
        public string Remarks { get; set; }
        public int? ContentNo { get; set; }

        public string Description { get; set; }
        public string PropertyNo { get; set; }
        public string CustItemNo { get; set; }

        public string SerialNo { get; set; }
        public string PlateNo { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }

        public string Location { get; set; }
        public string Condition { get; set; }

        public decimal? AcquisitionCost { get; set; }

        public string AccountabilityStatus { get; set; }

        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class PropertyCardComponentVM
    {
        public Guid Id { get; set; }
        public Guid PsCardItemId { get; set; }
        public string AcquisitionPoNo { get; set; }
        public string SubItemNo { get; set; }
        public string Description { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? QtyPerParent { get; set; }
        public string SourceType { get; set; }
        public bool IsRequiredForBundle { get; set; }
        public string Remarks { get; set; }

        public Guid? AIRSubItemId { get; set; }
        public bool IsAirSource { get; set; }

        // Counts
        public decimal ReceivedQty { get; set; }
        public int PhysicalUnitCount { get; set; }
        public decimal AssignedCount { get; set; }
        public decimal AvailableCount { get; set; }

        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanCreateUnit { get; set; }
    }

    public class PropertyCardBundleItemVM
    {
        public Guid Id { get; set; }
        public Guid PsCardSubItemId { get; set; }
        public Guid? PsCardItemExtnId { get; set; }
        public string SubItemNo { get; set; }
        public string Description { get; set; }
        public string PhysicalUnitCode { get; set; }
        public string SerialNo { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public string SourceType { get; set; }
        public bool IsRequiredForBundle { get; set; }
        public bool IsIndividuallyTracked { get; set; }
        public string Remarks { get; set; }
    }

    public class PropertyCardUnitAccountabilityVM
    {
        public Guid UnitId { get; set; }
        public Guid PsCardId { get; set; }
        public Guid PsCardItemId { get; set; }
        public string PropertyNo { get; set; }
        public string CustItemNo { get; set; }
        public string SerialOrPlateNo { get; set; }
        public string Description { get; set; }
        public int? ContentNo { get; set; }
        public string PoNo { get; set; }
        public string Location { get; set; }
        public string Condition { get; set; }
        public string Remarks { get; set; }
        public decimal? AcquisitionCost { get; set; }

        public string DerivedStatus { get; set; }
        public bool HasAccountability { get; set; }

        // Current Accountability Details
        public Guid? IcsParItemId { get; set; }
        public Guid? IcsParId { get; set; }
        public string RefType { get; set; }
        public string RefNo { get; set; }
        public DateTime? RefDate { get; set; }
        public string CurrentOfficer { get; set; }
        public string CurrentPosition { get; set; }
        public string CurrentDepartment { get; set; }
        public string CurrentLocation { get; set; }
        public string OriginalOfficer { get; set; }
        public string PreviousOfficer { get; set; }
        public string PostedBy { get; set; }
        public DateTime? PostedDt { get; set; }
        public bool IsPosted { get; set; }
        public string TransferStatus { get; set; }
        public bool HasDraftSuccessor { get; set; }
        public string DraftSuccessorRefNo { get; set; }

        // Assigned Bundle Components
        public List<PropertyCardBundleItemVM> Components { get; set; }

        // Accountability History
        public List<IcsParAccountabilityHistoryVM> History { get; set; }

        public PropertyCardUnitAccountabilityVM()
        {
            Components = new List<PropertyCardBundleItemVM>();
            History = new List<IcsParAccountabilityHistoryVM>();
        }
    }
}
