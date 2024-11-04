using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(PsCardItemTransaction.Metadata))]
    public partial class PsCardItemTransaction
    {
        [Display(Name = "Transaction Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> TransDate { get; set; }

        public string Department { get; set; }

        [Display(Name = "Location Code")]
        public string LocCode { get; set; }
        public string Location { get; set; }
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> PsCardItemExtnId { get; set; }
            public Nullable<System.Guid> PsCardItemId { get; set; }
            public Nullable<System.Guid> PsCardItemIssuanceId { get; set; }
            public Nullable<System.Guid> PsCardItemTransferId { get; set; }
            public Nullable<System.Guid> IcsParId { get; set; }
            public string Remarks { get; set; }                        
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }
}