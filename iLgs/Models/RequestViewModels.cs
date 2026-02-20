using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RequestVM
    {        
        public System.Guid Id { get; set; }

        //[Required]
        //[Display(Name = "RIS No.")]        
        //public Nullable<System.Guid> RisId { get; set; }        

        [Display(Name = "Ctrl. No.")]
        public string CtrlNo { get; set; }

        [Display(Name = "PR No.")]
        public string PrNo { get; set; }

        //[Required]
        [Display(Name = "PR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PrDate { get; set; }

        [Required]
        [Display(Name = "Cash Availability")]
        public string Availability { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string AvaialbilityDesig { get; set; }

        [Required]
        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string ApprovedDesig { get; set; }

        [Display(Name = "Submitted By")]
        public string SubmittedBy { get; set; }

        [Display(Name = "Submitted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> SubmittedDt { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }       

        public bool IsWithPO { get; set; }

        // TRANSIENTS 
        // From RIS

        //[Required]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Display(Name = "RIS Date")]
        //public Nullable<System.DateTime> RisDate { get; set; }

        [Required]
        public string Fund { get; set; }

        [Display(Name = "Specific")]
        public string FundSpecific { get; set; }

        [Required]
        [Display(Name = "Department")]
        public Nullable<System.Guid> DeptId { get; set; }

        [Required]
        [Display(Name = "Department Display")]
        public string Department { get; set; }
        public string Section { get; set; }

        [Required]
        public string FPP { get; set; }

        [Required]
        public string Purpose { get; set; }

        [Required]
        [Display(Name = "Requested By")] // --> ApprovedBy From RIS
        public string RequestedBy { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string RequestedDesig { get; set; }

        public Nullable<System.DateTime> ApprovedDate { get; set; }

        //[Display(Name = "RIS No.")]
        //public string RisNo { get; set; }
    }

    public class RequestItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PrId { get; set; }

        [Display(Name = "Item No.")]
        public string ItemNo { get; set; }
        
        //[Required]
        public string Description { get; set; }
        public string Remarks { get; set; }

        //[Required]
        public Nullable<decimal> Qty { get; set; }

        //[Required]
        public string Unit { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }

        [Display(Name = "Price Rate")]
        public Nullable<decimal> PriceRate { get; set; }

        [Display(Name = "PPMP Code")]
        public string PpmpCode { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients

        public string SetLotNo { get; set; }
        public string ItemNoIndex { get; set; }

        public int? Padding { get; set; }

        //public string GridRequestItemExtns { get; set; }
    }    

    public class RequestItemUnitGroupVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PrId { get; set; }
        //public Nullable<System.Guid> RisItemUnitGroupId { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        //public RisItemUnitGroup RisItemUnitGroup { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "Qty per Set/Lot")]
        public int? Qty { get; set; }
    }

    [MetadataType(typeof(RisItemUnitGroup.Metadata))]
    public partial class RisItemUnitGroup
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
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }

    public class RequestItemUnitGroupDescriptionVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RequestItemUnitGroupId { get; set; }
        //public Nullable<System.Guid> RisItemUnitGroupDescriptionId { get; set; }
        public string Description { get; set; }

        [Display(Name = "Other Particulars")]
        public string OtherParticulars { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        //public RisItemUnitGroupDescription RisItemUnitGroupDescription { get; set; }
    }

    public class RequestItemUnitGroupDescriptionItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RequestItemUnitGroupDescriptionId { get; set; }
        //public Nullable<System.Guid> RisItemUnitGroupDescriptionItemId { get; set; }
        public Nullable<System.Guid> RequestItemId { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        
        public string Category { get; set; }
        [Display(Name = "Stock/Prop No.")]
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "Qty per Set/Lot")]
        public Nullable<int> QtyRequest { get; set; }

        [Display(Name = "Price Rate")]
        public Nullable<decimal> PriceRate { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }       

        public Nullable<decimal> GroupUnitCost { get; set; }
        public Nullable<decimal> GroupTotalCost { get; set; }

        [Display(Name = "Qty of Sets/Lots")]
        public Nullable<int> GroupQty { get; set; }
        //public string GridItems { get; set; }
    }
}