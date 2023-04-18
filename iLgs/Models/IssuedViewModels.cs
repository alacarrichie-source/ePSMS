using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class IssuedVM
    {        
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> OrderId { get; set; }
        
        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }

        [Required]
        public string Fund { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> Date { get; set; }
        [Display(Name = "Certified By")]
        public string CertifiedBy { get; set; }

        [Display(Name = "Date Certified")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> CertifiedDate { get; set; }
        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Date Posted")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDate { get; set; }
        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }        

        // transients
        //public string SerialNo_ { get { return SerialYear + "-" + SerialMonth + "-" + SerialSeries; } }
    }

    public class IssuedItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> IssuedId { get; set; }
        public Nullable<System.Guid> RISId { get; set; }
        
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // TRANSIENTS

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }
        public string Item { get; set; }
        public string Unit { get; set; }                        

        [Display(Name = "RIS No.")]        
        public string RISNo { get; set; }

        [Display(Name = "Responsibility Center Code")]
        public string RCCode { get; set; }

        public Nullable<decimal> Qty { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        public Nullable<decimal> Amount { get; set; }
    }
}