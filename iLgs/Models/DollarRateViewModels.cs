using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(DollarRate.Metadata))]
    public partial class DollarRate
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Required]
            [Display(Name = "As Of")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AsOf { get; set; }

            [Required]
            [Display(Name = "Exchange Rate")]
            public Nullable<decimal> Value { get; set; }

            [Display(Name = "Posted By")]
            public string PostedBy { get; set; }

            [Display(Name = "Date Posted")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PostedDt { get; set; }

            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }
}