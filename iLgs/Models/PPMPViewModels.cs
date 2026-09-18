using System;
using System.ComponentModel.DataAnnotations;

namespace iLgs.Models
{
    public class PPMPVM
    {
        public System.Guid Id { get; set; }
        public Nullable<int> ForYear { get; set; }
        public Nullable<System.Guid> DeptId { get; set; }
        public string PostedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }
        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transient

        public string Department { get; set; }
    }

    public class PPMPItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PpmpId { get; set; }
        public Nullable<int> RecNo { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public Nullable<int> Qty { get; set; }
        public string Unit { get; set; }
        public Nullable<decimal> EstBudget { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
        public string ProcMode { get; set; }
        public Nullable<int> Jan { get; set; }
        public Nullable<int> Feb { get; set; }
        public Nullable<int> Mar { get; set; }
        public Nullable<int> Apr { get; set; }
        public Nullable<int> May { get; set; }
        public Nullable<int> Jun { get; set; }
        public Nullable<int> Jul { get; set; }
        public Nullable<int> Aug { get; set; }
        public Nullable<int> Sep { get; set; }
        public Nullable<int> Oct { get; set; }
        public Nullable<int> Nov { get; set; }
        public Nullable<int> Dec { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients
        public string SelectedIds { get; set; }

        [Display(Name = "Qty. Used")]
        public Nullable<int> QtyUsed { get; set; }

        [Display(Name = "Qty. Balance")]
        public Nullable<int> QtyBal { get; set; }
        public PPMP PPMP { get; set; }
        public bool IsInCart { get; set; }
        public string Category { get; set; }
    }

    public class PPMPUploadVM
    {
        public Nullable<System.Guid> PpmpId { get; set; }
        public System.Guid Id { get; set; }

        [Required]
        [Display(Name = "Delete Existing Record?")]
        public string Delete { get; set; }

        [Required]
        [Display(Name = "Starting Row")]
        public int? StartRow { get; set; }

        //[Required]
        //[Display(Name = "Ending Row")]
        //public int? EndRow { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Required]
        [Display(Name = "General Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Qty")]
        public string Qty { get; set; }

        [Required]
        [Display(Name = "Size")]
        public string Size { get; set; }

        [Required]
        [Display(Name = "Estimated Budget")]
        public string Budget { get; set; }

        [Required]
        [Display(Name = "Mode of Procurement")]
        public string Mode { get; set; }

        [Display(Name = "January")]
        public string Jan{ get; set; }

        [Display(Name = "February")]
        public string Feb { get; set; }

        [Display(Name = "March")]
        public string Mar { get; set; }

        [Display(Name = "April")]
        public string Apr { get; set; }

        [Display(Name = "May")]
        public string May { get; set; }

        [Display(Name = "June")]
        public string Jun { get; set; }

        [Display(Name = "July")]
        public string Jul { get; set; }

        [Display(Name = "August")]
        public string Aug { get; set; }

        [Display(Name = "September")]
        public string Sep { get; set; }

        [Display(Name = "October")]
        public string Oct { get; set; }

        [Display(Name = "November")]
        public string Nov { get; set; }

        [Display(Name = "December")]
        public string Dec { get; set; }

        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class PPMPItemUsageVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PpmpItemId { get; set; }
        public Nullable<System.Guid> PrId { get; set; }
        public string Type { get; set; }
        public string Reference { get; set; }
        public Nullable<int> Qty { get; set; }
        public string InsertedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
        // Who is saving the PPMP Records.
        public string CallerId { get; set; }
    }
}