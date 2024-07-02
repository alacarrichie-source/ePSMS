using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PsCardVM
    {
        public PsCardVM()
        {
            //this.FieldsAccountableForm = new FieldsAccountableForm() { Id = this.Id };
            //this.FieldsAgricultural = new FieldsAgricultural() { Id = this.Id };
            //this.FieldsAnimal = new FieldsAnimal() { Id = this.Id };
            //this.FieldsFurniture = new FieldsFurniture() { Id = this.Id };
            //this.FieldsLand = new FieldsLand() { Id = this.Id };
            //this.FieldsMachinery = new FieldsMachinery() { Id = this.Id };
            //this.FieldsMedical = new FieldsMedical() { Id = this.Id };
            //this.FieldsMedicine = new FieldsMedicine() { Id = this.Id };
            //this.FieldsMilitarySuuply = new FieldsMilitarySuuply() { Id = this.Id };
            //this.FieldsNonAccountableForm = new FieldsNonAccountableForm() { Id = this.Id };
            //this.FieldsOfficeSupply = new FieldsOfficeSupply() { Id = this.Id };
            //this.FieldsOther = new FieldsOther() { Id = this.Id };
            //this.FieldsOtherSupplyMaterial = new FieldsOtherSupplyMaterial() { Id = this.Id };
            //this.FieldsRepair = new FieldsRepair() { Id = this.Id };
            //this.FieldsTransportation = new FieldsTransportation() { Id = this.Id };
            //this.FieldsVehicle = new FieldsVehicle() { Id = this.Id };
            //this.FieldsConstruction = new FieldsConstruction() { Id = this.Id };
            this.AllField = new AllField() { Id = this.Id };
        }

        public System.Guid Id { get; set; }

        [Display(Name = "Article")]
        [Required]
        public Nullable<System.Guid> ItemCodeId { get; set; }

        [Required]
        public string Fund { get; set; }

        [Required]
        [MaxLength(900)]
        [Display(Name = "Item Description")]
        public string Description { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Card Category")]
        public string CardCategory { get; set; } // P or S only, to identify where the item belongs.

        [Display(Name = "Stock/Property No.")]
        [Required]
        public string PsNo { get; set; }
        
        [Display(Name = "Stock/Property Name")]
        public string PsName { get; set; }

        [Display(Name = "Prev. Stock/Property No.")]
        public string PrevPsNo { get; set; }

        //[Display(Name = "Acq. Date")]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //public Nullable<System.DateTime> AcqDate { get; set; }

        //[Display(Name = "Acq. Mode")]
        //public string AcqMode { get; set; }

        [Display(Name = "From Donation")]
        public Nullable<bool> FromDonation { get; set; }

        public Nullable<decimal> Amount { get; set; }
        //public string Brand { get; set; }

        //[Display(Name = "Model")]
        //public string Model_ { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        //public FieldsAccountableForm FieldsAccountableForm { get; set; }
        //public FieldsAgricultural FieldsAgricultural { get; set; }
        //public FieldsAnimal FieldsAnimal { get; set; }
        //public FieldsFurniture FieldsFurniture { get; set; }
        //public FieldsLand FieldsLand { get; set; }
        //public FieldsMachinery FieldsMachinery { get; set; }
        //public FieldsMedical FieldsMedical { get; set; }
        //public FieldsMedicine FieldsMedicine { get; set; }
        //public FieldsMilitarySuuply FieldsMilitarySuuply { get; set; }
        //public FieldsNonAccountableForm FieldsNonAccountableForm { get; set; }
        //public FieldsOfficeSupply FieldsOfficeSupply { get; set; }
        //public FieldsOther FieldsOther { get; set; }
        //public FieldsOtherSupplyMaterial FieldsOtherSupplyMaterial { get; set; }
        //public FieldsRepair FieldsRepair { get; set; }
        //public FieldsTransportation FieldsTransportation { get; set; }
        //public FieldsVehicle FieldsVehicle { get; set; }
        //public FieldsConstruction FieldsConstruction { get; set; }
        public AllField AllField { get; set; }

        // Transients

        [Display(Name = "Article")]
        public string Item { get; set; }
        public string ItemNo { get; set; }
        public string ItemCode { get; set; }

        [Display(Name = "Account")]
        public string ItemType { get; set; }
        public string ItemTypeCode { get; set; }

        [Display(Name = "Sub-account")]
        public string SubAccount { get; set; }
        public string SubAccountCode { get; set; }

        [Display(Name = "Field Group No.")]
        public int? FieldGroupNo { get; set; }
    }

    public class PsCardItemVM : IValidatableObject
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
                } else
                {
                    yield return new ValidationResult(
                        "Either Qty or Transfer-In must be provided.",
                        new[] { nameof(TransferIn) }
                    );
                }
            }
        }

    }

    public class PsCardItemIssuanceVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemId { get; set; }
        
        [Display(Name = "Department")]
        //[Required]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }
        
        [Display(Name = "Issued Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Required]
        public Nullable<int> Qty { get; set; }
        public Nullable<decimal> Amount { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted date")]
        public Nullable<System.DateTime> PostedDt { get; set; }

        // Transients
        public string Department { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public int? IssuedToSw { get; set; }
    }
}