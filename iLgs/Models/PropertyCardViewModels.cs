using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{        
    public class PropertyCardVM
    {                
        public System.Guid Id { get; set; }

        [Display(Name = "Item")]
        public Nullable<System.Guid> ItemCodeId { get; set; }
        public string Fund { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Property No.")]
        public string PropNo { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Old Property No.")]
        public string PrevPropNo { get; set; }

        [Display(Name = "Acquisition Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcqDate { get; set; }

        [Display(Name = "Acquisition Mode")]
        public string AcqMode { get; set; }
        public Nullable<decimal> Amount { get; set; }


        // Transients

        [Display(Name = "Item")]
        public string Item { get; set; }

        [Display(Name = "Item Code")]
        public string ItemCode { get; set; }

        [Display(Name = "Item Type")]
        public string ItemType { get; set; }

        public string ItemTypeCode { get; set; }

        //public ICollection<EquipmentFieldReference> EquipmentFieldReferences { get; set; }

    }


    //public class EquipmentFieldVM
    //{
    //    public EquipmentFieldVM()
    //    {
    //        this.EquipmentFieldReferences = new HashSet<EquipmentFieldReferenceVM>();
    //    }

    //    public System.Guid Id { get; set; }
    //    public string Type { get; set; }
    //    public string Brand { get; set; }
    //    public string Model_ { get; set; }
    //    public string SerialNo { get; set; }
    //    public string Others { get; set; }
    //    public string Color { get; set; }

    //    public ICollection<EquipmentFieldReferenceVM> EquipmentFieldReferences { get; set; }
    //}

    //public class EquipmentFieldReferenceVM
    //{
    //    public System.Guid Id { get; set; }
    //    public Nullable<System.Guid> EquipFieldId { get; set; }
    //    public Nullable<System.Guid> RefId { get; set; }

    //    public PropertyCardVM PropertyCard { get; set; }
    //    //public RisItem RisItem { get; set; }
    //    public EquipmentFieldVM EquipmentField { get; set; }
    //}

    public class PropertyCardVehicleVM : PropertyCardVM
    {
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

    public class PropertyCardPpeVM : PropertyCardVM
    {
        public string Type { get; set; }            
        public string Brand { get; set; }
        [Display(Name = "Model")]
        public string Model_ { get; set; }
        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }
        public string Others { get; set; }
        public string Color { get; set; }
    }

    //public class PropertyCardTranspoVM : PropertyCardVM
    //{
    //    public string EngineNo { get; set; }
    //    public string Brand { get; set; }
    //    [Display(Name = "Model")]
    //    public string Model_ { get; set; }
    //    [Display(Name = "Serial No.")]
    //    public string SerialNo { get; set; }
    //    public string Others { get; set; }
    //    public string Color { get; set; }
    //}

    //public class PropertyCardPpeFields
    //{
    //    public string Type { get; set; }
    //    public string Brand { get; set; }
    //    public string Model_ { get; set; }
    //    [Display(Name = "Serial No.")]
    //    public string SerialNo { get; set; }
    //    public string Others { get; set; }
    //    public string Color { get; set; }
    //}

    public class PropertyCardItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> CardId { get; set; }

        [Display(Name = "Ref. Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RefDate { get; set; }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        [Display(Name = "Ref. Type")]
        public string RefType { get; set; }

        [Display(Name = "Qty. Receipt")]
        public Nullable<int> QtyRec { get; set; }

        [Display(Name = "Qty")]
        public Nullable<int> Qty { get; set; }

        [Display(Name = "Ref. Mode")]
        public string TransType { get; set; }

        [Display(Name = "Qty. Balance")]
        public Nullable<int> QtyBal { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string Remarks { get; set; }
        public string Location { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }

        [Display(Name = "Accountable Officer")]
        public string Officer { get; set; }

        [Display(Name = "Prev. Accountable Officer")]
        public string PrevOfficer { get; set; }

        [Display(Name = "Prev. Ref. No.")]
        public string PrevRefNo { get; set; }

        [Display(Name = "Prev. Ref. Type")]
        public string PrevRefType { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class CardItemExtnVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> CardId { get; set; }
        public string ItemNo { get; set; }
        public string ItemKey { get; set; }
        public string ItemValue { get; set; }
        public Nullable<int> Sequence { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }
}