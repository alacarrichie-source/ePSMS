using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace iLgs.Models
{
    [MetadataType(typeof(AllField.Metadata))]
    public partial class AllField
    {
        internal sealed class Metadata
        {
            public System.Guid Id { get; set; }

            [Display(Name = "Acquisition Mode")]
            public string AcqMode { get; set; }

            [Display(Name = "Acquisition Cost")]
            public Nullable<decimal> AcqCost { get; set; }

            [Display(Name = "Inventory/For Distribution")]
            public string InvDist { get; set; }

            [Display(Name = "Generic Name")]
            public string GenericName { get; set; }

            [Display(Name = "Dosage Strength")]
            public string DosageStrength { get; set; }

            [Display(Name = "Dosage Form")]
            public string DosageForm { get; set; }

            [Display(Name = "Doage Volume")]
            public string DosageVolume { get; set; }

            public string Others { get; set; }
            public string Brand { get; set; }

            [Display(Name = "Multiples ('s)")]
            public Nullable<int> Multipliers { get; set; }

            [Display(Name = "Model")]
            public string Model_ { get; set; }
            public string Dimension { get; set; }
            public string Size { get; set; }
            public string Weight { get; set; }
            public string Materials { get; set; }
            public string Capacity { get; set; }
            public string Color { get; set; }
            public string Type { get; set; }
            
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

            [Display(Name = "Vendor/Donor")]
            public string VendorDonor { get; set; }

            [Display(Name = "Serial No. of the repair item")]
            public string SerialNo { get; set; }

            [Display(Name = "Property No. of the repair item")]
            public string PropNo { get; set; }

            [Display(Name = "Plate No. of the repair item")]
            public string PlateNo { get; set; }

            [Display(Name = "Body No. of the repair item")]
            public string BodyNo { get; set; }

            [Display(Name = "MV File No. of the repair item")]
            public string MVFileNo { get; set; }

            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }

            public PsCard PsCard { get; set; }
            //public RisItem RisItem { get; set; }
            public OrderItem OrderItem { get; set; }
        }        
    }
}