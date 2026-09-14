using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using IcsValue = iLgs.Models.Enums.IcsValue;

namespace iLgs.Models
{

    public class IcsParVM
    {
        public int? ItemCount { get; set; }
        public int? ItemCountActive { get; set; }

        [Display(Name = "Available Items")]
        public string ActiveItems { get; set; }

        [Display(Name = "Status")]
        public string Status_ { get; set; }

               
        [Display(Name = "Prev. Ref. No.")]
        public string PrevRefNo { get; set; }

        public string Remarks { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date")]
        [Required]
        public Nullable<System.DateTime> RefDate { get; set; }

        [Display(Name = "Location Code")]
        [Required]
        public Nullable<System.Guid> LocationId { get; set; }

        public string SelectedIds { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }

        [Display(Name = "Position")]
        public string Designation { get; set; }

        //------------------
        public System.Guid Id { get; set; }

        [Display(Name = "Update Code")]
        public string UpdateCode { get; set; }

        public string RefNo { get; set; }
        
        public string RefType { get; set; }
        
        public string LocationCode { get; set; }

        public string Location { get; set; }

        [Display(Name = "Received by")]
        [Required]
        public Nullable<System.Guid> ReceivedById { get; set; }

        [Required]
        [Display(Name = "Received by")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Title")]
        public string ReceivedByTitle { get; set; }

        [Display(Name = "Add'l Title")]
        public string ReceivedByTitle2 { get; set; }

        [Required]
        [Display(Name = "Position")]
        public string ReceivedByPosition { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ReceivedDate { get; set; }

        [Required]
        [Display(Name = "Department")]
        public string ReceivedDept { get; set; }

        [Required]
        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; }

        [Required]
        [Display(Name = "Position")]
        public string IssuedByPosition { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Required]
        [Display(Name = "Department")]
        public string IssuedDept { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }


    [MetadataType(typeof(IcsPar.Metadata))]
    public partial class IcsPar
    {        
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Display(Name = "Update Code")]
            public string UpdateCode { get; set; }

            public string RefNo { get; set; }

            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            [Display(Name = "Date")]
            public Nullable<System.DateTime> RefDate { get; set; }
            public string RefType { get; set; }

            [Display(Name = "Location Code")]
            public Nullable<System.Guid> LocationId { get; set; }

            public string LocationCode { get; set; }

            public string Location { get; set; }

            [Required]
            [Display(Name = "Received by")]
            public Nullable<System.Guid> ReceivedById { get; set; }

            [Display(Name = "Received by")]
            public string ReceivedBy { get; set; }
            
            [Display(Name = "Title")]
            public string ReceivedByTitle { get; set; }

            [Display(Name = "Add'l Title")]
            public string ReceivedByTitle2 { get; set; }

            [Required]
            [Display(Name = "Position")]
            public string ReceivedByPosition { get; set; }

            [Required]
            [Display(Name = "Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> ReceivedDate { get; set; }

            [Required]
            [Display(Name = "Department")]            
            public string ReceivedDept { get; set; }

            [Required]
            [Display(Name = "Issued By")]
            public string IssuedBy { get; set; }

            [Required]
            [Display(Name = "Position")]
            public string IssuedByPosition { get; set; }

            [Required]
            [Display(Name = "Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> IssuedDate { get; set; }

            [Required]
            [Display(Name = "Department")]
            public string IssuedDept { get; set; }

            [Display(Name = "Posted by")]
            public string PostedBy { get; set; }

            [Display(Name = "Posted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PostedDt { get; set; }

            public string InsertedBy { get; set; }

            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }            
        }
    }

    [MetadataType(typeof(IcsParItem.Metadata))]
    public partial class IcsParItem
    {
        public Nullable<decimal> UnitCost { get; set; }
        
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> IcsParId { get; set; }
            public Nullable<System.Guid> PsCardItemExtnId { get; set; }
            public Nullable<int> Qty { get; set; }

            [Display(Name = "Additional Cost")]
            public Nullable<decimal> AddCost { get; set; }
            public Nullable<decimal> Amount { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            [Display(Name = "Issued To")]
            public string IssuedTo { get; set; }
            public string Designation { get; set; }

            public IcsPar IcsPar { get; set; }
            public PsCardItemExtn PsCardItemExtn { get; set; }        
        }
    }

    public class IcsValueVM
    {
        public Guid Id { get; set; }
        public IcsValue IcsValue { get; set; }
    }

    public class GenerateIcsParVM
    {
        [Display(Name = "PO No.")]
        public string PoNo { get; set; }
        public DateTime? PoDate { get; set; }
        public Guid? UnitGroupId { get; set; }
        public string CardNo { get; set; }
        public Guid? PsCardItemId { get; set; }
        public Guid? PoItemId { get; set; }

        public Guid? DeptId { get; set; }

        [Display(Name = "Location Code")]
        [Required]
        public Guid? LocationId { get; set; }

        [Required]
        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }

        public string Location { get; set; }

        public int? Qty { get; set; }

        [Display(Name = "Reference Date")]
        [Required]
        public DateTime Date { get; set; }
        public string RefType { get; set; }

        public IcsPar IcsPar { get; set; }

        public string SelectedIds { get; set; }
        public string IndSet { get; set; } // I-Individual; S-Set

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }

        [Display(Name = "Position")]
        public string Designation { get; set; }

        public string BundleDataJson { get; set; }
        public List<ParBundleItemAllocationVM> Bundles { get; set; }
        public Guid? MainPsCardItemExtnId { get; set; }
        public Guid? ExistingParId { get; set; }
    }

    public class ParVM
    {
        public System.Guid Id { get; set; }
        public string CardNo { get; set; }
        public Nullable<System.Guid> GroupId { get; set; }
        public Nullable<System.Guid> PsCardId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Display(Name = "PO Date")]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "AIR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]

        public Nullable<System.DateTime> AirDate { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [Display(Name = "Issuance Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AirIssueDate { get; set; }

        //[Required]
        public Nullable<int> Qty { get; set; }

        [Display(Name = "Qty. Iss.")]
        public Nullable<int> QtyIss { get; set; }
        [Display(Name = "Qty. Bal.")]
        public Nullable<int> QtyBal { get; set; }

        [Display(Name = "Transfer-In")]
        public Nullable<int> TransferIn { get; set; }

        [Display(Name = "Transfer-Out")]
        public Nullable<int> TransferOut { get; set; }

        [Display(Name = "Transaction Type")]
        public string TranType { get; set; }

        public string Description { get; set; }

        [Required]
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "PO Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public Nullable<decimal> PriceRate { get; set; }
        public Nullable<int> Days { get; set; }

        [Display(Name = "Additional Cost")]
        public Nullable<decimal> AddCost { get; set; }

        [Display(Name = "Amount")]
        public Nullable<decimal> GTotalCost { get; set; }
        public string Remarks { get; set; }

        [Display(Name = "Department/Office")]
        [Required]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Location")]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Department Display")]
        public string DeptDisplay { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients
        public string Article { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Location")]
        public string LocCode { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }

        public int? ParBalance { get; set; }
        public int? IcsBalance { get; set; }
        public string StockNo { get; set; }
        [Display(Name = "Remaining Balance")]
        public Nullable<int> RemBalance { get; set; }

        public bool? IsForICS { get; set; }
        public Nullable<System.DateTime> AcqDate { get; set; }

        //public OrderItemUnitGroupDescriptionItem OrderItemUnitGroupDescriptionItem { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Qty.HasValue && !TransferIn.HasValue)
            {
                if (!Qty.HasValue)
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { "Qty" }
                    );
                }
                else
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { "TransferIn" }
                    );
                }
            }
        }
    }

