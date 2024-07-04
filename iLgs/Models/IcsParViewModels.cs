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

            [Display(Name = "Received by")]
            public string ReceivedBy { get; set; }

            [Display(Name = "Position")]
            public string ReceivedByPosition { get; set; }

            [Display(Name = "Date")]
            public Nullable<System.DateTime> ReceivedDate { get; set; }

            [Display(Name = "Department")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public string ReceivedDept { get; set; }

            [Display(Name = "Received From")]
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
        //public string Location { get; set; }

        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> IcsParId { get; set; }
            public Nullable<System.Guid> PsCardItemId { get; set; }
            public Nullable<int> Qty { get; set; }
            public Nullable<decimal> Amount { get; set; }
            public Nullable<System.Guid> LocationId { get; set; }

            public string Location { get; set; }
            public string PropNo { get; set; }
            public string PropYear { get; set; }
            public string PropSeq { get; set; }
            public string TctNo { get; set; }
            public string PhaseNo { get; set; }
            public Nullable<decimal> PhaseAmount { get; set; }
            public string SerialNo { get; set; }
            public Nullable<int> YearModel { get; set; }
            public string NetWeight { get; set; }
            public string ConductionSticker { get; set; }
            public string PlateNo { get; set; }
            public string BodyNo { get; set; }
            public string CRN { get; set; }
            public Nullable<System.DateTime> CRNDate { get; set; }
            public string MVFileNo { get; set; }
            public string DRPNo { get; set; }
            public Nullable<System.DateTime> DTPDate { get; set; }
            public Nullable<decimal> CGT { get; set; }
            public Nullable<decimal> CGTTransferTax { get; set; }
            public Nullable<decimal> CGTSurcharge { get; set; }
            public Nullable<decimal> CGTInterest { get; set; }
            public Nullable<decimal> CGTCompromise { get; set; }
            public Nullable<decimal> TransferTax { get; set; }
            public Nullable<decimal> Surcharge { get; set; }
            public Nullable<decimal> Interest { get; set; }
            public Nullable<decimal> ConfirmationFee { get; set; }
            public Nullable<decimal> TransferRegsFee { get; set; }
            public Nullable<decimal> RealPropertyTax { get; set; }
            public Nullable<decimal> VAT { get; set; }
            public Nullable<decimal> EstateTax { get; set; }
            public Nullable<decimal> Tilting { get; set; }
            public Nullable<decimal> CertificationFee { get; set; }
            public Nullable<decimal> Relocation { get; set; }
            public Nullable<decimal> Surveying { get; set; }
            public Nullable<decimal> IncidentalExpenses { get; set; }
            public string CapitalOutlayOrExpense { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            //public virtual Codextn Codextn { get; set; }
            public IcsPar IcsPar { get; set; }
            public PsCardItem PsCardItem { get; set; }
        }
    }

    public class GenerateIcsParVM
    {
        public Guid? PsCardItemId { get; set; }

        [Display(Name = "Location")]
        public Guid? LocationId { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }
        
        public int? Qty { get; set; }
        public DateTime Date { get; set; }
        public string RefType { get; set; }

        public IcsPar IcsPar { get; set; }
    }

    public class ParVM
    {
        public System.Guid Id { get; set; }
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
}