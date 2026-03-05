using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using static iLgs.Models.Enums;

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

            [Display(Name = "Received by")]
            public Nullable<System.Guid> ReceivedById { get; set; }

            [Display(Name = "Received by")]
            public string ReceivedBy { get; set; }

            [Display(Name = "Title")]
            public string ReceivedByTitle { get; set; }

            [Display(Name = "Add'l Title")]
            public string ReceivedByTitle2 { get; set; }

            [Display(Name = "Position")]
            public string ReceivedByPosition { get; set; }

            [Display(Name = "Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> ReceivedDate { get; set; }

            [Display(Name = "Department")]            
            public string ReceivedDept { get; set; }

            [Display(Name = "Issued By")]
            public string IssuedBy { get; set; }

            [Display(Name = "Position")]
            public string IssuedByPosition { get; set; }

            [Display(Name = "Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> IssuedDate { get; set; }

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
        public Guid? PsCardItemId { get; set; }

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
    }

    public class ParVM
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

        public int? ParBalance { get; set; } = 0;
        public int? IcsBalance { get; set; } = 0;
        public string StockNo { get; set; }
        [Display(Name = "Remaining Balance")]
        public Nullable<int> RemBalance { get; set; }

        public bool? IsForICS { get; set; }
        public Nullable<System.DateTime> AcqDate { get; set; }

        public OrderItemUnitGroupDescriptionItem OrderItemUnitGroupDescriptionItem { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Qty.HasValue && !TransferIn.HasValue)
            {
                if (!Qty.HasValue)
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { nameof(Qty) }
                    );
                }
                else
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { nameof(TransferIn) }
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

        public int? ParBalance { get; set; } = 0;
        public int? IcsBalance { get; set; } = 0;
        public string StockNo { get; set; }
        [Display(Name = "Remaining Balance")]
        public Nullable<int> RemBalance { get; set; }        
        public OrderItemUnitGroupDescriptionItem OrderItemUnitGroupDescriptionItem { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Qty.HasValue && !TransferIn.HasValue)
            {
                if (!Qty.HasValue)
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { nameof(Qty) }
                    );
                }
                else
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { nameof(TransferIn) }
                    );
                }
            }
        }
    }

    public class ParIcsPOGroupVM
    {
        public System.Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "PO Date")]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "AIR Date")]
        public Nullable<System.DateTime> AirDate { get; set; }

        public Nullable<System.Guid> DeptId { get; set; }

        public string Department { get; set; }

        public int? ParBalance { get; set; }
        public int? IcsBalance { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public string SPoDate { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public string SAirDate { get; set; }

        public string Status { get; set; }
        public int? Qty { get; set; }

        [Display(Name = "Finished")]
        public int? QtyFinished { get; set; }

        [Display(Name = "Pending")]
        public int? QtyBalance { get { return this.Qty - this.QtyFinished; } }
    }

    public class ParIcsItemVm
    {
        public System.Guid Id { get; set; }
        public System.Guid? GroupId { get; set; }
        public System.Guid? PsCardId { get; set; }
        public int? Qty { get; set; }

        [Display(Name = "Total Qty")]
        public int? TotalQty { get; set; }

        public string Unit { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }

        [Display(Name = "Additional Cost")]
        public Nullable<decimal> AddCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> GTotalCost { get; set; }

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
        public int? GeneratedItems { get; set; } = 0;
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
        public string ItemNo { get; set; }
        public string ItemNoIndex { get; set; }
        public int? Padding { get; set; }
        public bool IsSetLot { get; set; } // Sw, to hold to determine the unit. If set/lot, only set/lot unit is allowed.
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
        public ICollection<PsCardItemUnitGroupDescription> UnitGroupDescriptions { get; set; }

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
    }
}