    public class IcsVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> GroupId { get; set; }
        public Nullable<System.Guid> PsCardId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Display(Name = "PO Date")]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "AIR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]

        public Nullable<System.DateTime> AirDate { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [Display(Name = "Issuance Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AirIssueDate { get; set; }

        [Display(Name = "Acquisition Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcqDate { get; set; }

        //[Required]
        public Nullable<int> Qty { get; set; }

        [Display(Name = "Qty. Iss.")]
        public Nullable<int> QtyIss { get; set; }
        [Display(Name = "Qty. Bal.")]
        public Nullable<int> QtyBal { get; set; }

        [Display(Name = "Transfer-In")]
        public Nullable<int> TransferIn { get; set; }

        [Display(Name = "Transfer-Out")]
        public Nullable<int> TransferOut { get; set; }

        [Display(Name = "Transaction Type")]
        public string TranType { get; set; }

        public string Description { get; set; }

        [Required]
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "PO Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public Nullable<decimal> PriceRate { get; set; }
        public Nullable<int> Days { get; set; }
        public string Remarks { get; set; }

        [Display(Name = "Department/Office")]
        [Required]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Location")]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Department Display")]
        public string DeptDisplay { get; set; }

        public Nullable<bool> IsConsumable { get; set; }
        public Nullable<bool> IsIncorporated { get; set; }
        public Nullable<bool> IsOthers { get; set; }
        public string OtherRemarks { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients
        public string InvDist { get; set; }

        public string Article { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Location")]
        public string LocCode { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }

        public int? ParBalance { get; set; }
        public int? IcsBalance { get; set; }
        public string StockNo { get; set; }
        [Display(Name = "Remaining Balance")]
        public Nullable<int> RemBalance { get; set; }        
        //public OrderItemUnitGroupDescriptionItem OrderItemUnitGroupDescriptionItem { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Qty.HasValue && !TransferIn.HasValue)
            {
                if (!Qty.HasValue)
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { "Qty" }
                    );
                }
                else
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { "TransferIn" }
                    );
                }
            }
        }
    }

    public class ParIcsPOGroupVM
    {
        public System.Guid Id { get; set; }

        public string Fund { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "PO Date")]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; } // concat value due to 1 PO is to many AIRs

        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "AIR Date")]
        //public Nullable<System.DateTime> AirDate { get; set; }
        public string AirDate { get; set; } // concat value due to 1 PO is to many AIRs

        public Nullable<System.Guid> DeptId { get; set; }

        public string Department { get; set; }

        public decimal? ParBalance { get; set; }
        public decimal? IcsBalance { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public string SPoDate { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public string SAirDate { get; set; }

        public string Status { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Amount { get; set; }

        [Display(Name = "Finished")]
        public int? QtyFinished { get; set; }

        [Display(Name = "Pending")]
        public decimal? QtyBalance { get { return this.Qty - this.QtyFinished; } }
    }

    public class ParIcsItemVm
    {
        public System.Guid Id { get; set; }        
        public System.Guid? GroupId { get; set; }
        public System.Guid? PsCardId { get; set; }
        public System.Guid? PsCardItemId { get; set; }
        public Guid? UnitGroupId { get; set; }
        public string CardNo { get; set; }

        [Display(Name = "Set Qty")]
        public int? SetQty { get; set; }

        [Display(Name = "Idividual Qty")]
        public int? Qty { get; set; }

        [Display(Name = "Total Qty")]
        public int? TotalQty { get; set; }

        public string PoNo { get; set; }
        public string AirNo { get; set; }
        public Nullable<System.DateTime> PoDate { get; set; }
        public Nullable<System.DateTime> AirDate { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }

        [Display(Name = "Additional Cost")]
        public Nullable<decimal> AddCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> GTotalCost { get; set; }

        public Nullable<decimal> SetCost { get; set; }

        public string Article { get; set; }
        public string Description { get; set; }
        public string StockNo { get; set; }
        public int? Balance { get { return this.Qty - this.GeneratedItems; } }

        [Display(Name = "For ICS")]
        public Nullable<bool> IsForICS { get; set; }

        [Display(Name = "Consumable")]
        public Nullable<bool> IsConsumable { get; set; }

        [Display(Name = "Incorporated")]
        public Nullable<bool> IsIncorporated { get; set; }

        [Display(Name = "Others")]
        public Nullable<bool> IsOthers { get; set; }

        [Display(Name = "Remarks")]
        public string OtherRemarks { get; set; }
        public int? GeneratedItems { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }

        [Display(Name = "Consumable")]
        public string IsConsumableSetup { get; set; }

        [Display(Name = "Incorporated")]
        public string IsIncorporatedSetup { get; set; }

        [Display(Name = "Inventory/For Distribution")]
        public string ForDistributionSetup { get; set; }

        [Display(Name = "Inventory/For Distribution")]
        public string InvDist { get; set; }

        [Display(Name = "Inventory/For Distribution")]
        public string InvDistDisplay { get; set; }

        [Display(Name = "Posted by")]
        public string ParPostedBy { get; set; }

        [Display(Name = "Date Posted")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ParPostedDt { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Group Description")]
        public string SetLotDesc { get; set; }

        public int? ParIcsBalance { get { return this.Qty - this.GeneratedItems; } }

        public string OtherDesc { get; set; }

        [Display(Name = "PO Item No.")]
        public string ItemNo { get; set; }
        public string ItemNoIndex { get; set; }
        public int? Padding { get; set; }
        public bool IsSetLot { get; set; } // Sw, to hold to determine the unit. If set/lot, only set/lot unit is allowed.

        [Display(Name = "Designated Custodian")]
        public string DesignatedCustodian { get; set; }

        [Display(Name = "Property No.")]
        public string PropNo { get; set; }

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }

        [Display(Name = "ICS Status")]
        public string IcsStatus { get; set; }

        [Display(Name = "ICS No.")]
        public string GeneratedIcsNo { get; set; }

        [Display(Name = "Accountable Officer")]
        public string AccountableOfficer { get { return DesignatedCustodian; } set { DesignatedCustodian = value; } }

        [Display(Name = "PAR Status")]
        public string ParStatus { get { return IcsStatus; } set { IcsStatus = value; } }

        [Display(Name = "PAR No.")]
        public string GeneratedParNo { get { return GeneratedIcsNo; } set { GeneratedIcsNo = value; } }
    }

    public class ParIcsItemSetVm
    {
        public System.Guid Id { get; set; }
        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }
        public int? Qty { get; set; }
        public string Unit { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> TotalCost { get; set; }
        public Nullable<decimal> AddCost { get; set; }
        public Nullable<decimal> GTotalCost { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        //public ICollection<PsCardItemUnitGroupDescription> UnitGroupDescriptions { get; set; }

        [Display(Name = "Posted By")]
        public string SetPostedBy { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> SetPostedDt { get; set; }
    }

    public class ParIcsPoVM
    {
        public string PoNo { get; set; }
        public Nullable<System.DateTime> PoDate { get; set; }
        public string AirNo { get; set; }
        public Nullable<System.DateTime> AirDate { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
    }

    public class EmployeeVM
    {
        public System.Guid Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }        
    }

    public class IcsParTransferItemVM
    {
        public System.Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set/Lot No.")]
        public Nullable<int> SetLotQtyNo { get; set; }

        [Display(Name = "Item Qty No.")]
        public Nullable<int> ContentNo { get; set; }

        [Display(Name = "Total Qty")]        
        public Nullable<int> TContentNo { get; set; }

        public string ItemNo { get { return this.ContentNo.ToString().Trim() + (this.TContentNo == null ? "" : "/" + this.TContentNo.ToString().Trim()); } }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        public string Description { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TUnitCost { get; set; }
    }

    public class IcsParItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemExtnId { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set/Lot No.")]
        public Nullable<int> SetLotQtyNo { get; set; }

        [Display(Name = "Item Qty No.")]
        public Nullable<int> ContentNo { get; set; }

        [Display(Name = "Total Qty")]
        public Nullable<int> TContentNo { get; set; }

        [Display(Name = "Item No.")]
        public string ItemNo { get { return this.ContentNo.ToString().Trim() + (this.TContentNo == null ? "" : "/" + this.TContentNo.ToString().Trim()); } }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        public string Description { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TUnitCost { get; set; }

        [Display(Name = "Prev. ICS/PAR No.")]
        public string PrevIcsParNo { get; set; }

        [Display(Name = "Cancelled by ICS/PAR No.")]
        public string CanByIcsParNo { get; set; }

        [Display(Name = "Prop. No.")]
        public string PropNo { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }

        [Display(Name = "Designation")]
        public string Designation { get; set; }

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }
    }

    public class ParBundleComponentDisplayVM
    {
        public Guid IcsParItemId { get; set; }
        public Guid PsCardSubItemId { get; set; }
        public Guid? PsCardItemExtnId { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string SerialNo { get; set; }
        public decimal Qty { get; set; }
        public string Unit { get; set; }
        public string SourceType { get; set; }
        public bool IsRequiredForBundle { get; set; }
    }

    public class ParBundleItemAllocationVM
    {
        public Guid MainPhysicalItemId { get; set; }
        public Guid? MainPsCardItemExtnId
        {
            get { return MainPhysicalItemId; }
            set { if (value.HasValue) MainPhysicalItemId = value.Value; }
        }
        public string PropNo { get; set; }
        public string SerialNo { get; set; }
        public string MainDescription { get; set; }
        public string IssuedTo { get; set; }
        public string Designation { get; set; }
        public string Status { get; set; }
        public List<string> MissingComponents { get; set; }
        public List<ParBundleComponentAllocationVM> Components { get; set; }

        public ParBundleItemAllocationVM()
        {
            Components = new List<ParBundleComponentAllocationVM>();
            MissingComponents = new List<string>();
        }
    }

    public class IcsBatchPreviewVM
    {
        public List<ParBundleItemAllocationVM> Bundles { get; set; }
        public int ReadyCount { get { return Bundles.Count(b => b.Status == "Ready"); } }
        public int IncompleteCount { get { return Bundles.Count(b => b.Status != "Ready"); } }

        public IcsBatchPreviewVM()
        {
            Bundles = new List<ParBundleItemAllocationVM>();
        }
    }

    public class ParBundleComponentAllocationVM
    {
        public Guid PsCardSubItemId { get; set; }
        public Guid? PsCardItemExtnId { get; set; }
        public decimal Qty { get; set; }
        public string SerialNo { get; set; }
        public string Description { get; set; }
        public string SourceType { get; set; }
        public bool IsRequiredForBundle { get; set; }
    }

    public class ParComponentInventoryVM
    {
        public Guid PsCardSubItemId { get; set; }
        public string SubItemNo { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public string SourceType { get; set; }
        public bool IsRequiredForBundle { get; set; }
        public decimal QtyPerParent { get; set; }
        public decimal TotalQty { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal AvailableQty { get; set; }
        public bool IsSerialized { get; set; }
        public List<ParComponentSerialVM> AvailableSerials { get; set; }

        public ParComponentInventoryVM()
        {
            AvailableSerials = new List<ParComponentSerialVM>();
        }
    }

    public class ParComponentSerialVM
    {
        public Guid Id { get { return PsCardItemExtnId; } set { PsCardItemExtnId = value; } }
        public Guid PsCardItemExtnId { get; set; }
        public string SerialNo { get; set; }
        public string PropNo { get; set; }
        public int? ContentNo { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
    }

    public class ParMainUnitInventoryVM
    {
        public Guid PsCardItemExtnId { get; set; }
        public string SerialNo { get; set; }
        public int? ContentNo { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal AcqCost { get; set; }
        public string PropNo { get; set; }
        public string UpcomingOfficer { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
        public string Remarks { get; set; }
    }

    public class ParBundleBuilderInitVM
    {
        public Guid PsCardItemId { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string StockNo { get; set; }
        public string Unit { get; set; }
        public string PoNo { get; set; }
        public decimal TotalItemQty { get; set; }
        public decimal UnitCost { get; set; }
        public List<ParMainUnitInventoryVM> AvailableMainUnits { get; set; }
        public List<ParComponentInventoryVM> Components { get; set; }
        public Guid? MainPsCardItemExtnId { get; set; }
        public string SerialNo { get; set; }
        public string PropNo { get; set; }
        public decimal AddCost { get; set; }
        public decimal AcqCost { get; set; }
        public Guid? ExistingParId { get; set; }
        public string ExistingParNo { get; set; }
        public Guid? ExistingReceivedById { get; set; }
        public string ExistingIssuedTo { get; set; }
        public Guid? ExistingLocationId { get; set; }
        public string ExistingLocation { get; set; }
        public string ExistingLocationCode { get; set; }
        public DateTime? ExistingRefDate { get; set; }
        public string ExistingReceivedBy { get; set; }
        public string ExistingReceivedByTitle { get; set; }
        public string ExistingReceivedByTitle2 { get; set; }
        public string ExistingReceivedByPosition { get; set; }
        public string ExistingReceivedDept { get; set; }
        public DateTime? ExistingReceivedDate { get; set; }
        public string ExistingDesignation { get; set; }
        public string ExistingIssuedBy { get; set; }
        public string ExistingIssuedByPosition { get; set; }
        public string ExistingIssuedDept { get; set; }
        public DateTime? ExistingIssuedDate { get; set; }
        public List<Guid> ExistingPreselectedSerialIds { get; set; }
        public Dictionary<Guid, decimal> ExistingPreselectedQuantities { get; set; }

        public ParBundleBuilderInitVM()
        {
            AvailableMainUnits = new List<ParMainUnitInventoryVM>();
            Components = new List<ParComponentInventoryVM>();
            ExistingPreselectedSerialIds = new List<Guid>();
            ExistingPreselectedQuantities = new Dictionary<Guid, decimal>();
        }
    }
}
