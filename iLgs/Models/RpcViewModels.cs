using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RPCI_VM
    {
        public System.Guid Id { get; set; }

        public string Type { get; set; }

        [Required]
        [Display(Name = "As Of")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AsOf { get; set; }

        [Required]
        public string Fund { get; set; }

        [Display(Name = "From Donation")]
        public bool? FromDonation { get; set; } = false;

        [Required]
        [Display(Name = "Inventory / For Distribution")]
        public string InvDist { get; set; }

        //[Required]
        [Display(Name = "Account")]
        public Nullable<System.Guid> ItemTypeId { get; set; }
        public string Account { get; set; }

        [Display(Name = "Department / Location")]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Department / Location")]
        public string Department { get; set; }

        [Display(Name = "Certified correct by")]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Verrified by")]
        public string VerifiedBy { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedDt { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        public bool? IsPosted { get; set; } = true;

        // Transients

        [Display(Name = "Inventory/For distribution")]
        //public string InvDistDesc { get { return this.InvDist == "I" ? "Inventory" : this.InvDist == "D" ? "For Distribution" : ""; } }
        public string InvDistDesc { get; set; }

        [Display(Name = "Acquisition Mode")]
        //public string AcqMode { get { return this.FromDonation == true ? "From Donation" : "Purchase"; } }
        public string AcqMode { get; set; }

        [Display(Name = "Qty Balance")]
        public Nullable<int> QtyBalance { get; set; }

        [Display(Name = "Acquisition Cost")]
        public Nullable<decimal> AcqCost { get; set; }

        [Display(Name = "Consumables Qty Issued")]
        public Nullable<int> QtyCons { get; set; }

        [Display(Name = "Consumables Amount")]
        public Nullable<decimal> AmountCons { get; set; }

        [Display(Name = "SPHV Qty Issued")]
        public Nullable<int> QtySPHV { get; set; }

        [Display(Name = "SPHV Amount")]
        public Nullable<decimal> AmountSPHV { get; set; }

        [Display(Name = "SPLV Qty Issued")]
        public Nullable<int> QtySPLV { get; set; }

        [Display(Name = "SPLV Amount")]
        public Nullable<decimal> AmountSPLV { get; set; }

        [Display(Name = "Total Semi-Expendable Qty")]
        public int? QtySE { get; set; }

        [Display(Name = "Total Semi-Expendable Amount")]
        public decimal? AmountSE { get; set; }
    }

    public class RPCIItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RpciId { get; set; }

        [Display(Name = "Article")]
        public Nullable<System.Guid> ItemCodeId { get; set; }

        public string Account { get; set; }

        [Display(Name = "Sub-Accounts")]
        public string SubAccount { get; set; }

        public string Article { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [Display(Name = "AIR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AirDate { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Unit")]
        public string Unit { get; set; }

        [Display(Name = "Department")]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Department Display")]
        public string Department { get; set; }
        public Nullable<int> Qty { get; set; }

        [Display(Name = "Location Code")]
        public Nullable<System.Guid> LocationId { get; set; }
        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }

        [Display(Name = "Location")]
        public string LocationName { get; set; }

        [Display(Name = "Transfer In")]
        public Nullable<int> TransferIn { get; set; }

        [Display(Name = "Transfer Out")]
        public Nullable<int> TransferOut { get; set; }

        [Display(Name = "Qty Iss.")]
        public Nullable<int> QtyIss { get; set; }

        [Display(Name = "In Balance")]
        public Nullable<int> QtyInBalance { get; set; }

        [Display(Name = "Transfer In Balance")]
        public Nullable<int> TransferInBalance { get; set; }

        [Display(Name = "Total Balance")]
        public Nullable<int> TotalBalance { get; set; }

        [Display(Name = "Acquisition Cost")]
        public Nullable<decimal> AcqCost { get; set; }

        [Display(Name = "Old Stock No.")]
        public string OldStockNo { get; set; }

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }

        public string Brand { get; set; }

        [Display(Name = "Model")]
        public string Model_ { get; set; }

        [Display(Name = "Serisl No.")]
        public string SerialNo { get; set; }

        [Display(Name = "Item Description")]
        public string Description { get; set; }

        [Display(Name = "Other Particulars")]
        public string OtherDesc { get; set; }
        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }


    [MetadataType(typeof(RPCIItem.Metadata))]
    public partial class RPCIItem
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> RpciId { get; set; }

            [Display(Name = "Article")]
            public Nullable<System.Guid> ItemCodeId { get; set; }

            [Display(Name = "PO No.")]
            public string PoNo { get; set; }

            [Display(Name = "PO Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PoDate { get; set; }

            [Display(Name = "AIR No.")]
            public string AirNo { get; set; }

            [Display(Name = "AIR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AirDate { get; set; }

            [Display(Name = "Unit Cost")]
            public Nullable<decimal> UnitCost { get; set; }

            [Display(Name = "Unit")]
            public string Unit { get; set; }

            [Display(Name = "Department")]
            public Nullable<System.Guid> DeptId { get; set; }

            [Display(Name = "Department Display")]
            public string Department { get; set; }
            public Nullable<int> Qty { get; set; }

            [Display(Name = "Qty Iss.")]
            public Nullable<int> QtyIss { get; set; }

            [Display(Name = "Location Code")]
            public Nullable<System.Guid> LocationId { get; set; }

            [Display(Name = "Location Code")]
            public string LocationCode { get; set; }

            [Display(Name = "Location")]
            public string LocationName { get; set; }

            [Display(Name = "Transfer In")]
            public Nullable<int> TransferIn { get; set; }

            [Display(Name = "Transfer Out")]
            public Nullable<int> TransferOut { get; set; }

            [Display(Name = "Total Balance")]
            public Nullable<int> TotalBalance { get; set; }


            [Display(Name = "Acquisition Cost")]
            public Nullable<decimal> AcqCost { get; set; }

            [Display(Name = "Old Stock No.")]
            public string OldStockNo { get; set; }

            [Display(Name = "Stock No.")]
            public string StockNo { get; set; }

            public string Brand { get; set; }

            [Display(Name = "Model")]
            public string Model_ { get; set; }

            [Display(Name = "Serisl No.")]
            public string SerialNo { get; set; }

            [Display(Name = "Item Description")]
            public string Description { get; set; }

            [Display(Name = "Other Particulars")]
            public string OtherDesc { get; set; }
            public string Remarks { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            //public RPCI RPCI { get; set; }
        }
    }

    public class RPCEFFOPPE_VM
    {
        public System.Guid Id { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "As At")]
        public Nullable<System.DateTime> AsAt { get; set; }
        public string Department { get; set; }

        [Display(Name = "Accountable Officer")]
        public string AccountableOfficer { get; set; }
        public string Designation { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AssumptionDt { get; set; }

        [Display(Name = "Certified correct by")]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Verrified by")]
        public string VerifiedBy { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedDt { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        // Transients
    }

    [MetadataType(typeof(RPCIDepLoc.Metadata))]
    public partial class RPCIDepLoc
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> RpciId { get; set; }
            public Nullable<System.Guid> DeptId { get; set; }
            public Nullable<System.Guid> LocationId { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }

    public class RPCEFFOPPEItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RpceffoppeId { get; set; }

        [Display(Name = "Item Type")]
        public string ItemType { get; set; }
        public string Fund { get; set; }
        public string Article { get; set; }
        public string Description { get; set; }

        [Display(Name = "Property No.")]
        public string PropertyNo { get; set; }

        [Display(Name = "Cost")]
        public Nullable<decimal> Cost { get; set; }

        public string Location { get; set; }
        public string Condition { get; set; }
        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class RpcPpeVM
    {
        public System.Guid Id { get; set; }
        [Display(Name = "As Of Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AsOf { get; set; }
        public string Department { get; set; }
        [Display(Name = "Certified Correct by")]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Designation")]
        public string CertifiedCorrectDesignation { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Designation")]
        public string ApprovedDesignation { get; set; }

        [Display(Name = "Verified by")]
        public string VerifiedBy { get; set; }

        [Display(Name = "Designation")]
        public string VerifiedDesignation { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

    }

    public class RpcPpeItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RpcPpeId { get; set; }
        public string Fund { get; set; }
        public string Account { get; set; }
        public string Article { get; set; }

        [Display(Name = "Sub Article")]
        public string SubArticle { get; set; }
        public string Type { get; set; }
        public string Brand { get; set; }

        [Display(Name = "Model")]
        public string Model_ { get; set; }

        [Display(Name = "Serial No")]
        public string SerialNo { get; set; }
        public string Others { get; set; }
        public string Color { get; set; }

        [Display(Name = "New Property No.")]
        public string PropNo { get; set; }

        [Display(Name = "Property No.")]
        public string OldPropNo { get; set; }
        public Nullable<decimal> Cost { get; set; }

        [Display(Name = "Acquisition Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcqDate { get; set; }

        [Display(Name = "Acquisition Mode")]
        public string ActMode { get; set; }
        public string Location { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        [Display(Name = "Ref. Type")]
        public string RefType { get; set; }

        [Display(Name = "New Accountable Officer")]
        public string Officer { get; set; }

        [Display(Name = "Accountable Officer")]
        public string OldOfficer { get; set; }
        public string Condition { get; set; }
        public string Remarks { get; set; }
        public string Annex { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }
   
    public class RPCITotalVM
    {
        public Guid Id { get; set; }

        [Display(Name = "Item Type")]
        public string ItemType { get; set; }
        public string Account { get; set; }

        [Display(Name = "Sub-Account")]
        public string SubAccount { get; set; }

        public int? Qty { get; set; }

        [Display(Name = "Qty Issued")]
        public int? QtyIss { get; set; }

        [Display(Name = "Qty-In Balance")]
        public int? QtyInBalance { get; set; }

        [Display(Name = "Transfer-In Balance")]
        public int? TransferInBalance { get; set; }

        [Display(Name = "Total Balance")]
        public int? TotalBalance { get; set; }

        [Display(Name = "Acquisition Cost")]
        public decimal? AcqCost { get; set; }
    }
}