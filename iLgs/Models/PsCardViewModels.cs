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
            this.FieldsMedicine = new FieldsMedicine() { Id = this.Id };
            this.FieldsOther = new FieldsOther() { Id = this.Id };
            this.FieldsPpe = new FieldsPpe() { Id = this.Id };  
            this.FieldsVehicle = new FieldsVehicle() { Id = this.Id };
        }

        public System.Guid Id { get; set; }

        [Display(Name = "Article")]
        public Nullable<System.Guid> ItemCodeId { get; set; }
        public string Fund { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Card Category")]
        public string CardCategory { get; set; } // P or S only, to identify where the item belongs.

        [Display(Name = "Stock/Prop. No.")]
        public string PsNo { get; set; }
        
        [Display(Name = "Stock/Prop. Name")]
        public string PsName { get; set; }

        [Display(Name = "Prev. Stock/Prop. No.")]
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
        public FieldsMedicine FieldsMedicine { get; set; }
        public FieldsOther FieldsOther { get; set; }
        public FieldsPpe FieldsPpe { get; set; }
        public FieldsVehicle FieldsVehicle { get; set; }

        // Transients

        [Display(Name = "Article")]
        public string Item { get; set; }
        public string ItemCode { get; set; }

        [Display(Name = "Account")]
        public string ItemType { get; set; }
        public string ItemTypeCode { get; set; }

        [Display(Name = "Sub-acount")]
        public string SubAccount { get; set; }
        public string SubAccountCode { get; set; }        
    }

    public class PsCardItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "PO No.")]        
        public string PoNo { get; set; }

        [Display(Name = "AIR Date")]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]

        public Nullable<System.DateTime> AirDate { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [Display(Name = "Issuance Date")]
        ///[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AirIssueDate { get; set; }
        public Nullable<int> Qty { get; set; }

        [Display(Name = "Qty. Iss.")]
        public Nullable<int> QtyIss { get; set; }
        [Display(Name = "Qty. Bal.")]
        public Nullable<int> QtyBal { get; set; }

        [Display(Name = "Transaction Type")]
        public string TranType { get; set; }

        public string Unit { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public Nullable<int> Days { get; set; }
        public string Remarks { get; set; }

        [Display(Name = "Office")]
        public Nullable<System.Guid> DeptId { get; set; }        
        
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        
        // Transients
        [Display(Name = "Office")]
        public string Department { get; set; }
        
    }

    public class PsCardItemIssuanceVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemId { get; set; }
        public Nullable<System.Guid> RefIssuedId { get; set; }

        [Display(Name = "Location")]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Officer")]
        public Nullable<System.Guid> OfficerId { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }
        
        [Display(Name = "Issued Date")]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }
        public Nullable<int> Qty { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients
        public string Location { get; set; }
        public string Officer { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
    }
}