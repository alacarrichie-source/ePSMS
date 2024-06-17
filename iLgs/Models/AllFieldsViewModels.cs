using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace iLgs.Models
{
    public class AllFieldVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RisItemId { get; set; }
        public Nullable<System.Guid> PsCardId { get; set; }

        [Display(Name = "Acquisition Mode")]        
        public string AcqMode { get; set; }

        [Display(Name = "Inventory/For Distribution")]
        public string InvDist { get; set; }

        [Display(Name = "Generic Name")]
        public string GenericName { get; set; }

        [Display(Name = "Dosage Strength")]
        public string DosageStrength { get; set; }

        [Display(Name = "Dosage Form")]
        public string DosageForm { get; set; }

        [Display(Name = "Doage Volumne")]
        public string DosageVolume { get; set; }

        public string Others { get; set; }
        public string Brand { get; set; }

        [Display(Name = "Model")]
        public string Model_ { get; set; }

        [Display(Name = "Aera (SQM)")]
        public Nullable<decimal> Area { get; set; }

        public string Barangay { get; set; }

        [Display(Name = "Date of Sale")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateSale { get; set; }

        [Display(Name = "Date of Donation")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateDonation { get; set; }

        [Display(Name = "Date of Acquisition")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateAcquisition { get; set; }

        [Display(Name = "Date of Construction")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateConstruction { get; set; }

        [Display(Name = "Area Sold/Donated")]
        public Nullable<decimal> AreaSoldDonated { get; set; }

        [Display(Name = "Price per SQM")]
        public Nullable<decimal> PricePerSqm { get; set; }

        [Display(Name = "Acquisition Cost")]
        public Nullable<decimal> AcqCost { get; set; }

        [Display(Name = "Vendor/Donor")]
        public string VendorDonor { get; set; }

        [Display(Name = "TCT No.")]
        public string TctNo { get; set; }

        [Display(Name = "Phase No.")]
        public string PhaseNo { get; set; }

        [Display(Name = "Phase Amount")]
        public Nullable<decimal> PhaseAmount { get; set; }

        public string Type { get; set; }

        [Display(Name = "Serial No.")]
        public string SerialNo { get; set; }

        [Display(Name = "Year Model")]
        public Nullable<int> YearModel { get; set; }

        [Display(Name = "Net Weight")]
        public string NetWeight { get; set; }

        [Display(Name = "Conduction Sticker")]
        public string ConductionSticker { get; set; }

        [Display(Name = "Plate No")]
        public string PlateNo { get; set; }

        [Display(Name = "Body No.")]
        public string BodyNo { get; set; }

        [Display(Name = "Cert. of Regs. No.")]
        public string CRN { get; set; }

        [Display(Name = "Cert. of Regs. Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> CRNDate { get; set; }

        [Display(Name = "Motor Vehicle File No.")]
        public string MVFileNo { get; set; }

        public string Dimension { get; set; }
        public string Size { get; set; }
        public string Weight { get; set; }
        public string Materials { get; set; }
        public string Capacity { get; set; }
        public string Color { get; set; }

        [Display(Name = "Property No.")]
        public string PropertyNo { get; set; }

        [Display(Name = "DRP No.")]
        public string DRPNo { get; set; }

        [Display(Name = "DRP Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DRPDate { get; set; }

        public string Location { get; set; }

        [Display(Name = "CGT")]
        public Nullable<decimal> CGT { get; set; }

        [Display(Name = "CGT-Transfer Tax")]
        public Nullable<decimal> CGTTransferTax { get; set; }

        [Display(Name = "CGT-Surcharge")]
        public Nullable<decimal> CGTSurcharge { get; set; }

        [Display(Name = "CGT-Interest")]
        public Nullable<decimal> CGTInterest { get; set; }

        [Display(Name = "CGT-Compromise")]
        public Nullable<decimal> CGTCompromise { get; set; }

        public Nullable<decimal> DST { get; set; }

        [Display(Name = "DST-Transfer Tax")]
        public Nullable<decimal> DSTTransferTax { get; set; }

        [Display(Name = "DST-Surchage")]
        public Nullable<decimal> DSTSurcharge { get; set; }

        [Display(Name = "DST-Interest")]
        public Nullable<decimal> DSTInterest { get; set; }

        [Display(Name = "DST-Compromise")]
        public Nullable<decimal> DSTCompromise { get; set; }

        [Display(Name = "Transfer Tax")]
        public Nullable<decimal> TransferTax { get; set; }

        public Nullable<decimal> Surcharge { get; set; }
        public Nullable<decimal> Interest { get; set; }

        [Display(Name = "Confirmation Fee")]
        public Nullable<decimal> ConfirmationFee { get; set; }

        [Display(Name = "Transfer/Regs. Fee")]
        public Nullable<decimal> TransferRegsFee { get; set; }

        [Display(Name = "Real Property Tax")]
        public Nullable<decimal> RealPropertyTax { get; set; }
        public Nullable<decimal> VAT { get; set; }
        public Nullable<decimal> EstateTax { get; set; }
        public Nullable<decimal> Tilting { get; set; }
        public Nullable<decimal> CertificationFee { get; set; }
        public Nullable<decimal> Relocation { get; set; }
        public Nullable<decimal> Surveying { get; set; }
        public Nullable<decimal> IncidentalExpenses { get; set; }
        public string CapitalOutlyOrExpense { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        //public virtual PsCard PsCard { get; set; }
        //public virtual RisItem RisItem { get; set; }
    }
}