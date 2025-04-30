using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PsCardItemExtnVM : PsCardItemExtn
    {
        public string Fund { get; set; }
        public string Account { get; set; }

        [Display(Name = "Sub-Account")]
        public string SubAccount { get; set; }

        public string Article { get; set; }

        public string Description { get; set; }

        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "PO Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Location Code")]
        public string DeptCode { get; set; }
        public string Department { get; set; }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        [Display(Name = "P/S Card No.")]
        public string PsNo { get; set; }

        [Display(Name = "ICS/PAR Count")]
        public Nullable<int> IcsParCount { get; set; }

        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Set/Lot Unit Cost")]
        public Nullable<decimal> SetUnitCost { get; set; }
    }

    [MetadataType(typeof(PsCardItemExtn.Metadata))]
    public partial class PsCardItemExtn
    {
        public Nullable<System.Guid> TransferId { get; set; }
        public Nullable<System.Guid> TransferItemId { get; set; }
        [Display(Name = "Location")]
        public string Location { get; set; }
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> GroupId { get; set; }
            public Nullable<System.Guid> PsCardItemId { get; set; }
            public Nullable<System.Guid> AIRItemExtnId { get; set; }

            [Display(Name = "Group No.")]
            public string SetLotNo { get; set; }

            [Display(Name = "Set/Lot No.")]
            public Nullable<int> SetLotQtyNo { get; set; }

            [Display(Name = "Item Qty No.")]
            public Nullable<int> ContentNo { get; set; }

            [Display(Name = "Custodian Item No.")]
            public Nullable<int> CustItemNo { get; set; }

            [Display(Name = "Location Code")]
            public Nullable<System.Guid> LocationId { get; set; }

            [Display(Name = "Property No.")]
            public string PropNo { get; set; }
            public string PropYear { get; set; }
            public string PropSeq { get; set; }
            public string SeriesNo { get; set; }
            public string Remarks { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }

    public class PsCardItemExtnCommonVM
    {
        public System.Guid Id { get; set; } // Id of PsCardItemTransferItem, Id of PsCardItemExtn is PsCardItemExtnId (Transit)
        public Nullable<System.Guid> PsCardItemId { get; set; }
        public Nullable<System.Guid> AIRItemExtnId { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set/Lot No.")]
        public Nullable<int> SetLotQtyNo { get; set; }

        [Display(Name = "Item Qty No.")]
        public Nullable<int> ContentNo { get; set; }

        public Nullable<int> TContentNo { get; set; } // based on PsCardItem Qty && SetLotQty of UnitGroup

        [Display(Name = "Custodian Item No.")]
        public string CustItemNo { get; set; }

        [Display(Name = "Location Code")]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Property No.")]
        public string PropNo { get; set; }
        public string PropYear { get; set; }
        public string PropSeq { get; set; }

        [Display(Name = "Series No.")]
        public string SeriesNo { get; set; }
        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public string Condition { get; set; }

        [Display(Name = "Sub-Location")]
        public string SubLocation { get; set; }
        public string Annex { get; set; }

        [Display(Name = "Old Prop. No.")]
        public string OldPropNo { get; set; }

        [Display(Name = "Upcoming Officer")]
        public string UpcomingOfficer { get; set; }

        // Transients 

        public Guid? TransferId { get; set; }
        public Guid? PsCardItemExtnId { get; set; }
        
        [Display(Name = "Location")]
        public string Location { get; set; }
    }

    public class PsCardItemExtnOtherVM : PsCardItemExtnCommonVM
    {

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }
    }

    public class PsCardItemExtnVehicleVM : PsCardItemExtnCommonVM
    {

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

        //[Required]
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

        //[Display(Name = "PAR Reissuance")]
        //public string ParReissuance { get; set; }

        [Required]
        [Display(Name = "Conduction Sticker No.")]
        public string ConductionNo { get; set; }
    }

    [MetadataType(typeof(PsCardItemExtnOther.Metadata))]
    public partial class PsCardItemExtnOther : PsCardItemExtn
    {
        [Display(Name = "Beginning Serial No.")]
        public string BegSerial { get; set; }

        [Display(Name = "Ending Serial No.")]
        public string EndSerial { get; set; }

        new internal sealed class Metadata
        {
            [Display(Name = "Serial No.")]
            public string SerialNo { get; set; }
            public string Condition { get; set; }
        }
    }

    public class PsCardItemExtnBldgVM : PsCardItemExtnCommonVM
    {
        [Display(Name = "RPCPPE Acquisition Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcqDate { get; set; }

        [Display(Name = "Construction Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> StartDate { get; set; }

        [Display(Name = "Target Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> TargetDate { get; set; }

        [Display(Name = "Completion Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> CompletionDate { get; set; }

        public string Address { get; set; }

        [Display(Name = "Project Name")]
        public string ProjectName { get; set; }

        [Display(Name = "Bldg. Type")]
        public string BldgType { get; set; }

        [Display(Name = "Appraised Value")]
        public Nullable<decimal> AppraisedValue { get; set; }

        [Display(Name = "Total Amount")]
        public Nullable<decimal> TotalAmount { get; set; }

        [Display(Name = "Phase Amount MOOE")]
        public Nullable<decimal> PhaseAmountMooe { get; set; }
        

        [Display(Name = "Percent Complete")]
        public Nullable<decimal> PercentComplete { get; set; }        

        public string Status { get; set; }

        public Nullable<double> Latitude { get; set; }
        public Nullable<double> Longitude { get; set; }

        // Transients

        [Display(Name = "Year of Acquisition")]
        public Nullable<int> AcqYear { get; set; }
        
        [Display(Name = "Year of Construction")]
        public Nullable<int> StartYear { get; set; }

        [Display(Name = "Target Year")]
        public Nullable<int> TargetYear { get; set; }

        [Display(Name = "Target Month")]
        public Nullable<int> TargetMonth { get; set; }
    }


    public class PsCardItemExtnLandVM : PsCardItemExtnCommonVM
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

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DRPDate { get; set; }
        public string OldDRPNo { get; set; }

        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> OldDRPDate { get; set; }
        public Nullable<decimal> CGT { get; set; }

        public Nullable<decimal> CGTTransferTax { get; set; }

        public Nullable<decimal> CGTSurcharge { get; set; }

        public Nullable<decimal> CGTInteest { get; set; }

        public Nullable<decimal> CGTCompromise { get; set; }

        public Nullable<decimal> DST { get; set; }

        public Nullable<decimal> DSTTransferTax { get; set; }

        public Nullable<decimal> DSTSurcharge { get; set; }

        public Nullable<decimal> DSTInterest { get; set; }

        public Nullable<decimal> DSTCompromise { get; set; }

        public Nullable<decimal> TransferTax { get; set; }

        public Nullable<decimal> Surcharge { get; set; }

        public Nullable<decimal> Interest { get; set; }

        public Nullable<decimal> ConfirmationFee { get; set; }

        public Nullable<decimal> TransferRegsFee { get; set; }

        public Nullable<decimal> RealPropertyTax { get; set; }

        public Nullable<decimal> VAT { get; set; }

        public Nullable<decimal> EstateTax { get; set; }

        public Nullable<decimal> Titling { get; set; }

        public Nullable<decimal> CerttificationFee { get; set; }

        public Nullable<decimal> Relocation { get; set; }

        public Nullable<decimal> Surveying { get; set; }

        public Nullable<decimal> IncidentalExpenses { get; set; }

        public Nullable<decimal> CapitalOutlayOrExpense { get; set; }
    }

    [MetadataType(typeof(PsCardItemExtnVehicle.Metadata2))]
    public partial class PsCardItemExtnVehicle : PsCardItemExtn
    {
        internal sealed class Metadata2
        {

            [Display(Name = "Series No.")]
            public Nullable<int> SeriesNo { get; set; }

            //[Required]
            [Display(Name = "Year Model")]
            public Nullable<int> YearModel { get; set; }

            //[Required]
            [Display(Name = "Plate No.")]
            public string PlateNo { get; set; }

            //[Required]
            [Display(Name = "Body No.")]
            public string BodyNo { get; set; }

            //[Display(Name = "Engine No.")]
            public string EngineNo { get; set; }

            //[Display(Name = "Chasis No.")]
            public string ChasisNo { get; set; }
            public string Color { get; set; }

            //[Required]
            [Display(Name = "CR No.")]
            public string CRN { get; set; }

            //[Required]
            [Display(Name = "CR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CRDate { get; set; }

            //[Required]
            [Display(Name = "MV File No.")]
            public string MVFileNo { get; set; }

            //[Display(Name = "OR No.")]
            public string OrNo { get; set; }

            //[Display(Name = "OR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> OrDate { get; set; }

            //[Required]
            [Display(Name = "Net Weight")]
            public Nullable<int> NetWeight { get; set; }

            [Display(Name = "Insurance Policy No.")]
            public string InsPolicyNo { get; set; }
            public string ParReissuance { get; set; }
            public string Condition { get; set; }

            [Display(Name = "Sub location")]
            public string SubLocation { get; set; }

            //[Required]
            [Display(Name = "Conduction Sticker No.")]
            public string ConductionNo { get; set; }
        }
    }

    //public class PsCardItemExtnVehicleVM
    //{
    //    public Guid Id { get; set; }

    //    [Required]
    //    [Display(Name = "Year Model")]
    //    public int? YearModel { get; set; }

    //    [Required]
    //    [Display(Name = "Plate No.")]
    //    public string PlateNo { get; set; }

    //    [Required]
    //    [Display(Name = "Body No.")]
    //    public string BodyNo { get; set; }

    //    [Display(Name = "Engine No.")]
    //    public string EngineNo { get; set; }

    //    [Display(Name = "Chasis No.")]
    //    public string ChasisNo { get; set; }
    //    public string Color { get; set; }

    //    [Required]
    //    public string CRN { get; set; }

    //    [Required]
    //    [Display(Name = "CR Date")]
    //    [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
    //    public DateTime? CRDate { get; set; }

    //    [Display(Name = "MV File No.")]
    //    public string MVFileNo { get; set; }

    //    [Display(Name = "OR No.")]
    //    public string OrNo { get; set; }

    //    [Display(Name = "OR Date")]
    //    [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
    //    public DateTime? OrDate { get; set; }

    //    [Required]
    //    [Display(Name = "Net Weight")]
    //    public decimal? NetWeight { get; set; }

    //    [Display(Name = "Insurance Policy No.")]
    //    public string InsPolicyNo { get; set; }

    //    [Display(Name = "Par Reissuance")]
    //    public string ParReissuance { get; set; }
    //    public string Condition { get; set; }

    //    [Display(Name = "Sub-Location")]
    //    public string SubLocation { get; set; }

    //    [Required]
    //    [Display(Name = "Conduction No.")]
    //    public string ConductionNo { get; set; }
    //    public string Location { get; set; }

    //    public Guid? LocationId { get; set; }

    //    [Display(Name = "Location Code")]
    //    public string LocationCode { get; set; }

    //    [Display(Name = "Property No.")]
    //    public string PropNo { get; set; }

    //    [Display(Name = "PAR/ICS No.")]
    //    public string ParIcsNo { get; set; }

    //    [Display(Name = "PAR/ICS Date.")]
    //    [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
    //    public DateTime? ParIcsDate { get; set; }
    //}

    public class PsCardItemExtnParVm
    {
        public Guid Id { get; set; }

        [Display(Name = "Property No.")]
        public string PropNo { get; set; }

        [Display(Name = "PAR/ICS No.")]
        public string ParIcsNo { get; set; }

        [Display(Name = "PAR/ICS Date.")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ParIcsDate { get; set; }
    }

    public class PsCardItemExtnLocationVm
    {
        public Guid Id { get; set; }

        public string Location { get; set; }

        public Guid? LocationId { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }
    }

    public class PsCardItemExtnSetVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemId { get; set; }
        public Nullable<System.Guid> AIRItemExtnId { get; set; }
        //public Nullable<System.Guid> PsCardItemTransferItemId { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set/Lot No.")]
        public Nullable<int> SetLotQtyNo { get; set; }

        [Display(Name = "Item Qty No.")]
        public Nullable<int> ContentNo { get; set; }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }
        public string Description { get; set; }

        public string PoNo { get; set; }
    }

    public class SelectedIds
    {
        public System.Guid Id { get; set; }
    }

    public class PsCardItemExtnTransitVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemId { get; set; }

        [Display(Name = "Group No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set/Lot No.")]
        public Nullable<int> SetLotQtyNo { get; set; }

        [Display(Name = "Item Qty No.")]
        public Nullable<int> ContentNo { get; set; }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        public Nullable<System.Guid> IcsParId { get; set; }
        public Nullable<System.Guid> IcsParItemId { get; set; }

        [Display(Name = "ICS/PAR No.")]
        public string IcsParNo { get; set; }

        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }

        public string Location { get; set; }
    }

    public class PsCardItemExtnVehicleEntryVM : PsCardItemCommonEntryVM
    {

    }

    public class PsCardItemExtnPpeEntryVM : PsCardItemCommonEntryVM
    {

    }

    public class PsCardItemExtnSuppliesEntryVM : PsCardItemCommonEntryVM
    {

    }

    public class PsCardItemExtnStructuresEntryVM : PsCardItemCommonEntryVM
    {

    }

    public class PsCardItemExtnLandEntryVM : PsCardItemCommonEntryVM
    {

    }

    public class PsCardItemCommonEntryVM
    {
        public PsCardItemCommonEntryVM()
        {
            this.AllField = new AllField() { Id = this.Id };
        }

        public AllField AllField { get; set; }

        public System.Guid Id { get; set; }
        public System.Guid PsCarItemId { get; set; }

        [Display(Name = "Article")]
        public Nullable<System.Guid> ItemCodeId { get; set; }

        public string Category { get; set; }
        public string Article { get; set; }

        [Display(Name = "Sub-Accounts")]
        public string SubAccount { get; set; }
        public string Account { get; set; }

        [Display(Name = "From Donation")]
        public bool? FromDonation { get; set; }

        public string Fund { get; set; }

        [Display(Name = "Property Card No.")]
        public string PsNo { get; set; }

        [Display(Name = "Property No.")]
        public string PropNo { get; set; }

        [Display(Name = "Custodian Item No.")]
        public string CustItemNo { get; set; }

        [Display(Name = "Series No.")]
        public string SeriesNo { get; set; }

        public string Annex { get; set; }

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }


        [Display(Name = "Location Code")]

        public string LocationCode { get; set; }

        public string Location { get; set; }

        [Display(Name = "Sub-location")]
        public string SubLocation { get; set; }

        public string Condition { get; set; }


        [Display(Name = "Acquisition Cost")]
        public Nullable<decimal> AcqCost { get; set; }

        [Display(Name = "Acquisition Date (mm/dd/yyyy)")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcqDate { get; set; }


        [Display(Name = "Set/Lot No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set/Lot Amount")]
        public Nullable<decimal> SetLotAmount { get; set; }

        [Display(Name = "Set Price Rate (%)")]
        public Nullable<decimal> PriceRate { get; set; }

        [Display(Name = "Pro-rated Set Cost")]
        public Nullable<decimal> ProRatedCost { get; set; }

        [Display(Name = "Set/Lot Remarks")]
        public string SetLotRemarks { get; set; }

        [Display(Name = "Old Amounts (in RPCPPE")]
        public Nullable<decimal> OldAmount { get; set; }

        [Display(Name = "Upcomming Accountable Officer")]
        public string UpcomingOfficer { get; set; }

        [Display(Name = "Originating PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "Originating PO Date (mm/dd/yyyy)")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Display(Name = "Department Code")]
        public string DeptCode { get; set; }

        [Display(Name = "Originating PO Department")]
        public string Department { get; set; }

        [Display(Name = "Department Display")]
        public string DeptDisplay { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [Display(Name = "AIR Date (mm/dd/yyyy)")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AirDate { get; set; }

        [Display(Name = "PO Unit of Measurement")]
        public string Unit { get; set; }

        [Display(Name = "PO Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "Additional Cost")]
        public Nullable<decimal> AddCost { get; set; }

        [Display(Name = "Total Unit Cost")]
        public Nullable<decimal> TUnitCost { get; set; }

        [Display(Name = "Total Amount")]
        public Nullable<decimal> GTAmount { get; set; }

        [Display(Name = "PO Description")]
        public string Description { get; set; }

        [Display(Name = "Other Particulars")]
        public string OtherDesc { get; set; }

        [Display(Name = "Other Particulars (Qty) *for monoblocks and books only")]
        public Nullable<int> OtherQty { get; set; }

        [Display(Name = "PO Qty")]
        public Nullable<int> Qty { get; set; }

        public string FPP { get; set; }

        [Display(Name = "Created By")]
        public string InsertedBy { get; set; }

        [Display(Name = "Created Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }

        [Display(Name = "Updated Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        public string Remarks { get; set; }

        public string OldPropNo { get; set; }
    }
}