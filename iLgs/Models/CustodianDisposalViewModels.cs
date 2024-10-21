using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(CustodianDisposal.Metadata))]
    public partial class CustodianDisposal
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> DeptId { get; set; }
            public string Department { get; set; }

            [Display(Name = "Transmittal No.")]
            public string TransmittalNo { get; set; }

            [Required]
            [Display(Name = "Requested By")]
            public string RequestedBy { get; set; }

            [Required]
            [Display(Name = "Requested Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> RequestedDt { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            [Display(Name = "Posted By")]
            public string PostedBy { get; set; }

            [Display(Name = "Posted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PostedDt { get; set; }
        }
    }

    [MetadataType(typeof(CustodianDisposalItem.Metadata))]
    public partial class CustodianDisposalItem
    {
        public string GridItems { get; set; }
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> CustodianDisposalId { get; set; }
            public Nullable<System.Guid> CustodianReportItemId { get; set; }
            public Nullable<System.Guid> CustodianReportLandItemId { get; set; }
            public Nullable<System.Guid> CustodianReportBldgItemId { get; set; }
            public string ArticleFields { get; set; }

            [Display(Name = "Article Display")]
            public string ArticleDisplay { get; set; }

            [Display(Name = "Estimated No. of Kg. (Metal)")]
            public Nullable<decimal> EstimatedKgMetals { get; set; }

            [Display(Name = "Estimated No. of Kg. (Others)")]
            public Nullable<decimal> EstimatedKgOthers { get; set; }

            [Display(Name = "Replacement Cost")]
            public Nullable<decimal> ReplCost { get; set; }

            [Display(Name = "Total Estimated Cost")]
            public Nullable<decimal> EstimatedCost { get; set; }

            [Display(Name = "Years in Service")]
            public Nullable<int> ServiceYear { get; set; }

            [Display(Name = "Accumulated Depreciation")]
            public Nullable<decimal> AccDep { get; set; }

            [Display(Name = "Disposal Value")]
            public Nullable<decimal> DisposalValue { get; set; }

            [Display(Name = "Disposal Value/Kg")]
            public Nullable<decimal> ValueKg { get; set; }

            [Display(Name = "Final Appraised Disposal Value")]
            public Nullable<decimal> FinalDisposalValue { get; set; }

            [Display(Name = "Salvage Value")]
            public Nullable<decimal> SalvageValue { get; set; }

            [Display(Name = "Acquisition Year")]
            public Nullable<int> AcqYear { get; set; }

            [Display(Name = "Estimated Useful Life")]
            public Nullable<int> EstLife { get; set; }

            [Display(Name = "Total Units")]
            public Nullable<int> TotalUnits { get; set; }

            [Display(Name = "Final Replacement Cost")]
            public Nullable<decimal> FinalReplCost { get; set; }

            [Display(Name = "Appraisal Rate")]
            public Nullable<decimal> AppraisalRate { get; set; }

            [Display(Name = "Acquisition Rate")]
            public Nullable<decimal> AcquisitionRate { get; set; }

            public Nullable<decimal> CFF { get; set; }
            public Nullable<decimal> CF { get; set; }
            public Nullable<decimal> UF { get; set; }

            [Display(Name = "AS")]
            public Nullable<int> AS_ { get; set; }

            [Display(Name = "L-AS")]
            public Nullable<int> L_AS { get; set; }

            public Nullable<int> R { get; set; }

            [Display(Name = "R/L")]
            public Nullable<decimal> R_L { get; set; }
            public Nullable<decimal> RUV { get; set; }
            public Nullable<decimal> D { get; set; }
            public Nullable<decimal> AF { get; set; }

            [Display(Name = "Version 1")]
            public Nullable<decimal> V1 { get; set; }

            [Display(Name = "Version 2")]
            public Nullable<decimal> V2 { get; set; }

            [Display(Name = "Version 3")]
            public Nullable<decimal> V3 { get; set; }

            [Display(Name = "Version 4")]
            public Nullable<decimal> V4 { get; set; }

            public Nullable<int> Answer { get; set; }                        
            
            [Display(Name = "Show Acquisition Month")]
            public Nullable<bool> ShowAcqMonth { get; set; }

            [Display(Name = "Show Acquisition Day")]
            public Nullable<bool> ShowAcqDay { get; set; }

            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
        }
    }

    public class AvailableCustodianItemVM
    {
        public System.Guid Id { get; set; }        
        public string Account { get; set; }

        [Display(Name = "Sub-Account")]
        public string SubAccount { get; set; }

        public string Article { get; set; }
        public string Description { get; set; }

        [Display(Name = "Property No.")]
        public string PsNo { get; set; }
                
    }
}