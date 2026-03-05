using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class OrderVM
    {
        // 
        [Display(Name = "Ctrl. No.")]
        public string CtrlNo { get; set; }

        public string Fund { get; set; }

        //[Display(Name = "Specific")]
        //public string FundSpecific { get; set; }

        [Required]
        [Display(Name = "PR No.")]
        public string PrNo { get; set; }

        //[Required]
        [Display(Name = "PR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? PrDate { get; set; }

        //[Required]
        //[Display(Name = "Department")]
        //public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        //public string Section { get; set; }

        //public string FPP { get; set; }
        //        

        [Display(Name = "Mode of Procurement")]
        public string PoModeDesc { get; set; }

        public bool IsIssued { get; set; }
        public DataSourceRequest Request { get; set; }

        public System.Guid Id { get; set; }

        [Display(Name = "Supplier")]
        [Required]
        public Nullable<System.Guid> SupplierId { get; set; }

        [Display(Name = "Supplier")]
        public string SupName { get; set; }

        [Display(Name = "Business Name")]
        public string SupBusiness { get; set; }

        [Display(Name = "Address")]
        public string SupAddress { get; set; }

        [Display(Name = "TIN")]
        public string SupTIN { get; set; }

        [Display(Name = "Email")]
        public string SupEmail { get; set; }

        [Display(Name = "Zip Code")]
        public string SupZipCode { get; set; }

        [Display(Name = "Contact No.")]
        public string SupContactNo { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Required]
        [Display(Name = "PR No.")]
        public Nullable<System.Guid> PrId { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Mode of Procurement")]
        //[Required]
        public string PoMode { get; set; }


        [Display(Name = "Place of Delivery")]
        //[Required]
        public string DeliveryPlace { get; set; }

        [Display(Name = "Date of Delivery")]
        public string DeliveryDate { get; set; }

        [Display(Name = "Delivery Term")]
        //[Required]
        public string TermDelivery { get; set; }

        [Display(Name = "Payment Term")]
        //[Required]
        public string TermPayment { get; set; }

        [Display(Name = "Conforme: Signature over printed name of Supplier")]
        public string SignedBySuppName { get; set; }

        [Display(Name = "Date Signed")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required]
        public Nullable<System.DateTime> SignedBySuppDate { get; set; }

        [Display(Name = "Very truly yours, City Mayor")]
        public string SignedByAuthName { get; set; }

        [Display(Name = "Designation")]
        public string SignedByAuthDesignation { get; set; }

        [Display(Name = "Resolution No.")]
        public string ResoNo { get; set; }

        [Display(Name = "Certified Correct: Secretary of the Sanggunian")]
        //[Required]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Date Certified")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required]
        public Nullable<System.DateTime> CertifiedCorrectDate { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> PostedDt { get; set; }


        // TRANSIENTS        

        public Nullable<decimal> QtyTotal { get; set; }
        public Nullable<decimal> QtyIssued { get; set; }
        public Nullable<decimal> QtyRemaining { get; set; }
        public Nullable<decimal> TotalAmount { get; set; }
        public bool IsLocked { get; set; }                
    }

    public class OrderItemVM
    {                       
        public OrderItemVM()
        {
            this.Id = Guid.NewGuid();
            this.AllField = new AllField() { Id = this.Id };
        }

        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderId { get; set; }
        public Nullable<System.Guid> RequestItemId { get; set; }

        [Display(Name = "Item No.")]
        public string ItemNo { get; set; }
        public string ItemNoIndex { get; set; }

        [Display(Name = "Article")]
        //[Required]
        public Nullable<System.Guid> ItemCodeId { get; set; }

        [Display(Name = "Stock/Property No.")] // Generic (Without Brand)        
        public string PsNo { get; set; }

        [Display(Name = "Stock/Property No.")] // for printing
        public string PsNoDisplay { get; set; }

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        [Required]
        public string Description { get; set; }

        public string Brand { get; set; }

        [Display(Name = "Estimated Life")]
        public Nullable<decimal> EstimatedLife { get; set; }

        [Required]
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "Other Particulars")]
        public string OtherDesc { get; set; }

        [Required]
        public Nullable<decimal> Qty { get; set; }

        //[Required]
        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        //[Required]
        public Nullable<decimal> Amount { get; set; }

        [Display(Name = "Price Rate")]
        public Nullable<decimal> PriceRate { get; set; }

        public Nullable<decimal> AddCost { get; set; }
        public Nullable<decimal> TUnitCost { get; set; }
        public Nullable<decimal> GTotalCost { get; set; }

        [Display(Name = "PPMP Code")]
        public string PpmpCode { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }


        public AllField AllField { get; set; }


        // Transients                
        public Nullable<decimal> QtyIssued { get; set; }
        public Nullable<decimal> QtyRemaining { get; set; }
        public string GridOrderItemExtns { get; set; }
        public string Mode { get; set; }
        
        [Display(Name = "Sub-Accounts")]
        public string SubAccount { get; set; }

        public string Category { get; set; }

        [Display(Name = "Group")]
        public string SetLotNo { get; set; }

        public string ItemCode { get; set; }
        public string ItemType { get; set; }
        public string PsType { get; set; }

        [Display(Name = "Account")]
        public string PsTypeDesc { get; set; }

        public int? Padding { get; set; }

        public bool IsSetLot { get; set; } // Sw, to hold to determine the unit. If set/lot, only set/lot unit is allowed.
        public string OriginalDescription { get; set; }
        public string OriginalOtherDesc { get; set; }

        public Nullable<System.Guid> UnitGroupDescriptionId { get; set; }
    }

    public class OrderItemGroupVM
    {
        public Guid? ItemCodeId { get; set; }
        public string StockNo { get; set; }
        public string StockName { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Fund { get; set; }
        public string Unit { get; set; }
        public string AccountCode { get; set; }
        public string ItemCategory { get; set; }
        public string CardCategory { get; set; }
        public string InvDist { get; set; }
    }

    public class OrderItemExtnVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }
        
        [Display(Name = "Field No")]
        public string ItemNo { get; set; }

        [Display(Name = "Field Name")]
        public string ItemKey { get; set; }

        [Display(Name = "Field Value")]
        public string ItemValue { get; set; }

        public int? Sequence { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class PoIssuanceVM
    {
        public System.Guid Id { get; set; }
        public System.Guid? OrderItemId { get; set; }
        public System.Guid? RisItemId { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Display(Name = "PR No.")]
        public string PrNo { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "AIR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AirDate { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Fund")]
        public string Fund { get; set; }

        [Display(Name = "Qty")]
        public int? Qty { get; set; }

        [Display(Name = "Qty Iss")]
        public int? QtyIss { get; set; }

        [Display(Name = "Transfer-In")]
        public Nullable<int> TransferIn { get; set; }

        [Display(Name = "Transfer-Out")]
        public Nullable<int> TransferOut { get; set; }

        [Display(Name = "Balance")]
        public int? Balance { get; set; }

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }

        [Display(Name = "Stock Name")]
        public string StockName { get; set; }

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }
        public bool IsProperty { get; set; }
        public bool IsWithPar { get; set; }
        public bool IsWithIcs { get; set; }

        public int? ParBalance { get; set; }
        public int? IcsBalance { get; set; }
    }

    public class OrderItemUnitGroupVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderId { get; set; }
        public Nullable<System.Guid> RequestItemUnitGroupId { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> TotalCost { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transients
        [Display(Name = "Set/Lot Qty")]
        public int? Qty { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }
        //public RequestItemUnitGroup RequestItemUnitGroup { get; set; }
    }

    public class OrderItemUnitGroupDescriptionVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderItemUnitGroupId { get; set; }
        public Nullable<System.Guid> RequestItemUnitGroupDescriptionId { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        //public RequestItemUnitGroupDescription RequestItemUnitGroupDescription { get; set; }
        public string Description { get; set; }

        [Display(Name = "Other Particulars")]
        public string OtherParticulars { get; set; }

        // transients
        public Nullable<System.Guid> OrderId { get; set; }
        [Display(Name = "Set/Lot No.")]
        public string SetLotNo { get; set; }
        public Nullable<int> Qty { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Amount")]
        public Nullable<decimal> TotalCost { get; set; }        
    }

    [MetadataType(typeof(OrderItemUnitGroup.Metadata))]
    public partial class OrderItemUnitGroup
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> OrderId { get; set; }
            public Nullable<System.Guid> RequestItemUnitGroupId { get; set; }

            [Display(Name = "Set/Lot No.")]
            public string SetLotNo { get; set; }
            public Nullable<int> Qty { get; set; }
            public string Unit { get; set; }

            [Display(Name = "Unit Cost")]
            public Nullable<decimal> UnitCost { get; set; }

            [Display(Name = "Amount")]
            public Nullable<decimal> TotalCost { get; set; }
            public Nullable<decimal> AddCost { get; set; }
            public Nullable<decimal> TUnitCost { get; set; }
            public Nullable<decimal> GTotalCost { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }        
    }

    public class OrderItemUnitGroupDescriptionItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderItemUnitGroupDescriptionId { get; set; }
        public Nullable<System.Guid> RequestItemUnitGroupDescriptionItemId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public RequestItemUnitGroupDescriptionItem RequestItemUnitGroupDescriptionItem { get; set; }

        public string Category { get; set; }

        [Display(Name = "Stock/Prop No.")]
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }

        [Display(Name = "Other Particulars")]
        public string OtherDesc { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "Qty per Set/Lot")]
        public Nullable<int> QtyRequest { get; set; }

        [Display(Name = "Price Rate")]
        public Nullable<decimal> PriceRate { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }

        public Nullable<decimal> GroupUnitCost { get; set; }
        public Nullable<decimal> GroupTotalCost { get; set; }

        [Display(Name = "Qty of Sets/Lots")]
        public Nullable<int> GroupQty { get; set; }

        [Display(Name = "Item No.")]
        public string ItemNo { get; set; }
        public string ItemNoIndex { get; set; }

        public string GridItems { get; set; }
    }

    public class UnitGroupAvailableItemVM
    {
        public System.Guid Id { get; set; }

        public string Category { get; set; }

        [Display(Name = "Stock/Prop No.")]
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }
        public Nullable<int> QtyRequest { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
    }    
}