using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(ErrorLog.Metadata))]
    public partial class ErrorLog
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public string ErrorSource { get; set; }
            public string ErrorCode { get; set; }
            public string Description { get; set; }

            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string InsertedBy { get; set; }
        }
    }
}