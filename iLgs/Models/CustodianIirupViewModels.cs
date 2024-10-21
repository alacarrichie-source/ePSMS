using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(CustodianIIRUP.Metadata))]    
    public partial class CustodianIIRUP
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Required]
            [Display(Name = "As Of")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AsOf { get; set; }

            public string Chairman { get; set; }

            [Display(Name = "Chairman Title")]
            public string ChairmanTitle { get; set; }

            [Display(Name = "Vice Chairman")]
            public string ViceChairman { get; set; }

            [Display(Name = "Vice Chairman Title")]
            public string ViceChairmanTitle { get; set; }

            public string Member { get; set; }

            [Display(Name = "Member Title")]
            public string MemberTitle { get; set; }

            [Display(Name = "Supply Officer")]
            public string Officer { get; set; }

            [Display(Name = "Supply Officer Title")]
            public string OfficerTitle { get; set; }

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

    [MetadataType(typeof(CustodianIirupItem.Metadata))]
    public partial class CustodianIirupItem
    {
        public string GridItems { get; set; }

        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> CustodianIirupId { get; set; }
            public Nullable<System.Guid> CustodianDisposalId { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }            
        }
    }

    public class CustodianIirupExportVM
    {
        public System.Guid Id { get; set; }

        [Display(Name = "As of Date")]
        public DateTime? AsOf { get; set; }
        public string Chairman { get; set; }
        public string ChairmanTitle { get; set; }
        public string ViceChairman { get; set; }
        public string ViceChairmanTitle { get; set; }
        public string Member { get; set; }
        public string MemberTitle { get; set; }
        public string Officer { get; set; }
        public string OfficerTitle { get; set; }
        public string Department { get; set; }
        public DateTime? RequestedDt { get; set; }
        public string Article { get; set; }
        public string Account { get; set; }
        public string SubAccount { get; set; }
        public string Brand { get; set; }

        [Display(Name = "Model")]
        public string Model_ { get; set; }
        public string Dimension { get; set; }
        public string Size { get; set; }
        public string Weight { get; set; }
        public string Materials { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
        public string OtherDesc { get; set; }
        public int? Qty { get; set; }
        public Decimal? UnitCost { get; set; }
        public string Code { get; set; }
        public string SerialNo { get; set; }
        public string BodyNo { get; set; }
        public string PlateNo { get; set; }
        public string EngineNo { get; set; }
        public string MVFileNo { get; set; }
        public int? EstimatedKg { get; set; }
        public Decimal? ReplacementCost { get; set; }
        public DateTime? AcqDate { get; set; }
    }
}