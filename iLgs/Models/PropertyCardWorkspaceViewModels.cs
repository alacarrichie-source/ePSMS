using System;
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
}