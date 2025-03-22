using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{

    [MetadataType(typeof(StockCardVM.Metadata))]
    public partial class StockCardVM : PsCardVM
    {
        internal sealed class Metadata
        {
            [Display(Name = "Stock Card No.")]
            [Required]
            public string PsNo { get; set; }                      
        }
    }

    [MetadataType(typeof(StockCardItemVM.Metadata))]
    public partial class StockCardItemVM : PsCardItemVM
    {
        internal sealed class Metadata
        {
            
        }        
    }    
}

//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Web;

//namespace iLgs.Models
//{
//    public class StockCardVM
//    {        
//        public System.Guid Id { get; set; }

//        [Display(Name = "Item")]
//        public Nullable<System.Guid> ItemCodeId { get; set; }
//        public string Fund { get; set; }
//        public string Description { get; set; }
//        public string Unit { get; set; }

//        [Display(Name = "Stock No.")]
//        public string StockNo { get; set; }

//        [Display(Name = "Stock Name")]
//        public string StockName { get; set; }

//        [Display(Name = "Reorder Point")]
//        public Nullable<decimal> ReorderPoint { get; set; }

//        [Display(Name = "Old Stock No.")]
//        public string PrevStockNo { get; set; }

//        [Display(Name = "Acquisition Date")]
//        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
//        public Nullable<System.DateTime> AcqDate { get; set; }

//        [Display(Name = "Acquisition Mode")]
//        public string AcqMode { get; set; }
//        public Nullable<decimal> Amount { get; set; }

//        [Display(Name = "Generic Name")]
//        public string GenericName { get; set; }

//        [Display(Name = "Dosage Strength")]
//        public string DosageStrength { get; set; }

//        [Display(Name = "Dosage From")]
//        public string DosageForm { get; set; }
//        public string Brand { get; set; }
//        public string Others { get; set; }

//        [Display(Name = "Other Description")]
//        public string OtherDesc { get; set; }
//        public string InsertedBy { get; set; }
//        public Nullable<System.DateTime> InsertedDt { get; set; }
//        public string UpdatedBy { get; set; }
//        public Nullable<System.DateTime> UpdatedDt { get; set; }

//        // Transients

//        [Display(Name = "Item")]
//        public string Item { get; set; }

//        [Display(Name = "Item Code")]
//        public string ItemCode { get; set; }

//        [Display(Name = "Item Type")]
//        public string ItemType { get; set; }

//        public string ItemTypeCode { get; set; }
//    }

//    public class StockItemVM
//    {        
//        public System.Guid Id { get; set; }
//        public Nullable<System.Guid> CardId { get; set; }
//        public Nullable<System.Guid> OrderItemId { get; set; }

//        [Display(Name = "Ref. Date")]
//        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
//        public Nullable<System.DateTime> RefDate { get; set; }

//        [Display(Name = "Ref No.")]
//        public string RefNo { get; set; }

//        [Display(Name = "Ref. Type")]
//        public string RefType { get; set; }
//        public Nullable<int> Qty { get; set; }

//        [Display(Name = "Qty. Issued")]
//        public Nullable<int> QtyIss { get; set; }

//        [Display(Name = "Qty. Balance")]
//        public Nullable<int> QtyBal { get; set; }
//        public Nullable<int> Days { get; set; }
//        public string Remarks { get; set; }

//        [Display(Name = "Unit Meas.")]
//        public string UnitMeas { get; set; }

//        [Display(Name = "Unit Cost")]
//        public Nullable<decimal> UnitCost { get; set; }
//        public Nullable<decimal> Amount { get; set; }
//        public string InsertedBy { get; set; }
//        public Nullable<System.DateTime> InsertedDt { get; set; }
//        public string UpdatedBy { get; set; }
//        public Nullable<System.DateTime> UpdatedDt { get; set; }
//    }

//    public class StockItemIssuanceVM
//    {
//        public System.Guid Id { get; set; }
//        public Nullable<System.Guid> StockItemId { get; set; }
//        public Nullable<System.Guid> RisIssuedId { get; set; }
//        public string Location { get; set; }

//        [Display(Name = "Issued To")]
//        public string IssuedTo { get; set; }

//        [Display(Name = "Accountable Officer")]
//        public string Officer { get; set; }

//        [Display(Name = "Issued Date")]
//        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
//        public Nullable<System.DateTime> IssuedDate { get; set; }

//        [Display(Name = "Issued By")]
//        public string IssuedBy { get; set; }

//        [Display(Name = "Qty. Iss.")]
//        public Nullable<int> Qty { get; set; }
//        public Nullable<decimal> Amount { get; set; }
//        public Nullable<System.DateTime> InsertedDt { get; set; }
//        public string InsertedBy { get; set; }
//        public Nullable<System.DateTime> UpdatedDt { get; set; }
//        public string UpdatedBy { get; set; }        
//    }

//    [MetadataType(typeof(PsCardItemExtnOther.Metadata))]
//    public partial class PsCardItemExtnOther : PsCardItemExtn
//    {
//        internal sealed class Metadata
//        {
//            [Display(Name = "Serial No.")]
//            public string SerialNo { get; set; }
//            public string Condition { get; set; }
//        }
//    }

//    [MetadataType(typeof(PsCardItemExtnVehicle.Metadata))]
//    public partial class PsCardItemExtnVehicle : PsCardItemExtn
//    {
//        internal sealed class Metadata
//        {
//            [Display(Name = "Series No.")]
//            public Nullable<int> SeriesNo { get; set; }

//            [Required]
//            [Display(Name = "Year Model")]
//            public Nullable<int> YearModel { get; set; }

//            [Required]
//            [Display(Name = "Plate No.")]
//            public string PlateNo { get; set; }

//            [Required]
//            [Display(Name = "Body No.")]
//            public string BodyNo { get; set; }

//            [Display(Name = "Engine No.")]
//            public string EngineNo { get; set; }

//            [Display(Name = "Chasis No.")]
//            public string ChasisNo { get; set; }
//            public string Color { get; set; }

//            [Required]
//            [Display(Name = "CR No.")]
//            public string CRN { get; set; }

//            [Required]
//            [Display(Name = "CR Date")]
//            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
//            public Nullable<System.DateTime> CRDate { get; set; }

//            [Required]            
//            [Display(Name = "MV File No.")]
//            public string MVFileNo { get; set; }

//            [Display(Name = "OR No.")]
//            public string OrNo { get; set; }

//            [Display(Name = "OR Date")]
//            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
//            public Nullable<System.DateTime> OrDate { get; set; }

//            [Required]
//            [Display(Name = "Net Weight")]
//            public Nullable<int> NetWeight { get; set; }
//            public string InsPolicyNo { get; set; }
//            public string ParReissuance { get; set; }
//            public string Condition { get; set; }
//            public string SubLocation { get; set; }

//            [Required]
//            [Display(Name = "Conduction Sticker No.")]
//            public string ConductionNo { get; set; }
//        }
//    }
//}

