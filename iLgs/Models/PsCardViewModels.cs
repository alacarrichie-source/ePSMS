using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class PsCardVM
    {
        public PsCardVM()
        {
            this.AllField = new AllField() { Id = this.Id };
        }

        public System.Guid Id { get; set; }

        [Display(Name = "Article")]
        [Required]
        public Nullable<System.Guid> ItemCodeId { get; set; }

        [Required]
        public string Fund { get; set; }

        [MaxLength(900)]
        [Display(Name = "Item Description")]
        public string Description { get; set; }
        public string Unit { get; set; }

        [Display(Name = "Card Category")]
        public string CardCategory { get; set; } // P or S only, to identify where the item belongs.

        [Display(Name = "Property/Stock Card No.")]
        [Required]
        public string PsNo { get; set; }
        
        [Display(Name = "Property/Stock Name")]
        public string PsName { get; set; }

        [Display(Name = "Previous Property/Stock Card No.")]
        public string PrevPsNo { get; set; }

        //[Display(Name = "Acq. Date")]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //public Nullable<System.DateTime> AcqDate { get; set; }

        //[Display(Name = "Acq. Mode")]
        //public string AcqMode { get; set; }

        [Display(Name = "From Donation")]
        public Nullable<bool> FromDonation { get; set; }

        public Nullable<decimal> Amount { get; set; }
        //public string Brand { get; set; }

        //[Display(Name = "Model")]
        //public string Model_ { get; set; }

        [Display(Name = "Inserted By")]
        public string InsertedBy { get; set; }

        [Display(Name = "Iserted Dt")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> InsertedDt { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }

        [Display(Name = "Updated Dt")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted Dt")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy hh:mm tt}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        public AllField AllField { get; set; }

        // Transients

        [Display(Name = "Article")]
        public string Item { get; set; }
        public string ItemNo { get; set; }
        public string ItemCode { get; set; }

        [Display(Name = "Account")]
        public string ItemType { get; set; }
        public string ItemTypeCode { get; set; }

        [Display(Name = "Sub-Account")]
        public string SubAccount { get; set; }

        [Display(Name = "Sub-Account 1")]
        public string SubAccount1 { get; set; }

        [Display(Name = "Sub-Account 2")]
        public string SubAccount2 { get; set; }

        [Display(Name = "Sub-Account 3`")]
        public string SubAccount3 { get; set; }

        [Display(Name = "Sub-Account 4")]
        public string SubAccount4 { get; set; }

        public string SubAccountCode { get; set; }

        [Display(Name = "Partial Page")]
        public string PartialPage { get; set; }

        public Guid? SelectedId { get; set; }

        public string Mode { get; set; }
        public bool IsDuplicateStockNo { get; set; }

        [Display(Name = "Item Count")]
        public int? ItemCount { get; set; }

        [Display(Name = "Not Posted")]
        public int? NotPosted { get; set; }
    }

    public class PsCardItemVM 
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> ItemCodeId { get; set; }
        public Nullable<System.Guid> GroupId { get; set; }
        public Nullable<System.Guid> PsCardId { get; set; }
        public Nullable<System.Guid> OrderItemId { get; set; }
        public Nullable<System.Guid> TransferRefId { get; set; }
        public Nullable<System.Guid> TransferId { get; set; }
        public Nullable<System.Guid> ParentId { get; set; }

        [Required]
        [Display(Name = "PO/Cut-off Date (mm/dd/yyyy)")]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PoDate { get; set; }

        [Required]
        [Display(Name = "PO No.")]
        public string PoNo { get; set; }

        [Display(Name = "AIR Date (mm/dd/yyyy)")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]

        public Nullable<System.DateTime> AirDate { get; set; }

        [Display(Name = "AIR No.")]
        public string AirNo { get; set; }

        [Display(Name = "Issuance Date (mm/dd/yyyy)")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MMMM dd, yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AirIssueDate { get; set; }

        [Required]
        public Nullable<decimal> Qty { get; set; }

        [Display(Name = "Qty. Issued")]
        public Nullable<decimal> QtyIss { get; set; }

        //[Required]
        [Display(Name = "Qty. Balance")]
        public Nullable<decimal> QtyBal { get; set; }

        [Display(Name = "Transit-In Qty")]
        public Nullable<decimal> TransferIn { get; set; }

        [Display(Name = "Transit-Out Qty")]
        public Nullable<decimal> TransferOut { get; set; }

        [Display(Name = "Transaction Type")]
        public string TranType { get; set; }

        public Nullable<int> Days { get; set; }

        [Required]
        [Display(Name = "PO Unit of Measurement")]
        public string Unit { get; set; }

        [Required]
        [Display(Name = "PO Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }

        [Display(Name = "PO Amount")]
        public Nullable<decimal> Amount { get; set; }

        [Display(Name = "Issued Amount")]
        public Nullable<decimal> IssueAmount { get; set; }

        [Display(Name = "Balance Amount")]
        public Nullable<decimal> BalanceAmount { get; set; }

        [Display(Name = "Price Rate (%)")]
        public Nullable<decimal> PriceRate { get; set; }

        [Display(Name = "Pro-rated Cost")]
        public Nullable<decimal> ProRatedCost{ get; set; }

        [Display(Name = "Additional Cost")]
        public Nullable<decimal> AddCost { get; set; }

        [Display(Name = "Total Unit Cost")]
        public Nullable<decimal> TUnitCost { get; set; }

        [Display(Name = "Total Cost")]
        public Nullable<decimal> GTotalCost { get; set; }

        public string Remarks { get; set; }

        [Display(Name = "Originating PO Department")]
        [Required]
        public Nullable<System.Guid> DeptId { get; set; }

        [Display(Name = "Moved to Location")]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Department Display")]
        public string DeptDisplay { get; set; }

        [Display(Name = "Dept. Code")]
        public string DeptCode { get; set; }

        [Required]
        [Display(Name = "PO Description")]
        public string Description { get; set; }

        [Display(Name = "Other Particulars")]
        public string OtherDesc { get; set; }

        [Display(Name = "Other Particulars Qty *for monoblocks and books only")]
        public Nullable<int> OtherQty { get; set; }

        [Display(Name = "For ICS")]
        public Nullable<bool> IsForICS { get; set; }

        [Display(Name = "Consumable")]
        public Nullable<bool> IsConsumable { get; set; }

        [Display(Name = "Incorporated")]
        public Nullable<bool> IsIncorporated { get; set; }

        [Display(Name = "Others")]
        public Nullable<bool> IsOthers { get; set; }

        public string Type { get; set; }

        [Display(Name = "Remarks")]
        public string OtherRemarks { get; set; }

        [Required]
        [Display(Name = "Inventory/For Distribution")]
        public string InvDist { get; set; }
        
        [Display(Name = "Mode of Acquisition")]
        public string AcqMode { get; set; }

        [Display(Name = "Date Acquired")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcqDate { get; set; }

        [Display(Name = "Area Sold/Donated")]
        public Nullable<decimal> AreaSoldDonated { get; set; }

        [Display(Name = "Construction Year")]
        public Nullable<int> ConstructionYear { get; set; }

        [Display(Name = "Vendor/Donor")]
        public string Vendor { get; set; }

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

        [Display(Name = "Old Amount")]
        public Nullable<decimal> OldAmount { get; set; }

        [Display(Name = "Phase No.")]
        public Nullable<int> PhaseNo { get; set; }

        [Display(Name = "Phase Amount")]
        public Nullable<decimal> PhaseAmount { get; set; }

        [Display(Name = "Previous Property/Stock No.")]
        public string PrevPsNo { get; set; }

        // Transients
        [Display(Name = "Inventory/For Distribution")]
        public string InvDistDesc { get; set; }
        public string Article { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }
        //public string Department { get { return _Deparment?.Description; } }

        [Display(Name = "Location")]
        public string LocCode { get; set; }
        //public string LocCode { get { return _Location?.Code; } }

        [Display(Name = "Location")]
        public string Location { get; set; }
        //public string Location { get { return _Location?.Description; } }

        public decimal? ParBalance { get; set; } = 0;
        public decimal? IcsBalance { get; set; } = 0;
        public string StockNo { get; set; }
        [Display(Name = "Remaining Balance")]
        public Nullable<decimal> RemBalance { get; set; }

        [Display(Name = "Transit Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> TransDate { get; set; }

        [Display(Name = "Set/Lot No.")]
        public string SetLotNo { get; set; }

        [Display(Name = "Set/Lot Amount")]
        public Nullable<decimal> SetLotAmount { get; set; }

        [Display(Name = "Set/Lot Remarks")]
        public string SetLotRemarks { get; set; }

        [Display(Name = "FPP")]
        public string FPP { get; set; }

        public bool? IsWithItemExtn { get; set; } = false;

        public string SelectedIds { get; set; }

        [Display(Name = "Sub-Accounts")]
        public string SubAccount { get; set; }
        public string Account { get; set; }
        public string Fund { get; set; }
    }

    public class PsCardItemIssuanceVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemId { get; set; }
        
        [Display(Name = "Department")]
        public Nullable<System.Guid> DeptId { get; set; }

        [Required]
        [Display(Name = "Issuance To")]        
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }
        
        [Display(Name = "Issued Date")]
        [Required]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> IssuedDate { get; set; }

        [Required]
        public Nullable<decimal> Qty { get; set; }
        public Nullable<decimal> Amount { get; set; }

        public string IssuedToCode { get; set; }

        [Display(Name = "Issued to Description")]
        public string IssuedToDescription { get; set; }

        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }

        [Display(Name = "Posted date")]
        public Nullable<System.DateTime> PostedDt { get; set; }

        // Transients
        public string Department { get; set; }

        [Display(Name = "\"Issuance To\" Reference")]
        public string Location { get; set; }
                
        [Display(Name = "Unit Cost")]
        public Nullable<decimal> UnitCost { get; set; }
        public int? IssuedToSw { get; set; }

        public string SelectedIds { get; set; }
        public bool? IsWithItemExtn { get; set; } = false;
    }

    public class FieldSw
    {
        public bool InvDist { get; set; } = false;
        public bool AcqDate { get; set; } = false;
        public bool AcqYear { get; set; } = false;
        public bool PhaseNo { get; set; } = false;
        public bool Type { get; set; } = false;
        public bool OtherDesc { get; set; } = false;
        public bool CapitalOutlay { get; set; } = false;
    }

    public class PsCardItemLocationVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> PsCardItemExtnId { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> TransDate { get; set; }

        [Display(Name = "Transaction Type")]
        public string TransType { get; set; }
        public Nullable<System.Guid> TransId { get; set; }

        [Required]
        [Display(Name = "Moved to Location")]
        public Nullable<System.Guid> LocationId { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; }

        public string Location { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    
        public PsCardItemExtn PsCardItemExtn { get; set; }
        public PsCardItemIssuance PsCardItemIssuance { get; set; }
    }
}