using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RISlipVM
    {        
        public System.Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public Nullable<System.Guid> OrderId { get; set; }
        public string Fund { get; set; }
        public string Division { get; set; }
        public string Office { get; set; }
        public string FPP { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Display(Name = "RIS Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RisDate { get; set; }
        public string Purpose { get; set; }

        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }

        [Display(Name = "Requested Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RequestedDate { get; set; }

        [Display(Name = "Requested By Designation")]
        public string RequestedByDesignation { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approved Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ApprovedDate { get; set; }

        [Display(Name = "Aporoved By Designation")]
        public string ApprovedByDesignation { get; set; }

        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; }

        [Display(Name = "Issued Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Display(Name = "Issued By Designation")]
        public string IssuedByDesignation { get; set; }

        [Display(Name = "Received By")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Received Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ReceivedDate { get; set; }

        [Display(Name = "Received By Designation")]
        public string ReceivedByDesignation { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }        
    }

    public class RISlipItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisId { get; set; }
        public Nullable<System.Guid> StockItemId { get; set; }
        public Nullable<decimal> ReqQty { get; set; }
        public Nullable<decimal> IssQty { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        public Nullable<decimal> Amount { get; set; }

        public string IssRemarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients

        public string StockNo { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }        

    }

    public class RIS_VM
    {        
        public System.Guid Id { get; set; }

        [Required]
        public string Fund { get; set; }
        public string Division { get; set; }

        [Required]
        [Display(Name = "Office")]
        public Nullable<System.Guid> OfficeId { get; set; }

        [Required]
        [Display(Name = "Office Display")]
        public string Office { get; set; }

        [Required]
        public string FPP { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Required]
        [Display(Name = "RIS Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RisDate { get; set; }

        [Required]
        public string Purpose { get; set; }

        [Required]
        [Display(Name = "Requested by")]
        public string RequestedBy { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string RequestedByDesignation { get; set; }

        [Required]
        [Display(Name = "Date")]
        public Nullable<System.DateTime> RequestedDate { get; set; }

        [Required]
        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string ApprovedByDesignation { get; set; }

        [Required]
        [Display(Name = "Date")]
        public Nullable<System.DateTime> ApprovedDate { get; set; }

        [Required]
        [Display(Name = "Issued by")]
        public string IssuedBy { get; set; }

        [Display(Name = "Designation")]
        public string IssuedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Display(Name = "Received by")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Designation")]
        public string ReceivedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> ReceivedDate { get; set; }


        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> PostedDt { get; set; }

        public bool IsPosted { get; set; }
        public bool IssuanceSw { get; set; }
    }

    public class RisItemCommonVM
    {
        [Required]
        public string Description { get; set; }

        [Display(Name = "Other Description")]
        public string OtherDesc { get; set; } 

        [Display(Name = "Stock/Property No.")]
        public string PsNo { get; set; } // Generic (Without Brand)        

        [Display(Name = "Stock/Property No.")]
        public string PsNoDisplay { get; set; } // For printing 

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; } // Used as category

        [Display(Name = "Category Code")]
        public string PsType { get; set; } // Used as category

        [Display(Name = "Account")]
        public string PsTypeDesc { get; set; } // Used as category description

        [Display(Name = "Item Code")]
        public string ItemCode { get; set; } // Code of ItemCodeId

        [Display(Name = "Item")]
        public string ItemType { get; set; } // Description of ItemCodeId

        public string ItemNo { get; set; }

        [Required]
        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }
        public Nullable<decimal> PriceRate { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }
    }

    public class RisItemVM : RisItemCommonVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisId { get; set; }

        [Required]
        [Display(Name = "Item")]
        public Nullable<System.Guid> ItemCodeId { get; set; }        

        [Required]
        [Display(Name = "Qty Req.")]
        public Nullable<int> QtyRequest { get; set; }

        [Display(Name = "Qty Iss.")]
        public Nullable<int> QtyIssue { get; set; }

        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }                        

        public string GridRisItemExtns { get; set; }

        // for reference in dropdown templates
        public string Department { get; set; }
        public bool IsPosted { get; set; }
    }

    [MetadataType(typeof(RisItemMedecine.Metadata))]
    public partial class RisItemMedecine
    {
        internal sealed class Metadata
        {
            [Display(Name = "Generic Name")]
            public string GenericName { get; set; }

            [Display(Name = "Dosage Strength")]
            public string DosageStrength { get; set; }

            [Display(Name = "Dosage Form")]
            public string DosageForm { get; set; }
            public string Brand { get; set; }
            public string Others { get; set; }
        }
    }

    [MetadataType(typeof(RisItemVehicle.Metadata))]
    public partial class RisItemVehicle
    {
        internal sealed class Metadata
        {
            public System.Guid RisItemId { get; set; }
            public string Type { get; set; }
            public string Make { get; set; }
            public string Series { get; set; }

            [Display(Name = "Year Model")]
            public Nullable<int> YearModel { get; set; }

            [Display(Name = "Plate No.")]
            public string PlateNo { get; set; }

            [Display(Name = "Body No.")]
            public string BodyNo { get; set; }

            public string Color { get; set; }

            [Display(Name = "Engine No.")]
            public string EngineNo { get; set; }

            [Display(Name = "Chassis No.")]
            public string ChassisNo { get; set; }         
        }
    }

    [MetadataType(typeof(RisItemPpe.Metadata))]
    public partial class RisItemPpe
    {
        internal sealed class Metadata
        {
            public System.Guid RisItemId { get; set; }
            public string Type { get; set; }
            public string Brand { get; set; }

            [Display(Name = "Model")]
            public string Model_ { get; set; }

            [Display(Name = "Serial No.")]
            public string SerialNo { get; set; }
            public string Others { get; set; }
            public string Color { get; set; }
        }        
    }

    public class RisItemEntryVM : RisItemVM
    {
        public RisItemEntryVM()
        {
            this.Id = Guid.NewGuid();
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

        [Display(Name = "Sub-account")]
        public string SubAccount { get; set; }
        public string SubAccountCode { get; set; }
    }

    public class RisItemExtnVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisItemId { get; set; }
        [Display(Name = "Field No.")]
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

        // Transients
        public bool IsEnabled { get; set; }
    }
    
    public class RisIssuedVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisItemId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Display(Name = "Location")]
        [Required]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Accountable Officer")]
        public Nullable<System.Guid> OfficerId { get; set; }


        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }

        [Display(Name = "Issued To Position")]
        public string IssuedToPosition { get; set; }

        [Display(Name = "Issued Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; }

        [Display(Name = "Issued By Position")]
        public string IssuedByPosition { get; set; }

        [Display(Name = "Issued By Date")]
        public Nullable<System.DateTime> IssuedByDate { get; set; }

        [Required]
        public Nullable<int> Qty { get; set; }

        public Nullable<decimal> Amount { get; set; }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        [Display(Name = "Ref. Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RefDate { get; set; }

        [Display(Name = "Ref. Type")]
        public string RefType { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        [Display(Name = "Property No.")]
        public string PropNo { get; set; }

        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transient
        public Nullable<decimal> UnitCost { get; set; }
        public string Location { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; } // for PAR/ICS Generation (used in _GeneratePAR.cshtml)

        [Display(Name = "Accountable Officer")]
        public string Officer { get; set; }

    }

    [MetadataType(typeof(RisItemUnitGroupVM.Metadata))]
    public class RisItemUnitGroupVM : RisItemUnitGroup
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> RisId { get; set; }

            [Display(Name = "Group No.")]
            public string SetLotNo { get; set; }

            [Display(Name = "Set/Lot Qty")]
            public Nullable<int> Qty { get; set; }

            [Display(Name = "Unit of Measurement")]
            public string Unit { get; set; }
            public string InsertedBy { get; set; }
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }

    [MetadataType(typeof(RisItemUnitGroupDescriptionVM.Metadata))]
    public class RisItemUnitGroupDescriptionVM : RisItemUnitGroupDescription
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> UnitGroupId { get; set; }
            public string Description { get; set; }
            public string InsertedBy { get; set; }
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }

    [MetadataType(typeof(RisItemUnitGroupDescriptionItemVM.Metadata))]
    public class RisItemUnitGroupDescriptionItemVM : RisItemUnitGroupDescriptionItem
    {
        public string Category { get; set; }

        [Display(Name = "Stock/Prop No.")]
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "Qty")]
        public Nullable<int> QtyRequest { get; set; }        

        public string GridItems { get; set; }
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> UnitGroupDescriptionId { get; set; }
            public Nullable<System.Guid> RisItemId { get; set; }
            public string InsertedBy { get; set; }
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }            
        }
    }

    public class RisItemUnitGroupAvailableVM
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

    public class RisPrintVM 
    {
        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Required]
        [Display(Name = "As Of")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AsOfDate { get; set; }
        
    }
}