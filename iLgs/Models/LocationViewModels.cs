using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class LocationCodePreviewVM
    {
        public Guid Id { get; set; }

        [Display(Name = "Code Index")]
        public string CodeIndex { get; set; }

        [Display(Name = "Code")]
        public string Code { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }

        [Display(Name = "Sub-Location")]
        public string SubLocation { get; set; }
    }

    public class LocationCodeVM
    {
        public Guid Id { get; set; }

        [Display(Name = "Code Index")]
        public string CodeIndex { get; set; }

        [Display(Name = "Code")]
        public string Code { get; set; }

        [Display(Name = "Location")]
        public string MainLocation { get; set; }

        [Display(Name = "Sub-Location")]
        public string SubLocation { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }
    }


    public class LocationBudgetVM
    {
        public System.Guid Id { get; set; }

        [Display(Name = "Location")]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Budget Code")]
        public Nullable<System.Guid> BudgetId { get; set; }
        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients

        [Display(Name = "Budget Code")]
        public string BudgetCode { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        public string Fund { get; set; }

        //public virtual Codextn Codextn { get; set; }
        //public virtual Codextn Codextn1 { get; set; }
    }
}