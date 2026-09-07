using System;
using System.Collections.Generic;

namespace iLgs.Models
{
    public class RisCandidateVM
    {
        public Guid Id { get; set; }
        public string PoNo { get; set; }
        public DateTime? PoDate { get; set; }
        public string Supplier { get; set; }
        public string PrNo { get; set; }
        public string Department { get; set; }
        public int ItemCount { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal PreviousQty { get; set; }
        public decimal RemainingQty { get; set; }
        public string Status { get; set; }
    }

    public class RisAllocationVM
    {
        public Guid Id { get; set; }
        public string ItemNo { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal PreviousQty { get; set; }
        public decimal RemainingQty { get; set; }
        public decimal QtyRequest { get; set; }
        public decimal QtyIssue { get; set; }
    }

    public class RisWizardGroupVM
    {
        public RIS_VM Header { get; set; }
        public List<RisAllocationVM> Items { get; set; }
    }

    public class RisWizardVM
    {
        public Guid OrderId { get; set; }
        public string Department { get; set; }
        public string PoNo { get; set; }
        public List<RisWizardGroupVM> Groups { get; set; }
    }
}
