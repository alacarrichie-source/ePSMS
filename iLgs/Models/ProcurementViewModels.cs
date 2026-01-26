using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class ProcurementOrderVM : ProcurementVM
    {
        public ProcurementOrderVM()
        {
            this.RefType = "PO";
        }
        public Nullable<System.Guid> SupplierId { get; set; }
        public string SupName { get; set; }
        public string SupBusiness { get; set; }
        public string SupAddress { get; set; }
        public string SupTIN { get; set; }
        public string SupEmail { get; set; }
        public string SupZipCode { get; set; }
        public string SupContactNo { get; set; }
        public string DeliveryPlace { get; set; }
        public string DeliveryDate { get; set; }
        public string TermDelivery { get; set; }
        public string TermPayment { get; set; }
        public string SignedBySuppName { get; set; }
        public Nullable<System.DateTime> SignedBySuppDate { get; set; }
        public string SignedByAuthName { get; set; }
        public string SignedByAuthDesignation { get; set; }
        public string ResoNo { get; set; }
        public string CertifiedCorrectBy { get; set; }
        public Nullable<System.DateTime> CertifiedCorredtDate { get; set; }
        public string PrNo { get; set; }
        public Nullable<System.DateTime> PrDate { get; set; }
    }

    public class ProcurementVM
    {
        public System.Guid Id { get; set; }
        public string RefType { get; set; }
        public string RefValue { get; set; }
        public Nullable<System.DateTime> RefDate { get; set; }
        public string Fund { get; set; }
        public Nullable<System.Guid> DepartmentId { get; set; }
        public string Department { get; set; }
        public string Division { get; set; }
        public string FPP { get; set; }
        public string PostedBy { get; set; }
        public Nullable<System.DateTime> PostedDt { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class ProcurementUnitGroupVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> ProcId { get; set; }
        public string SetLotNo { get; set; }
        public Nullable<int> Qty { get; set; }
        public string Unit { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> TotalCost { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class ProcurementUnitGroupDescriptionVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> UnitGroupId { get; set; }
        public string Description { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class ProcurementUnitGroupDescriptionItemVM 
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> UnitGroupDescriptionId { get; set; }
        public Nullable<System.Guid> ProcItemId { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients
        public string Category { get; set; }
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal? QtyRequest { get; set; }
        public decimal? PriceRate { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
        public decimal? GroupUnitCost { get; set; }
        public decimal? GroupTotalCost { get; set; }
        public int? GroupQty { get; set; }
    }    
}