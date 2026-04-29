using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RsmiVM 
    {        
        public System.Guid Id { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> Date { get; set; }

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }

        public string Fund { get; set; }
        public string Custodian { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        

        [Display(Name = "Consumables Qty Issued")]
        public Nullable<int> QtyCons { get; set; }

        [Display(Name = "Consumables Amount")]
        public Nullable<decimal> AmountCons { get; set; }

        [Display(Name = "SPHV Qty Issued")]
        public Nullable<int> QtySPHV { get; set; }

        [Display(Name = "SPHV Amount")]
        public Nullable<decimal> AmountSPHV { get; set; }

        [Display(Name = "SPLV Qty Issued")]
        public Nullable<int> QtySPLV { get; set; }

        [Display(Name = "SPLV Amount")]
        public Nullable<decimal> AmountSPLV { get; set; }

        [Display(Name = "Total Semi-Expendable Qty")]
        public int? QtySE { get; set; }        

        [Display(Name = "Total Semi-Expendable Amount")]
        public decimal? AmountSE { get; set; }
        
        [Display(Name = "Total Qty Issued")]
        public Nullable<int> Qty { get; set; }
        
        [Display(Name = "Total Amount")]
        public Nullable<decimal> Amount { get; set; }        
    }

    public class RSMIProcessVM : IValidatableObject
    {
        [Display(Name = "Period From")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateFrom { get; set; }

        [Display(Name = "Period To")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateTo { get; set; }

        public string Fund { get; set; }
        public string Custodian { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateTo < DateFrom)
            {
                yield return new ValidationResult(string.Format("Period to {0} must be greater than or equal to Period From {1}", DateTo.Value.ToShortDateString(), DateFrom.Value.ToShortDateString()), new[] { "Period" });                
            }            
        }
    }

    public class RISListDto
    {
        public string RisNo { get; set; }
        public DateTime? Date { get; set; }
        public string Fund { get; set; }
        public string RCC { get; set; }
        public string Department { get; set; }
    }

    public class RSMIItemVM 
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RsmiId { get; set; }
        public Nullable<System.Guid> ItemCodeId { get; set; }
        public string ItemType { get; set; }

        [Display(Name = "RIS No.")]
        public string RisNo { get; set; }

        [Display(Name = "PO Reference No.")]
        public string PoNo { get; set; }

        [Display(Name = "Originating Department")]
        public string Department { get; set; }
        public string RCC { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }
        public string Location { get; set; }

        [Display(Name = "Item Code")]
        public string ItemCode { get; set; }

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }

        [Display(Name = "Item")]
        public string ItemName { get; set; }

        public string Unit { get; set; }

        [Display(Name = "Qty Issued")]
        public Nullable<int> Qty { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        public Nullable<decimal> Amount { get; set; }

        [Display(Name = "Account Code")]
        public string AccountCode { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transient

        public DateTime? Date { get; set; }
        public string Fund { get; set; }
    }

    public class RSMIRecapVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RsmiId { get; set; }

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }

        public Nullable<decimal> Qty { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> TotalCost { get; set; }

        [Display(Name = "Account Code")]
        public string AccountCode { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class RsmiPrintVM : IValidatableObject
    {
        [Display(Name = "Item Type")]
        public string RpciType { get; set; }
        public System.Guid Id { get; set; }
        public bool IsPosted { get; set; }

        [Display(Name = "Period From")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateFrom { get; set; }

        [Display(Name = "Period To")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateTo { get; set; }

        public bool SavePrints { get; set; }

        [Display(Name = "With Daily Recap")]
        public bool WithDailyRecap { get; set; } = true;

        public string Fund { get; set; }

        public string Custodian { get; set; }

        [Display(Name = "Report Type")]
        public string Type { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateTo < DateFrom)
            {
                yield return new ValidationResult(string.Format("Period to {0} must be greater than or equal to Period From {1}", DateTo.Value.ToShortDateString(), DateFrom.Value.ToShortDateString()), new[] { "Period" });
            }
        }
    }

    public class RSMITotalVM
    {
        public Guid? Id { get; set; }
        public string ItemType { get; set; }
        public string Account { get; set; }
        public string SubAccount { get; set; }
        public int? PoQty { get; set; }
        public int? QtyIssPast { get; set; }
        public int? QtyIss { get; set; }
        public int? QtyBalancePast { get; set; }
        public int? QtyBalance { get; set; }
        public Nullable<decimal> PoAmount { get; set; }
        public Nullable<decimal> IssPastAmount { get; set; }
        public Nullable<decimal> BalancePastAmount { get; set; }
        public Nullable<decimal> IssAmount { get; set; }
        public Nullable<decimal> BalanceAmount { get; set; }
        public int? QtyIssTotal { get; set; }
        public Nullable<decimal> IssTotalAmount { get; set; }
    }
}