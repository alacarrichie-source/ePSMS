using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(Upload.Metadata))]
    public partial class Upload
    {
        public string Directory { get { return new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["UPLOAD_URL"].ToString()).DataSource; } }
        public int FileSize { get { return 100; } }
        //public string PathName { get { return (this.Directory + this.FileName).Replace("/", "\\"); } } 
        public string PathName { get { return this.Directory + this.FileName; } }
        public Guid? PsCardItemId { get; set; }
        
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> ImageId { get; set; }

            [Display(Name = "File Name")]
            public string FileName { get; set; }

            [Display(Name ="Document Type")]
            public string Description { get; set; }
            public string ServerIpAddress { get; set; }
            public string VirtualDirectory { get; set; }

            [Display(Name = "Uploaded By")]
            public string InsertedBy { get; set; }

            [Display(Name = "Upload Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }

            [Display(Name = "Updated By")]
            public string UpdatedBy { get; set; }

            [Display(Name = "Update Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }
}