using System;
using System.ComponentModel.DataAnnotations;

namespace iLgs.Models
{
    public class PoAdjustmentVM
    {
        public System.Guid Id { get; set; }

        //[Required]
        [Display(Name = "For Year")]
        public Nullable<int> ForYear { get; set; }

        //[Required]
        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Required]
        public string Remarks { get; set; }

        [Display(Name = "Is Ok?")]
        public Nullable<bool> IsOk { get; set; }

        [Display(Name = "Inserted By")]
        public string InsertedBy { get; set; }

        [Display(Name = "Inserted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }

        [Display(Name = "Updated Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transients

        [Display(Name = "Is Ok?")]
        public string IsOkay { get; set; }
    }
}