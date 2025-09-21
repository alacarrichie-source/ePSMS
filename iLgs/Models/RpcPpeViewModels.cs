using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(RpcPpe.Metadata))]
    public partial class RpcPpe
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Display(Name = "Account Group")]
            public Nullable<int> AccountGroup { get; set; }
            
            [Required]
            [Display(Name = "As of Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AsOf { get; set; }

            public string Fund { get; set; }

            [Display(Name = "Department")]
            public Nullable<System.Guid> DeptId { get; set; }
            public string Department { get; set; }

            [Display(Name = "Certified correct by")]
            public string CertifiedCorrectBy { get; set; }

            [Display(Name = "Designation")]
            public string CertifiedCorrectDesignation { get; set; }

            [Display(Name = "Approved by")]
            public string ApprovedBy { get; set; }

            [Display(Name = "Designation")]
            public string ApprovedDesignation { get; set; }

            [Display(Name = "Verified by")]
            public string VerifiedBy { get; set; }

            [Display(Name = "Designation")]
            public string VerifiedDesignation { get; set; }

            [Display(Name = "Posted by")]
            public string PostedBy { get; set; }

            [Display(Name = "Posted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PostedDt { get; set; }

            [Display(Name = "Inserted by")]
            public string InsertedBy { get; set; }

            [Display(Name = "Inserted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }         
        }
    }

    [MetadataType(typeof(RpcPpeItem.Metadata))]
    public partial class RpcPpeItem
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }
            public Nullable<System.Guid> RpcPpeId { get; set; }
            public Nullable<System.Guid> PsCardItemExtnId { get; set; }
            public string Fund { get; set; }

            [Display(Name = "Custodian Item No")]
            public Nullable<decimal> CustodianItemNo { get; set; }

            [Display(Name = "Series No")]
            public string SeriesNo { get; set; }

            [Display(Name = "From Donation")]
            public Nullable<bool> FromDonation { get; set; }

            [Display(Name = "Inventory/For Distribution")]
            public string InvDist { get; set; }
            public string Account { get; set; }

            public Nullable<System.Guid> ItemCodeId { get; set; }

            [Display(Name = "Sub-Account")]
            public string SubAccount { get; set; }

            public string Article { get; set; }

            [Display(Name = "PO No.")]
            public string PoNo { get; set; }

            [Display(Name = "PO Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> PoDate { get; set; }

            [Display(Name = "AIR No.")]
            public string AirNo { get; set; }

            [Display(Name = "AIR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> AirDate { get; set; }

            [Display(Name = "Unit Cost")]
            public Nullable<decimal> UnitCost { get; set; }

            [Display(Name = "Unit of Measurement")]
            public string Unit { get; set; }

            [Display(Name = "Set/Lot No.")]
            public string SetLotNo { get; set; }
            
            public Nullable<System.Guid> DeptId { get; set; }

            [Display(Name = "Department")]
            public string Department { get; set; }

            [Display(Name = "Department Code")]
            public string DeptCode { get; set; }

            public Nullable<System.Guid> LocationId { get; set; }

            [Display(Name = "Location Code")]
            public string LocationCode { get; set; }

            [Display(Name = "Location")]
            public string Location { get; set; }

            [Display(Name = "Sub-Location")]
            public string SubLocation { get; set; }
            public Nullable<int> Qty { get; set; }
            public Nullable<int> TransferIn { get; set; }
            public Nullable<int> QtyBalance { get; set; }

            [Display(Name = "Amount")]
            public Nullable<decimal> TotalCost { get; set; }

            [Display(Name = "Old Amount")]
            public Nullable<decimal> OldAmount { get; set; }

            [Display(Name = "Old Prop. Card No.")]
            public string OldPsNo { get; set; }

            [Display(Name = "Property Card No.")]
            public string PsNo { get; set; }
            public string Description { get; set; }
            public string Brand { get; set; }

            [Display(Name = "Model")]
            public string Model_ { get; set; }
            public string Size { get; set; }
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

            [Display(Name = "Dosage Volume")]
            public string DosageVolume { get; set; }
            public Nullable<int> Multipliers { get; set; }

            [Display(Name = "Serial No.")]
            public string SerialNo { get; set; }

            [Display(Name = "Old Prop. No.")]
            public string OldPropNo { get; set; }

            [Display(Name = "Property No.")]
            public string PropNo { get; set; }

            [Display(Name = "Year Model")]
            public Nullable<int> YearModel { get; set; }

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
            public string CRN { get; set; }

            [Display(Name = "CR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> CRDate { get; set; }
            public string OrNo { get; set; }

            [Display(Name = "OR Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> OrDate { get; set; }

            [Display(Name = "Insurance Policy No.")]
            public string InsPolicyNo { get; set; }

            [Display(Name = "Conduction No.")]
            public string ConductionNo { get; set; }

            [Display(Name = "Item Serial No.")]
            public string ItemSerialNo { get; set; }

            [Display(Name = "Other Description")]
            public string OtherDesc { get; set; }

            [Display(Name = "Other Qty")]
            public Nullable<int> OtherQty { get; set; }
            public string Condition { get; set; }
            public string Remarks { get; set; }

            [Display(Name = "PAR No.")]
            public string ParNo { get; set; }

            [Display(Name = "PAR Issued To")]
            public string ParIssuedto { get; set; }

            [Display(Name = "PAR Accountable Officer")]
            public string AccountableOfficer { get; set; }

            [Display(Name = "ARE No.")]
            public string AreNo { get; set; }

            [Display(Name = "ARE Issued To")]
            public string AreIssuedto { get; set; }

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

            [Display(Name = "Upcoming PAR")]
            public string UpcomingPar { get; set; }

            [Display(Name = "Upcoming ICS")]
            public string UpcomingIcs { get; set; }


            public string Type { get; set; }
            public string Annex { get; set; }
            public string InsertedBy { get; set; }

            [Display(Name = "Inserted Date")]
            [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
            public Nullable<decimal> SetLotAmount { get; set; }
            public string SetLotRemarks { get; set; }
        }
    }
}