using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Ai.Models
{
    public class PurchaseRequestGridViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "PR No.")]
        public string PRNumber { get; set; }

        [Display(Name = "PR Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateTime? PRDate { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Purpose")]
        public string Purpose { get; set; }

        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        public int ItemsCount { get; set; }
    }
}