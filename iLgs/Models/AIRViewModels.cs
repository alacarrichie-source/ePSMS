using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class AIR_VM
    {
        public AIR_VM()
        {
            this.AIRInvoices = new List<AIRInvoice>();
        }

        public System.Guid Id { get; set; }

        [Display(Name = "PO No.")]
        public Nullable<System.Guid> OrderId { get; set; }
        public string Fund { get; set; }

        [Display(Name = "AIR No.")]
        public string AIRNo { get; set; }

        [Display(Name = "Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AIRDate { get; set; }

        [Display(Name = "Invoice No.")]
        //[Required]
        public string InvoiceNo { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required]
        public Nullable<System.DateTime> InvoiceDate { get; set; }

        [Display(Name = "Date Received")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcceptedDate { get; set; }

        [Display(Name = "Complete")]
        public Nullable<bool> IsComplete { get; set; }

        [Display(Name = "Partial")]
        public Nullable<bool> IsPartial { get; set; }

        public string Custodian { get; set; }

        public string Remarks { get; set; }

        [Display(Name = "Date Inspected")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InspectedDate { get; set; }

        [Display(Name = "Inspected")]
        public Nullable<bool> IsInspected { get; set; }

        [Display(Name = "Officer/Committee")]
        public string Officer { get; set; }

        [Display(Name = "Inventory/For Distribution")]
        public string InvDist { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public string PostedBy { get; set; }
        public Nullable<System.DateTime> PostedDt { get; set; }

        public ICollection<AIRInvoice> AIRInvoices { get; set; }

        // Transients

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        public string Supplier { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Requisitioning Office/Dept.")]
        public string Department { get; set; }

        public string Mode { get; set; }
    }

    public class AIRItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> AirId { get; set; }

        [Display(Name = "Stock/Property No.")]
        public Nullable<System.Guid> OrderItemId { get; set; }

        public Nullable<decimal> Qty { get; set; }

        //[Required]
        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // Transients

        [Display(Name = "Property/Stock No.")]
        public string PsNo { get; set; }

        [Display(Name = "Item")]
        public string PsItem { get; set; }

        [Display(Name = "Description")]
        public string OrderDescription { get; set; }

        [Display(Name = "Unit of Measurement")]
        public string PsUnit { get; set; }

        public string GridOrderItemExtns { get; set; }
        public string Mode { get; set; }
        public string PsType { get; set; }

        [Display(Name = "Area Sold/Donated")]

        public Nullable<decimal> AreaSoldDonated { get; set; }

        [Display(Name = "Construction Year")]
        public Nullable<int> ConstructionYear { get; set; }

        [Required]
        [Display(Name = "Inventory/For Distribution")]
        public string InvDist { get; set; }

        [Display(Name = "Inventory/For Distribution")]
        public string InvDistDesc { get { return this.InvDist == "I" ? "Inventory" : this.InvDist == "D" ? "For Distribution" : ""; } }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }
    }

    public class AIRInvoiceVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> AirId { get; set; }

        [Display(Name = "Invoice No.")]
        //[Required]
        public string InvoiceNo { get; set; }

        [Display(Name = "Invoice Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required]
        public Nullable<System.DateTime> InvoiceDate { get; set; }

        public Nullable<decimal> Amount { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    [MetadataType(typeof(AIRItemExtnVehicle.Metadata))]
    public partial class AIRItemExtnVehicle : AIRItemExtn
    {
        new internal sealed class Metadata
        {
            [Display(Name = "Group No.")]
            public string SetLotNo { get; set; }

            [Display(Name = "Group Qty No.")]
            public Nullable<int> SetLotQtyNo { get; set; }

            [Display(Name = "Item Qty No.")]
            public Nullable<int> ContentNo { get; set; }

            [Display(Name = "Series No.")]
            public string SeriesNo { get; set; }

            //[Required]
            [Display(Name = "Year Model")]
            public Nullable<int> YearModel { get; set; }

            //[Required]
            [Display(Name = "Plate No.")]
            public string PlateNo { get; set; }

            //[Required]
            [Display(Name = "Body No.")]
            public string BodyNo { get; set; }

            [Display(Name = "Engine No.")]
            public string EngineNo { get; set; }

            [Display(Name = "Chasis No.")]
            public string ChasisNo { get; set; }

            public string Color { get; set; }

            //[Required]
            [Display(Name = "CR No.")]
            public string CRN { get; set; }

            [Display(Name = "CR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CRDate { get; set; }

            //[Required]
            [Display(Name = "MV File No.")]
            public string MVFileNo { get; set; }

            [Display(Name = "OR No.")]
            public string OrNo { get; set; }

            [Display(Name = "OR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> OrDate { get; set; }

            //[Required]
            [Display(Name = "Net Weight")]
            public Nullable<int> NetWeight { get; set; }

            [Display(Name = "Insurance Policy No.")]
            public string InsPolicyNo { get; set; }

            [Display(Name = "Sub-Location")]
            public string SubLocation { get; set; }

            [Required]
            [Display(Name = "Conduction Sticker No.")]
            public string ConductionNo { get; set; }

            public AIRItem AIRItem { get; set; }
        }
    }

    [MetadataType(typeof(AIRItemExtnBuilding.Metadata))]
    public partial class AIRItemExtnBuilding : AIRItemExtn
    {
        new internal sealed class Metadata
        {                        
            public string Address { get; set; }

            [Display(Name = "Project Name")]
            public string ProjectName { get; set; }

            [Display(Name = "Building Type")]
            public string BuildingType { get; set; }

            [Display(Name = "Area (sqm)")]
            public Nullable<decimal> Area { get; set; }

            [Display(Name = "Total Amount")]
            public Nullable<decimal> TotalAmount { get; set; }

            [Display(Name = "Phase Amount")]
            public Nullable<decimal> PhaseAmountMooe { get; set; }

            [Display(Name = "Start Year")]
            public Nullable<int> StartYear { get; set; }

            [Display(Name = "Start Month")]
            public Nullable<int> StartMont { get; set; }

            [Display(Name = "Target Year")]
            public Nullable<int> TargetYear { get; set; }

            [Display(Name = "Target Month")]
            public Nullable<int> TargetMonth { get; set; }

            [Display(Name = "Percent Complete")]
            public Nullable<decimal> PercentComplete { get; set; }

            [Display(Name = "Completion Year")]
            public Nullable<int> CompletionYear { get; set; }

            [Display(Name = "Completion Month")]
            public Nullable<int> CompletionMonth { get; set; }

            public string Status { get; set; }
            public string Condition { get; set; }            
            public AIRItem AIRItem { get; set; }
        }
    }

    [MetadataType(typeof(AIRItemExtnLand.Metadata))]
    public partial class AIRItemExtnLand : AIRItemExtn
    {
        new internal sealed class Metadata
        {            
            public string PIN { get; set; }
            public string Address { get; set; }
            public string LandMarks { get; set; }
            public Nullable<decimal> MarketValue { get; set; }
            public Nullable<decimal> PricePerSqm { get; set; }
            public Nullable<decimal> OldAmounts { get; set; }
            public string TctNo { get; set; }
            public string OldTctNo { get; set; }
            public string DRPNo { get; set; }
            public Nullable<System.DateTime> DRPDate { get; set; }
            public string OldDRPNo { get; set; }
            public Nullable<System.DateTime> OldDRPDate { get; set; }
            public Nullable<decimal> CGT { get; set; }
            public Nullable<decimal> CGTTransferTax { get; set; }
            public Nullable<decimal> CGTSurcharge { get; set; }
            public Nullable<decimal> CGTInterest { get; set; }
            public Nullable<decimal> CGTCompromise { get; set; }
            public Nullable<decimal> DST { get; set; }
            public Nullable<decimal> DSTTransferTax { get; set; }
            public Nullable<decimal> DSTSurcharge { get; set; }
            public Nullable<decimal> DSTInterest { get; set; }
            public Nullable<decimal> TransferTax { get; set; }
            public Nullable<decimal> Surcharge { get; set; }
            public Nullable<decimal> Interest { get; set; }
            public Nullable<decimal> ConfirmationFee { get; set; }
            public Nullable<decimal> TranferRegsFee { get; set; }
            public Nullable<decimal> RealPropertyTax { get; set; }
            public Nullable<decimal> VAT { get; set; }
            public Nullable<decimal> EstateTax { get; set; }
            public Nullable<decimal> Titling { get; set; }
            public Nullable<decimal> CertificationFee { get; set; }
            public Nullable<decimal> Relocation { get; set; }
            public Nullable<decimal> Surveying { get; set; }
            public Nullable<decimal> IncidentalExpense { get; set; }
            public string CapitalOutlayOrExpense { get; set; }            
            public AIRItem AIRItem { get; set; }
        }
    }

    [MetadataType(typeof(AIRItemExtnOther.Metadata))]
    public partial class AIRItemExtnOther : AIRItemExtn
    {
        new internal sealed class Metadata
        {
            [Display(Name = "Group No.")]
            public string SetLotNo { get; set; }

            [Display(Name = "Group Qty No.")]
            public Nullable<int> SetLotQtyNo { get; set; }

            [Display(Name = "Item Qty No.")]
            public Nullable<int> ContentNo { get; set; }

            [Display(Name = "Serial No.")]
            public string SerialNo { get; set; }

            [Display(Name = "Condition")]
            public string Condition { get; set; }
            public AIRItem AIRItem { get; set; }

        }
    }

    [MetadataType(typeof(AIRItemExtn.Metadata))]
    public partial class AIRItemExtn
    {
        internal sealed class Metadata
        {            
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> AIRItemId { get; set; }

            //[Display(Name = "Group No.")]
            //public string SetLotNo { get; set; }

            //[Display(Name = "Group Qty No.")]
            //public Nullable<int> SetLotQtyNo { get; set; }

            //[Display(Name = "Item Qty No.")]
            //public Nullable<int> ContentNo { get; set; }

            [Display(Name = "Custodian Item No.")]
            public Nullable<int> CustItemNo { get; set; }

            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
            
        }
    }
}