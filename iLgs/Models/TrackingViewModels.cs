using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class TrackingVM : Tracking
    {
        [Display(Name = "Tracking Name")]
        public string TrackName { get; set; }
    }


    [MetadataType(typeof(Tracking.Metadata))]
    public partial class Tracking
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Display(Name = "Tracking Name")]
            public Nullable<System.Guid> CodeMastId { get; set; }

            [Display(Name = "Tracking No.")]
            public string TrackNo { get; set; }

            [Display(Name = "Tracking Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> TrackDate { get; set; }

            [Display(Name = "Tracking Record Id")]
            public Nullable<System.Guid> RefId { get; set; }

            public string Description { get; set; }

            [Display(Name = "Submitted By")]
            public string InsertedBy { get; set; }

            [Display(Name = "Submitted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }

            [Display(Name = "Updated By")]
            public string UpdatedBy { get; set; }

            [Display(Name = "Updated Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }

    public class TrackingItemVM : TrackingItem
    {
        public string Sequence { get; set; }
        public string Status { get; set; }
        public string Station { get; set; }
    }


    [MetadataType(typeof(TrackingItem.Metadata))]
    public partial class TrackingItem
    {        
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            public Nullable<System.Guid> TrackingId { get; set; }

            [Display(Name = "Status")]
            public Nullable<System.Guid> StatusId { get; set; }

            [Display(Name = "Check-In Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CheckInDt { get; set; }

            [Display(Name = "Check-In By")]
            public string CheckInBy { get; set; }

            [Display(Name = "Check-Out Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CheckOutDt { get; set; }

            [Display(Name = "Check-Out By")]
            public string CheckOutBy { get; set; }


            [Display(Name = "Return Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> ReturnDt { get; set; }

            [Display(Name = "Return By")]
            public string ReturnBy { get; set; }
        }
    }
}