using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Models
{
    [MetadataType(typeof(CustodianReport.Metadata))]
    public partial class CustodianReport
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Display(Name = "As of Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AsOf { get; set; }

            public string Fund { get; set; }

            [Display(Name = "Department")]
            public Nullable<System.Guid> DeptId { get; set; }
            public string Department { get; set; }

            [Display(Name = "Certified correct by")]
            public string CertifiedCorrectBy { get; set; }

            [Display(Name = "Account Group")]
            public int? AccountGroup { get; set; }

            [Display(Name = "Aporoved by")]
            public string ApprovedBy { get; set; }

            [Display(Name = "Verified by")]
            public string VerifiedBy { get; set; }

            [Display(Name = "Posted by")]
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

    [MetadataType(typeof(CustodianReportItem.Metadata))]
    public partial class CustodianReportItem
    {
        
        public string ItemType_Code { get; set; }
        public string Item_Code { get; set; }

        public AllField AllField { get; set; }
        public Guid? MainDeptId { get; set; }

        [Display(Name = "Custodian Department")]
        public string MainDeptName { get; set; }
        public int? AccountGroup { get; set; }

        
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> ReportId { get; set; }

            //[Required]
            public string Fund { get; set; }

            [Display(Name = "Custodian Item No.")]
            public Nullable<decimal> CustodianItemNo { get; set; }

            [Display(Name = "Series No.")]
            public string SeriesNo { get; set; }

            [Display(Name = "From Donation")]
            public Nullable<bool> FromDonation { get; set; }

            //[Required]
            [Display(Name = "Inventory/For Distribution")]
            public string InvDist { get; set; }

            public string Account { get; set; }

            [Required]
            [Display(Name = "Article")]
            public Nullable<System.Guid> ItemCodeId { get; set; }

            [Display(Name = "Sub-Account")]
            public string SubAccount { get; set; }
            public string Article { get; set; }

            [Display(Name = "PO No.")]
            public string PoNo { get; set; }

            [Display(Name = "PO Date (mm/dd/yyyy)")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PoDate { get; set; }

            [Display(Name = "AIR No.")]
            public string AirNo { get; set; }

            [Display(Name = "AIR Date (mm/dd/yyyy)")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AirDate { get; set; }

            [Display(Name = "Acquisition Date (mm/dd/yyyy)")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AcqDate { get; set; }

            //[Required]
            [Display(Name = "P.O. Unit Cost")]
            public Nullable<decimal> UnitCost { get; set; }

            //[Required]
            [Display(Name = "P.O. Unit of Measurement")]
            public string Unit { get; set; }

            [Display(Name = "Set/Lot No.")]
            public string SetLotNo { get; set; }

            //[Required]
            [Display(Name = "Originating P.O. Department")]
            public Nullable<System.Guid> DeptId { get; set; }

            [Display(Name = "Department Display")]
            public string Department { get; set; }

            [Display(Name = "Location Code")]
            public Nullable<System.Guid> LocationId { get; set; }

            public string LocationCode { get; set; }
            public string Location { get; set; }

            [Display(Name = "Sub-Location")]
            public string SubLocation { get; set; }

            [Display(Name = "Acquisition Cost")]
            public Nullable<decimal> TotalCost { get; set; }

            [Display(Name = "Old Amounts (Recorded in RPCPPE)")]
            public Nullable<decimal> OldAmount { get; set; }


            //[Required]
            [Display(Name = "PO Description")]
            public string Description { get; set; }
            //public string Brand { get; set; }

            //[Display(Name = "Model")]
            //public string Model_ { get; set; }
            public string Dimension { get; set; }
            public string Size { get; set; }

            //[Display(Name = "Net Weight")]
            public string Weight { get; set; }
            public string Materials { get; set; }
            public string Capacity { get; set; }
            public string Color { get; set; }

            [Display(Name = "Generic Name")]
            public string GenericName { get; set; }

            [Display(Name = "Dosage Strength")]
            public string DosageStrength { get; set; }

            [Display(Name = "Dosage Form")]
            public string DosageForm { get; set; }

            [Display(Name = "Dosage Volumne")]
            public string DosageVolume { get; set; }

            [Display(Name = "Multiples (#'s)")]
            public Nullable<int> Multipliers { get; set; } = 0;

            [Display(Name = "Serial No.")]
            public string SerialNo { get; set; }

            [Display(Name = "Old Property No.")]
            public string OldPropNo { get; set; }

            [Display(Name = "Property No.")]
            public string PropNo { get; set; }

            [Display(Name = "Plate No.")]
            public string PlateNo { get; set; }

            [Display(Name = "Body No.")]
            public string BodyNo { get; set; }

            [Display(Name = "MV File No.")]
            public string MVFileNo { get; set; }

            [Display(Name = "Engine No.")]
            public string EngineNo { get; set; }

            [Display(Name = "Chasis No.")]
            public string ChasisNo { get; set; }

            [Display(Name = "CR No.")]
            public string CRN { get; set; }

            [Display(Name = "CR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CRDate { get; set; }

            [Display(Name = "OR No.")]
            public string OrNo { get; set; }

            [Display(Name = "OR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> OrDate { get; set; }

            [Display(Name = "Insurance Policy No.")]
            public string InsPolicyNo { get; set; }

            [Display(Name = "Conduction Sticker No.")]
            public string ConductionNo { get; set; }

            [Display(Name = "Item Serial No.")]
            public string ItemSerialNo { get; set; }

            [Display(Name = "Other Particulars")]
            public string OtherDesc { get; set; }

            [Display(Name = "Other Particulars (Qty)")]
            public Nullable<int> OtherQty { get; set; }
            public string Condition { get; set; }
            public string Remarks { get; set; }

            [Display(Name = "PAR No.")]
            public string ParNo { get; set; }

            [Display(Name = "PAR Issued To")]
            public string ParIssuedTo { get; set; }

            [Display(Name = "PAR Accountable Officer")]
            public string AccountableOfficer { get; set; }

            [Display(Name = "ARE No.")]
            public string AreNo { get; set; }

            [Display(Name = "ARE Issued To")]
            public string AreIssuedTo { get; set; }

            [Display(Name = "ARE Accountable Officer")]
            public string AreOfficer { get; set; }

            [Display(Name = "MR No.")]
            public string MrNo { get; set; }

            [Display(Name = "MR Issued To")]
            public string MrIssuedTo { get; set; }

            [Display(Name = "MR Accountable Officer")]
            public string MrOfficer { get; set; }

            [Display(Name = "ICS No.")]
            public string IcsNo { get; set; }

            [Display(Name = "ICS Issued To")]
            public string IcsIssuedTo { get; set; }

            [Display(Name = "ICS Accountable Officer")]
            public string IcsOfficer { get; set; }


            [Display(Name = "Upcoming PAR Accountable Officer")]
            public string UpcomingPar { get; set; }

            [Display(Name = "Upcoming ICS Accountable Officer")]
            public string UpcomingIcs { get; set; }

            public string Type { get; set; }

            //[Required]            
            public string Annex { get; set; }

            [Display(Name = "Inserted By")]
            public string InsertedBy { get; set; }

            [Display(Name = "Inserted Date")]
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

            [Display(Name = "Set/Lot Amount")]
            public Nullable<decimal> SetLotAmount { get; set; }

            [Display(Name = "Set/Lot Remarks")]
            public string SetLotRemarks { get; set; }

            public ItemCode ItemCode { get; set; }
        }
    }

    public class CustodianReportItemStockVM : CustodianReportItem
    {
        [Display(Name = "Old Stock Card No.")]
        public new string OldPsNo { get => base.OldPsNo; set => base.OldPsNo = value; }

        [Display(Name = "Stock Card No.")]
        public new string PsNo { get => base.PsNo; set => base.PsNo = value; }

        [Display(Name = "Qty In")]
        public new Nullable<int> Qty { get => base.Qty; set => base.Qty = value; }

        [Display(Name = "Transit In Qty")]
        public new Nullable<int> TransferIn { get => base.TransferIn; set => base.TransferIn = value; }

        [Display(Name = "Balance")]
        public new Nullable<int> QtyBalance { get => base.QtyBalance; set => base.QtyBalance = value; }

        [Display(Name = "Model")]
        public new string Model_ { get => base.Model_; set => base.Model_ = value; }
    }

    public class CustodianReportItemPpeVM : CustodianReportItem
    {
        [Display(Name = "Property Card No.")]
        public new string PsNo { get => base.PsNo; set => base.PsNo = value; }

        [Display(Name = "Old Property Card No.")]
        public new string OldPsNo { get => base.OldPsNo; set => base.OldPsNo = value; }

        [Display(Name = "Weight")]
        public new string Weight { get => base.Weight; set => base.Weight = value; }

        [Display(Name = "Brand")]
        public new string Brand { get => base.Brand; set => base.Brand = value; }

        [Display(Name = "Model")]
        public new string Model_ { get => base.Model_; set => base.Model_ = value; }
    }

    public class CustodianReportItemVehicleVM : CustodianReportItem
    {
        [Display(Name = "Property Card No.")]
        public new string PsNo { get => base.PsNo; set => base.PsNo = value; }

        [Display(Name = "Old Property Card No.")]
        public new string OldPsNo { get => base.OldPsNo; set => base.OldPsNo = value; }

        //[Required]
        [Display(Name = "Year Model")]
        public new Nullable<int> YearModel { get => base.YearModel; set => base.YearModel = value; }

        //[Required]
        [Display(Name = "Body No.")]
        public new string BodyNo { get => base.BodyNo; set => base.BodyNo = value; }

        //[Required]
        [Display(Name = "CR No.")]
        public new string CRN { get => base.CRN; set => base.CRN = value; }

        //[Required]
        [Display(Name = "CR Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public new Nullable<System.DateTime> CRDate { get => base.CRDate; set => base.CRDate = value; }

        //[Required]
        [Display(Name = "MV File No.")]
        public new string MVFileNo { get => base.MVFileNo; set => base.MVFileNo = value; }

        [Display(Name = "Net Weight")]
        public new string Weight { get => base.Weight; set => base.Weight = value; }

        [Display(Name = "Brand/Make")]
        public new string Brand { get => base.Brand; set => base.Brand = value; }

        [Display(Name = "Model/Series")]
        public new string Model_ { get => base.Model_; set => base.Model_ = value; }

    }

    [MetadataType(typeof(CustodianReportLandItem.Metadata))]
    public partial class CustodianReportLandItem
    {
        //public CustodianReportLandItem()
        //{
        //    this.AllField = new AllField();
        //}

        public string ItemType_Code { get; set; }
        public string Item_Code { get; set; }

        public AllField AllField;
        public Guid? MainDeptId { get; set; }

        [Display(Name = "Custodian Department")]
        public string MainDeptName { get; set; }
        public int? AccountGroup { get; set; }

        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> ReportId { get; set; }
            public string Fund { get; set; }

            [Display(Name = "Custodian Item No.")]
            public Nullable<decimal> CustodianItemNo { get; set; }

            [Display(Name = "Series No.")]
            public string SeriesNo { get; set; }

            [Display(Name = "From Donation")]
            public Nullable<bool> FromDonation { get; set; }
            public string Account { get; set; }

            [Display(Name = "Article")]
            public Nullable<System.Guid> ItemCodeId { get; set; }

            [Display(Name = "Sub-Account")]
            public string SubAccount { get; set; }
            public string Article { get; set; }

            [Display(Name = "Location")]
            public Nullable<System.Guid> LocationId { get; set; }

            [Display(Name = "Location")]
            public string LocationCode { get; set; }
            public string Location { get; set; }
            public string Type { get; set; }
            public string Condition { get; set; }
            public string Description { get; set; }

            [Display(Name = "Sub-Location")]
            public string SubLocation { get; set; }

            [Display(Name = "Land ID Number")]
            public string PIN { get; set; }
            public string Address { get; set; }

            [Display(Name = "Landmarks")]
            public string LandMarks { get; set; }

            //[Required]
            [Display(Name = "Area (sqm)")]
            public Nullable<decimal> Area { get; set; }

            //[Required]
            [Display(Name = "Price per sqm.")]
            public Nullable<decimal> PricePerSqm { get; set; }

            [Display(Name = "Market Value")]
            public Nullable<decimal> MarketValue { get; set; }

            [Display(Name = "Property Card No.")]
            public string PsNo { get; set; }

            [Display(Name = "Property No.")]
            public string PropNo { get; set; }

            [Display(Name = "Old Property No.")]
            public string OldPropNo { get; set; }

            [Display(Name = "Acquisition Cost")]
            public Nullable<decimal> AcqCost { get; set; }

            [Display(Name = "Old Amounts (in RPCPPE)")]
            public Nullable<decimal> OldAmount { get; set; }

            [Display(Name = "Acquisition Date")]
            public Nullable<System.DateTime> AcqDate { get; set; }

            [Display(Name = "Vendor/Donor")]
            public string Vendor { get; set; }

            [Display(Name = "Representative")]
            public string Representative { get; set; }

            [Display(Name = "TCT No.")]
            public string TctNo { get; set; }

            //[Required]
            [Display(Name = "Old TCT No.")]
            public string OldTctNo { get; set; }

            [Display(Name = "DRP No.")]
            public string DRPNo { get; set; }

            [Display(Name = "Date Regs.")]
            public Nullable<System.DateTime> DRPDate { get; set; }

            [Display(Name = "Old DRP No.")]
            public string OldDRPNo { get; set; }

            [Display(Name = "Date Regs.")]
            public Nullable<System.DateTime> OldDRPDate { get; set; }
            public string Remarks { get; set; }

            [Display(Name = "Capital Gains Tax (CGT)")]
            public Nullable<decimal> CGT { get; set; }

            [Display(Name = "CGT Transfer Tax")]
            public Nullable<decimal> CGTTransferTax { get; set; }

            [Display(Name = "CGT Surcharge")]
            public Nullable<decimal> CGTSurcharge { get; set; }

            [Display(Name = "CGT Interest")]
            public Nullable<decimal> CGTInterest { get; set; }

            [Display(Name = "CGT Compromise")]
            public Nullable<decimal> CGTCompromise { get; set; }

            [Display(Name = "Document Stamp Tax (DST)")]
            public Nullable<decimal> DST { get; set; }

            [Display(Name = "DST Transfer TAx")]
            public Nullable<decimal> DSTTransferTax { get; set; }

            [Display(Name = "DST Surcharge")]
            public Nullable<decimal> DSTSurcharge { get; set; }

            [Display(Name = "DST Interest")]
            public Nullable<decimal> DSTInterest { get; set; }

            [Display(Name = "DST Compromise")]
            public Nullable<decimal> DSTCompromise { get; set; }

            [Display(Name = "Transfer Tax")]
            public Nullable<decimal> TransferTax { get; set; }

            public Nullable<decimal> Surcharge { get; set; }
            public Nullable<decimal> Interest { get; set; }

            [Display(Name = "Confirmation Fee")]
            public Nullable<decimal> ConfirmationFee { get; set; }

            [Display(Name = "Transfer Regs. Fee")]
            public Nullable<decimal> TransferRegsFee { get; set; }

            [Display(Name = "Real Property Tax")]
            public Nullable<decimal> RealPropertyFee { get; set; }
            public Nullable<decimal> VAT { get; set; }

            [Display(Name = "Estate Fee")]
            public Nullable<decimal> EstateFee { get; set; }
            public Nullable<decimal> Titling { get; set; }

            [Display(Name = "Certificattion Fee")]
            public Nullable<decimal> CertificationFee { get; set; }
            public Nullable<decimal> Relocation { get; set; }
            public Nullable<decimal> Surveying { get; set; }

            [Display(Name = "Incidental Expense")]
            public Nullable<decimal> IncidentalExpenses { get; set; }

            [Display(Name = "Capital Outlay or Expense")]
            public string CapitalOutlayOrExpense { get; set; }

            [MaxLength(1)]
            public string Annex { get; set; }

            [Display(Name = "Inserted By")]
            public string InsertedBy { get; set; }

            [Display(Name = "Inserted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            [Display(Name = "Posted By")]
            public string PostedBy { get; set; }

            [Display(Name = "Posted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PostedDt { get; set; }
        }
    }

    public class CustodianReportLandItemVM : CustodianReportLandItem
    {

    }

    [MetadataType(typeof(CustodianReportBldgItem.Metadata))]
    public partial class CustodianReportBldgItem
    {
        //public CustodianReportBldgItem()
        //{
        //    this.AllField = new AllField();
        //}

        public string ItemType_Code { get; set; }
        public string Item_Code { get; set; }

        public AllField AllField;
        public Guid? MainDeptId { get; set; }

        [Display(Name = "Custodian Department")]
        public string MainDeptName { get; set; }
        public int? AccountGroup { get; set; }
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> ReportId { get; set; }
            public string Fund { get; set; }

            [Display(Name = "Custodian Item No.")]
            public Nullable<decimal> CustodianItemNo { get; set; }

            [Display(Name = "Series No.")]
            public string SeriesNo { get; set; }

            [Display(Name = "From Donation")]
            public Nullable<bool> FromDonation { get; set; }
            public string Account { get; set; }

            [Display(Name = "Article")]
            public Nullable<System.Guid> ItemCodeId { get; set; }

            [Display(Name = "Sub-Acount")]
            public string SubAccount { get; set; }
            public string Article { get; set; }

            [Display(Name = "Building Item")]
            public string BldgItem { get; set; }

            [Display(Name = "PO No.")]
            public string PoNo { get; set; }

            [Display(Name = "PO Date")]
            public Nullable<System.DateTime> PoDate { get; set; }

            [Display(Name = "Acquisition Cost")]
            public Nullable<decimal> AcqCost { get; set; }

            [Display(Name = "Department")]
            public Nullable<System.Guid> DeptId { get; set; }
            public string Department { get; set; }

            [Display(Name = "Location Code")]
            public Nullable<System.Guid> LocationId { get; set; }

            [Display(Name = "Location Code")]
            public string LocationCode { get; set; }

            [Display(Name = "Location/Barangay")]
            public string Location { get; set; }

            [Display(Name = "Sub-Location/Address")]
            public string SubLocation { get; set; }

            [Display(Name = "Engineering Project Name")]
            public string ProjectName { get; set; }

            [Display(Name = "Property Card No.")]
            public string PsNo { get; set; }

            [Display(Name = "Acquisition Month")]
            public Nullable<int> AcqMonth { get; set; }

            [Display(Name = "Acquisition Year")]
            public Nullable<int> AcqYear { get; set; }

            [Display(Name = "Acquisition Day")]
            public Nullable<int> AcqDay { get; set; }

            [Display(Name = "Date Acquisition/Construction")]
            public Nullable<System.DateTime> AcqDate { get; set; }

            [Display(Name = "Property Number")]
            public string PropNo { get; set; }

            [Display(Name = "Old Amounts (Recorded in RPCPPE)")]
            public Nullable<decimal> OldAmount { get; set; }

            [Display(Name = "Building/Structure Type")]
            public string BuildingType { get; set; }

            [Display(Name = "Building Area (sqm)")]
            public Nullable<decimal> Area { get; set; }

            [Display(Name = "Phase Amount Total")]
            public Nullable<decimal> TotalAmount { get; set; }

            //[Required]
            [Display(Name = "Phase No.")]
            public string PhaseNo { get; set; }

            [Display(Name = "Phase Amount MOOE")]
            public Nullable<decimal> PhaseAmountMooe { get; set; }

            //[Required]
            [Display(Name = "Phase Amount Capital Outlay")]
            public Nullable<decimal> PhaseAmountCo { get; set; }

            [Display(Name = "Start Year")]
            public Nullable<int> StartYear { get; set; }

            [Display(Name = "Start Month")]
            public Nullable<int> StartMonth { get; set; }

            [Display(Name = "Start Day")]
            public Nullable<int> StartDay { get; set; }

            [Display(Name = "Start Date")]
            public Nullable<System.DateTime> StartDate { get; set; }

            [Display(Name = "Target Year")]
            public Nullable<int> TargetYear { get; set; }

            [Display(Name = "Target Month")]
            public Nullable<int> TargetMonth { get; set; }

            [Display(Name = "Target Day")]
            public Nullable<int> TargetDay { get; set; }

            [Display(Name = "Target Date")]
            public Nullable<System.DateTime> TargetDate { get; set; }

            [Display(Name = "Percent Complete (%)")]
            public Nullable<decimal> PercentComplete { get; set; }

            [Display(Name = "Completion Year")]
            public Nullable<int> CompletionYear { get; set; }

            [Display(Name = "Completion Month")]
            public Nullable<int> CompletionMonth { get; set; }

            [Display(Name = "Completion Day")]
            public Nullable<int> CompletionDay { get; set; }

            [Display(Name = "Completion Date")]
            public Nullable<System.DateTime> CompletionDate { get; set; }

            public string Status { get; set; }
            public string Condition { get; set; }
            public string Remarks { get; set; }
            public string Annex { get; set; }

            [Display(Name = "Inserted By")]
            public string InsertedBy { get; set; }

            [Display(Name = "Inserted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            [Display(Name = "Posted By")]
            public string PostedBy { get; set; }

            [Display(Name = "Posted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PostedDt { get; set; }
            public Nullable<double> Latitude { get; set; }
            public Nullable<double> Longitude { get; set; }
        }
    }

    public class CustodianReportBldgItemVM : CustodianReportBldgItem
    {

    }

    public partial class CustodianReportItemIssuance
    {
        //public System.Guid Id { get; set; }
        //public Nullable<System.Guid> ReportItemId { get; set; }
        //public string RefType { get; set; }
        //public string RefNo { get; set; }

        //[Display(Name = "Issued To")]
        //public string IssuedTo { get; set; }

        //[Display(Name = "Accountable Officer")]
        //public string AccountableOfficer { get; set; }
        //public string InsertedBy { get; set; }
        //public Nullable<System.DateTime> InsertedDt { get; set; }
        //public string UpdatedBy { get; set; }
        //public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    [MetadataType(typeof(CustodianReportItemIssuanceParVM.Metadata))]
    public class CustodianReportItemIssuanceParVM : CustodianReportItemIssuance
    {
        internal sealed class Metadata
        {
            [Display(Name = "PAR No.")]
            [Required]
            public string RefNo { get; set; }

            [Display(Name = "Issued To")]
            public string IssuedTo { get; set; }

            [Required]
            [Display(Name = "Accountable Officer")]
            public string AccountableOfficer { get; set; }
        }
    }

    [MetadataType(typeof(CustodianReportItemIssuanceIcsVM.Metadata))]
    public class CustodianReportItemIssuanceIcsVM : CustodianReportItemIssuance
    {
        internal sealed class Metadata
        {
            [Required]
            [Display(Name = "ICS No.")]
            public string RefNo { get; set; }

            [Display(Name = "Issued To")]
            public string IssuedTo { get; set; }

            [Required]
            [Display(Name = "Accountable Officer")]
            public string AccountableOfficer { get; set; }
        }
    }

    [MetadataType(typeof(CustodianReportItemIssuanceMrVM.Metadata))]
    public class CustodianReportItemIssuanceMrVM : CustodianReportItemIssuance
    {
        internal sealed class Metadata
        {
            [Required]
            [Display(Name = "MR No.")]
            public string RefNo { get; set; }

            [Display(Name = "Issued To")]
            public string IssuedTo { get; set; }

            [Required]
            [Display(Name = "Accountable Officer")]
            public string AccountableOfficer { get; set; }
        }
    }

    [MetadataType(typeof(CustodianReportItemIssuanceAreVM.Metadata))]
    public class CustodianReportItemIssuanceAreVM : CustodianReportItemIssuance
    {
        internal sealed class Metadata
        {
            [Required]
            [Display(Name = "ARE No.")]
            public string RefNo { get; set; }

            [Display(Name = "Issued To")]
            public string IssuedTo { get; set; }

            [Required]
            [Display(Name = "Accountable Officer")]
            public string AccountableOfficer { get; set; }
        }
    }
}