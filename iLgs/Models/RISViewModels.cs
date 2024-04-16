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
        public string Fund { get; set; }
        public string Division { get; set; }
        public string Office { get; set; }
        public string FPP { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Display(Name = "RIS Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RisDate { get; set; }
        public string Purpose { get; set; }

        [Display(Name = "Requested by")]
        public string RequestedBy { get; set; }

        [Display(Name = "Designation")]
        public string RequestedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> RequestedDate { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Designation")]
        public string ApprovedByDesignation { get; set; }

        [Display(Name = "Date")]
        public Nullable<System.DateTime> ApprovedDate { get; set; }

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
        public string Description { get; set; }

        [Display(Name = "Other Description")]
        public string OtherDesc { get; set; } 

        [Display(Name = "Stock/Property No.")]
        public string PsNo { get; set; } // Generic (Without Brand)        

        public string PsNoDisplay { get; set; } // For printing 

        [Display(Name = "Item Name")]
        public string ItemName { get; set; }

        [Display(Name = "Category Code")]
        public string PsType { get; set; } // Used as category

        [Display(Name = "Category Name")]
        public string PsTypeDesc { get; set; } // Used as category description

        [Display(Name = "Item Code")]
        public string ItemCode { get; set; } // Code of ItemCodeId

        [Display(Name = "Item")]
        public string ItemType { get; set; } // Description of ItemCodeId

        public string Unit { get; set; }
        public Nullable<decimal> PriceRate { get; set; }
    }

    public class RisItemVM : RisItemCommonVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisId { get; set; }
        [Display(Name = "Item")]
        public Nullable<System.Guid> ItemCodeId { get; set; }        

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
            this.RisItemMedicine = new RisItemMedicine();
            this.RisItemVehicle = new RisItemVehicle();
            this.RisItemPpe = new RisItemPpe();
        }

        public RisItemMedicine RisItemMedicine { get; set; }        
        public RisItemVehicle RisItemVehicle { get; set; }
        public RisItemPpe RisItemPpe { get; set; }
        
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

        [Display(Name = "Issued Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; }

        [Display(Name = "Designation")]
        public string IssuedByDesignation { get; set; }

        public Nullable<int> Qty { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> Amount { get; set; }
        
        [Display(Name = "Issued To")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Designation")]
        public string ReceivedByDesignation { get; set; }

        [Display(Name = "Received Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ReceivedDate { get; set; }

        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }            
        public string Department { get; set; }
    }

    public class RisItemUnitGroupVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisId { get; set; }
        public Nullable<int> Qty { get; set; }
        public string Unit { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }

    public class RisItemUnitGroupDescriptionVM
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

    public class RisItemUnitGroupDescriptionItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> UnitGroupDescriptionId { get; set; }
        public Nullable<System.Guid> RisItemId { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        [Display(Name = "Stock/Prop No.")]
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public Nullable<int> QtyRequest { get; set; }        
        public string GridItems { get; set; }        
    }

    public class RisItemUnitGroupAvailableVM
    {
        public System.Guid Id { get; set; }
        [Display(Name = "Stock/Prop No.")]
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public Nullable<int> QtyRequest { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
    }
}