using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PAR_VM
    {        
        public System.Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public Nullable<System.Guid> OrderId { get; set; }

        [Display(Name = "PAR No.")]
        public string ParNo { get; set; }

        [Display(Name = "PAR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ParDate { get; set; }

        [Display(Name = "Received By")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Position")]
        public string ReceivedByPosition { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ReceivedDate { get; set; }

        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; }

        [Display(Name = "Position")]
        public string IssuedByPosition { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> PostedDt { get; set; }

        // Transients

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? PoDate { get; set; }

        public string Fund { get; set; }

    }

    public class PARItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> ParId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }
        public Nullable<int> Qty { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public string Unit { get; set; }
        public Nullable<decimal> Amount { get; set; }
        [Display(Name = "Prop No.")]
        public string PsNo { get; set; }

        [Display(Name = "Date Acquired")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? DateAcquired { get; set; }

        public string Description { get; set; }

        // Transients

        [Display(Name = "Item")]
        public string PsItem { get; set; }

        [Display(Name = "Description")]
        public string OrderDescription { get; set; }

        [Display(Name = "Unit")]
        public string PsUnit { get; set; }

        public string GridOrderItemExtns { get; set; }
        public string Mode { get; set; }
        public Guid? PsCodeId { get; set; }

    }


    public class PARAcknowledgementVM
    {
        public Guid ParId { get; set; }
        public Guid ParItemId { get; set; }
        [Display(Name = "PAR No.")]
        public string ParNo { get; set; }

        [Required]
        [Display(Name = "PAR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ParDate { get; set; }
        public Nullable<System.Guid> OrderId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Required]
        public Nullable<int> Qty { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public string Unit { get; set; }
        public Nullable<decimal> Amount { get; set; }

        [Display(Name = "Prop No.")]
        public string PsNo { get; set; }

        [Display(Name = "Date Acquired")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? DateAcquired { get; set; }

        public string Description { get; set; }

        
        [Display(Name = "Item")]
        public string PsItem { get; set; }

        [Display(Name = "Description")]
        public string OrderDescription { get; set; }

        [Display(Name = "Unit")]
        public string PsUnit { get; set; }

        [Display(Name = "Received By")]
        public string ReceivedBy { get; set; }

        [Display(Name = "Position")]
        public string ReceivedByPosition { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ReceivedDate { get; set; }

        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; }

        [Display(Name = "Position")]
        public string IssuedByPosition { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        public Nullable<System.DateTime> PostedDt { get; set; }
        public string Mode { get; set; }

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }
    }

    public class GenerateParVM
    {
        public Nullable<System.Guid> OrderItemId { get; set; }

        [Display(Name = "PAR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> ParDate { get; set; }
        
        //[Required]
        public Nullable<int> Qty { get; set; }
        
    }
}