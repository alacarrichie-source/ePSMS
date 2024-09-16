using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(PsCardItemExtnOther.Metadata))]
    public partial class PsCardItemExtnOther : PsCardItemExtn
    {
        [Display(Name = "Beginning Serial No.")]
        public string BegSerial { get; set; }

        [Display(Name = "Ending Serial No.")]
        public string EndSerial { get; set; }

        internal sealed class Metadata
        {
            [Display(Name = "Serial No.")]
            public string SerialNo { get; set; }
            public string Condition { get; set; }
        }
    }

    [MetadataType(typeof(PsCardItemExtnVehicle.Metadata))]
    public partial class PsCardItemExtnVehicle : PsCardItemExtn
    {
        internal sealed class Metadata
        {
            [Display(Name = "Series No.")]
            public Nullable<int> SeriesNo { get; set; }

            [Required]
            [Display(Name = "Year Model")]
            public Nullable<int> YearModel { get; set; }

            [Required]
            [Display(Name = "Plate No.")]
            public string PlateNo { get; set; }

            [Required]
            [Display(Name = "Body No.")]
            public string BodyNo { get; set; }

            [Display(Name = "Engine No.")]
            public string EngineNo { get; set; }

            [Display(Name = "Chasis No.")]
            public string ChasisNo { get; set; }
            public string Color { get; set; }

            [Required]
            [Display(Name = "CR No.")]
            public string CRN { get; set; }

            [Required]
            [Display(Name = "CR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CRDate { get; set; }

            [Required]
            [Display(Name = "MV File No.")]
            public string MVFileNo { get; set; }

            [Display(Name = "OR No.")]
            public string OrNo { get; set; }

            [Display(Name = "OR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> OrDate { get; set; }

            [Required]
            [Display(Name = "Net Weight")]
            public Nullable<int> NetWeight { get; set; }

            [Display(Name = "Insurance Policy No.")]
            public string InsPolicyNo { get; set; }
            public string ParReissuance { get; set; }
            public string Condition { get; set; }

            [Display(Name = "Sub location")]
            public string SubLocation { get; set; }

            [Required]
            [Display(Name = "Conduction Sticker No.")]
            public string ConductionNo { get; set; }
        }
    }

}