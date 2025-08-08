using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PsCardItemTransferVM 
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemId { get; set; }
        public Nullable<System.Guid> ParentId { get; set; }
        public Nullable<decimal> Qty { get; set; }

        [Display(Name = "Transit Date (mm/dd/yyyy)")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> TransDate { get; set; }

        [Display(Name = "Location Code")]
        [Required]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }

        public string Location { get; set; }

        [Display(Name = "Qty Issued")]
        public Nullable<decimal> QtyIss { get; set; }

        [Display(Name = "Qty Balance")]
        public Nullable<decimal> QtyBal { get; set; }

        [Display(Name = "Transit-In")]
        public Nullable<decimal> TransferIn { get; set; }

        [Display(Name = "Transit-Out")]
        public Nullable<decimal> TransferOut { get; set; }

        [Display(Name = "Trans. Type")]
        public string TranType { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transients
        public string SelectedIds { get; set; }
        public bool IsWithItemExtn { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO/Cut-off Date (mm/dd/yyyy)")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<DateTime> PoDate { get; set; }

        [Display(Name = "Originating PO Department")]
        public Nullable<Guid> DeptId { get; set; }

        [Display(Name = "Department Display")]
        public string DeptDisplay { get; set; }

        public string Unit { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Remaining Balance")]
        public Nullable<int> RemBalance { get; set; }

        [Display(Name = "Total Content")]
        public Nullable<int> TContentNo { get; set; }
    }

    public class PsCardItemTransferItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemTransferId { get; set; }
        public Nullable<System.Guid> PsCardItemExtnId { get; set; }
        public Nullable<System.Guid> IcsParItemId { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }

    public class PsCardItemTransferIssuanceVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemTransferId { get; set; }

        [Required]
        [Display(Name = "Issuance To")]
        public Nullable<System.Guid> LocationId { get; set; }        

        [Display(Name = "Department")]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedToCode { get; set; }

        [Display(Name = "\"Issuance To\" Reference")]
        public string IssuedToDescription { get; set; }

        [Display(Name = "Issued Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Required]
        public Nullable<decimal> Qty { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transients        
        public string Department { get; set; }

        [Display(Name = "\"Issuance To\" Reference")]
        public string Location { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public int? IssuedToSw { get; set; }

        public string SelectedIds { get; set; }
        public bool? IsWithItemExtn { get; set; } = false;
    }

    public class PsCardItemTransferIssuanceItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemTransferIssuanceId { get; set; }
        public Nullable<System.Guid> PsCardItemTransferItemId { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }
}