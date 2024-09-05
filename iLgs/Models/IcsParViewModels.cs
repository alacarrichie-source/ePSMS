using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(IcsPar.Metadata))]
    public partial class IcsPar
    {        
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public string RefNo { get; set; }

            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> RefDate { get; set; }
            public string RefType { get; set; }

            [Required]
            [Display(Name = "Received by")]
            public string ReceivedBy { get; set; }

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
            [Display(Name = "Received From")]
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
            public Nullable<System.DateTime> PostedDt { get; set; }

            public string InsertedBy { get; set; }
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
            public Nullable<decimal> Amount { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            public IcsPar IcsPar { get; set; }
            public PsCardItemExtn PsCardItemExtn { get; set; }        
        }
    }

    public class GenerateIcsParVM
    {
        [Display(Name = "PO No.")]
        public string PoNo { get; set; }
        public DateTime? PoDate { get; set; }
        public Guid? PsCardItemId { get; set; }

        public Guid? DeptId { get; set; }

        [Display(Name = "Location")]
        [Required]
        public Guid? LocationId { get; set; }

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
        public string Unit { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }
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

        [Display(Name = "Posted by")]
        public string ParPostedBy { get; set; }

        [Display(Name = "Date Posted")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ParPostedDt { get; set; }
    }

    public class ParIcsItemSetVm
    {
        public System.Guid Id { get; set; }
        public int? Qty { get; set; }
        public string Unit { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> TotalCost { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public ICollection<PsCardItemUnitGroupDescription> UnitGroupDescriptions { get; set; }
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
}