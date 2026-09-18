using System;
using System.ComponentModel.DataAnnotations;

namespace iLgs.Models
{
    public class PpmpAcctgCodeVM
    {
        public Guid? Id { get; set; }

        [Display(Name = "PPMP Code")]
        public string Code { get; set; }

        [Display(Name = "Description / Group Name")]
        public string Description { get; set; }

        [Display(Name = "GSO Code")]
        public Guid? GSOCodeId { get; set; }

        [Display(Name = "GSO Code")]
        public string GSOCodeDescription { get; set; }

        [Display(Name = "Accounting Code")]
        public string AcctgCode { get; set; }

        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        public bool HasMapping { get; set; }

        public string Status
        {
            get
            {
                return HasMapping ? "Assigned" : "Unassigned";
            }
        }
    }
}
