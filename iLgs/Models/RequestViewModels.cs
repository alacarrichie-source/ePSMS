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

        [Display(Name = "RIS No.")]
        public Nullable<System.Guid> RisId { get; set; }        
        
        [Display(Name = "PR No.")]
        public string PrNo { get; set; }

        [Required]
        [Display(Name = "PR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PrDate { get; set; }        
        

        [Display(Name = "Cash Availability")]
        public string Availability { get; set; }

        [Display(Name = "Designation")]
        public string AvaialbilityDesig { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Designation")]
        public string ApprovedDesig { get; set; }

        [Display(Name = "Posted By")]
        public string SubmittedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> SubmittedDt { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }       

        public bool IsWithPO { get; set; }

        // TRANSIENTS 
        // From RIS

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> RisDate { get; set; }
        public string Fund { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string FPP { get; set; }
        public string Purpose { get; set; }

        [Display(Name = "Requested By")] // --> ApprovedBy From RIS
        public string RequestedBy { get; set; }

        [Display(Name = "Designation")]
        public string RequestedDesig { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }
    }

    public class RequestItemVM : RisItemCommonVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisItemId { get; set; }
        public Nullable<System.Guid> PrId { get; set; }

        [Required]
        public Nullable<decimal> Qty { get; set; }

        //[Required]
        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        //[Required]
        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }                
        
        public string GridRequestItemExtns { get; set; }
    }

    public class RequestItemExtnVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RequestItemId { get; set; }
        
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
        
        public bool IsEnabled { get; set; }
    }

    public class RequestItemUnitGroupVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PrId { get; set; }
        public Nullable<System.Guid> RisItemUnitGroupId { get; set; }        

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public RisItemUnitGroup RisItemUnitGroup { get; set; }

        // transients
        public string Unit { get; set; }
        public int? Qty { get; set; }
    }

    public class RequestItemUnitGroupDescriptionVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RequestItemUnitGroupId { get; set; }
        public Nullable<System.Guid> RisItemUnitGroupDescriptionId { get; set; }
        public string InsertedBy { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public RisItemUnitGroupDescription RisItemUnitGroupDescription { get; set; }
    }

    public class RequestItemUnitGroupDescriptionItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RequestItemUnitGroupDescriptionId { get; set; }
        public Nullable<System.Guid> RisItemUnitGroupDescriptionItemId { get; set; }
        public Nullable<System.Guid> RequestItemId { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<OrderItemUnitGroupDescriptionItem> OrderItemUnitGroupDescriptionItems { get; set; }
        //public RequestItem RequestItem { get; set; }
        //public virtual RequestItemUnitGroupDescription RequestItemUnitGroupDescription { get; set; }
        //public virtual RisItemUnitGroupDescriptionItem RisItemUnitGroupDescriptionItem { get; set; }

        //public System.Guid Id { get; set; }
        //public Nullable<System.Guid> UnitGroupDescriptionId { get; set; }
        //public Nullable<System.Guid> RisItemId { get; set; }
        //public string InsertedBy { get; set; }
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        //public Nullable<System.DateTime> InsertedDt { get; set; }
        //public string UpdatedBy { get; set; }
        //public Nullable<System.DateTime> UpdatedDt { get; set; }
        [Display(Name = "Stock/Prop No.")]
        public string PsNo { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Qty")]
        public Nullable<int> QtyRequest { get; set; }

        [Display(Name = "Price Rate")]
        public Nullable<decimal> PriceRate { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }

        public Nullable<decimal> GroupCost { get; set; }
        //public string GridItems { get; set; }
    }
}