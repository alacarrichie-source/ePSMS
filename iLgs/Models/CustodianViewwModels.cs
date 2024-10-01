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

            [Required]
            [Display(Name = "As of Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AsOf { get; set; }

            [Required]
            public string Fund { get; set; }

            //[Required]
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
        public CustodianReportItem()
        {
            this.AllField = new AllField();            
        }

        public string ItemType_Code { get; set; }
        public string Item_Code { get; set; }

        public AllField AllField;
        public int? AccountGroup { get; set; }        
        
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> ReportId { get; set; }

            [Display(Name = "Custodian Item No.")]
            public Nullable<int> CustodianItemNo { get; set; }

            [Display(Name = "Series No.")]
            public string SeriesNo { get; set; }

            [Display(Name = "From Donation")]
            public Nullable<bool> FromDonation { get; set; }

            [Required]
            [Display(Name = "Inventory/For Distribution")]
            public string InvDist { get; set; }

            //[Required]
            //[Display(Name = "Account")]
            //public Nullable<System.Guid> ItemTypeId { get; set; }
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

            [Required]
            [Display(Name = "Unit Cost")]
            public Nullable<decimal> UnitCost { get; set; }

            [Required]
            [Display(Name = "Unit of Measurement")]
            public string Unit { get; set; }

            [Display(Name = "Set/Lot No.")]
            public string SetLotNo { get; set; }

            [Required]
            [Display(Name = "Originating O.R. Department")]
            public Nullable<System.Guid> DeptId { get; set; }

            [Display(Name = "Department Display")]
            public string Department { get; set; }

            [Display(Name = "Location Code")]
            public Nullable<System.Guid> LocationId { get; set; }

            public string LocationCode { get; set; }
            public string Location { get; set; }

            [Display(Name = "Sub-Location")]
            public string SubLocation { get; set; }

            [Display(Name = "Total Amount")]
            public Nullable<decimal> TotalCost { get; set; }

            [Display(Name = "Old Amount")]
            public Nullable<decimal> OldAmount { get; set; }
            

            [Required]
            public string Description { get; set; }
            public string Brand { get; set; }

            [Display(Name = "Model")]
            public string Model_ { get; set; }
            public string Dimension { get; set; }
            public string Size { get; set; }

            [Display(Name = "Net Weight")]
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

            [Display(Name = "Multipiers ('s)")]
            public Nullable<int> Multipliers { get; set; }

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

            [Display(Name = "CRN")]
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

            [Display(Name = "Cunduction Sticker No.")]
            public string ConductionNo { get; set; }

            [Display(Name = "Item Serial No.")]
            public string ItemSerialNo { get; set; }

            [Display(Name = "Other Particulars")]
            public string OtherDesc { get; set; }

            [Display(Name = "Other Qty")]
            public Nullable<int> OtherQty { get; set; }
            public string Condition { get; set; }
            public string Remarks { get; set; }

            [Display(Name = "PAR No.")]
            public string ParNo { get; set; }

            [Display(Name = "ARE No.")]
            public string AreNo { get; set; }

            [Display(Name = "MR No.")]
            public string MrNo { get; set; }

            [Display(Name = "Issued To")]
            public string ParIssuedTo { get; set; }

            [Display(Name = "Accountable Officer")]
            public string AccountableOfficer { get; set; }

            [Display(Name = "Upcoming PAR")]
            public string UpcomingPar { get; set; }

            public string Type { get; set; }

            [Display(Name = "Year Model")]
            public Nullable<int> YearModel { get; set; }

            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
        }
    }
    
    public class CustodianReportItemStockVM : CustodianReportItem
    {
        [Display(Name = "Old Stock No.")]
        public new string OldPsNo { get => base.OldPsNo; set => base.OldPsNo = value; }

        [Display(Name = "Stock No.")]
        public new string PsNo { get => base.PsNo; set => base.PsNo = value; }

        [Display(Name = "Qty In")]
        public new Nullable<int> Qty { get => base.Qty; set => base.Qty = value; }

        [Display(Name = "Transit In")]
        public new Nullable<int> TransferIn { get => base.TransferIn; set => base.TransferIn = value; }

        [Required]
        [Display(Name = "Balance")]
        public new Nullable<int> QtyBalance { get => base.QtyBalance; set => base.QtyBalance = value; }
    }

    public class CustodianReportItemPpeVM : CustodianReportItem
    {
        [Display(Name = "Property Card No.")]
        public new string PsNo { get => base.PsNo; set => base.PsNo = value; }

        [Display(Name = "Old Property Card No.")]
        public new string OldPsNo { get => base.OldPsNo; set => base.OldPsNo = value; }
        
    }

    public partial class CustodianReportItemVehicleVM : CustodianReportItem
    {
        [Display(Name = "Property No.")]
        public new string PsNo { get => base.PsNo; set => base.PsNo = value; }

        [Display(Name = "Old Property No.")]
        public new string OldPsNo { get => base.OldPsNo; set => base.OldPsNo = value; }
    }

}