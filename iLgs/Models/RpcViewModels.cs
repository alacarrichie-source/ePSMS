using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class RPCI_VM
    {        
        public System.Guid Id { get; set; }
       
        [Display(Name = "As At")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AsAt { get; set; }

        public string Department { get; set; }

        [Display(Name = "Accountable Officer")]
        public string AccountableOfficer { get; set; }
        public string Designation { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AssumptionDt { get; set; }

        [Display(Name = "Certified correct by")]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Verrified by")]
        public string VerifiedBy { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedDt { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }      
        
        // Transients
    }

    public class RPCIItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RpciId { get; set; }
        [Display(Name = "Item Type")]
        public string ItemType { get; set; }        

        public string Fund { get; set; }
        public string Article { get; set; }
        public string Description { get; set; }

        [Display(Name = "Stock No.")]
        public string StockNo { get; set; }

        public string Unit { get; set; }

        [Display(Name = "Unit Value")]
        public Nullable<decimal> UnitValue { get; set; }

        [Display(Name = "Qty Balance")]
        public Nullable<int> QtyBalance { get; set; }

        [Display(Name = "Qty On hand")]
        public Nullable<int> QtyOnHand { get; set; }

        [Display(Name = "Qty Short/Over")]
        public Nullable<int> QtyShortOver { get; set; }

        [Display(Name = "Value Short/Over")]
        public Nullable<decimal> ValueShortOver { get; set; }

        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }

    public class RPCEFFOPPE_VM
    {
        public System.Guid Id { get; set; }
        
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "As At")]
        public Nullable<System.DateTime> AsAt { get; set; }
        public string Department { get; set; }

        [Display(Name = "Accountable Officer")]
        public string AccountableOfficer { get; set; }
        public string Designation { get; set; }
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AssumptionDt { get; set; }

        [Display(Name = "Certified correct by")]
        public string CertifiedCorrectBy { get; set; }

        [Display(Name = "Approved by")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Verrified by")]
        public string VerifiedBy { get; set; }

        [Display(Name = "Posted by")]
        public string PostedBy { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public System.DateTime UpdatedDt { get; set; }

        [Display(Name = "Posted Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }

        // Transients
    }

    public class RPCEFFOPPEItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RpceffoppeId { get; set; }

        [Display(Name = "Item Type")]
        public string ItemType { get; set; }
        public string Fund { get; set; }
        public string Article { get; set; }
        public string Description { get; set; }

        [Display(Name = "Property No.")]
        public string PropertyNo { get; set; }
        
        [Display(Name = "Cost")]
        public Nullable<decimal> Cost { get; set; }

        public string Location { get; set; }
        public string Condition { get; set; }
        public string Remarks { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
    }

    public class RpcPpeVM
    {        
        public System.Guid Id { get; set; }
        [Display(Name = "As Of Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AsOf { get; set; }
        public string Department { get; set; }
        [Display(Name = "Certified Correct by")]
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
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> PostedDt { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
     
    }

    public class RpcPpeItemVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> RpcPpeId { get; set; }
        public string Fund { get; set; }
        public string Account { get; set; }
        public string Article { get; set; }

        [Display(Name = "Sub Article")]
        public string SubArticle { get; set; }
        public string Type { get; set; }
        public string Brand { get; set; }

        [Display(Name = "Model")]
        public string Model_ { get; set; }

        [Display(Name = "Serial No")]
        public string SerialNo { get; set; }
        public string Others { get; set; }
        public string Color { get; set; }

        [Display(Name = "New Property No.")]
        public string PropNo { get; set; }

        [Display(Name = "Property No.")]
        public string OldPropNo { get; set; }
        public Nullable<decimal> Cost { get; set; }

        [Display(Name = "Acquisition Date")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> AcqDate { get; set; }

        [Display(Name = "Acquisition Mode")]
        public string ActMode { get; set; }
        public string Location { get; set; }

        [Display(Name = "Issued To")]
        public string IssuedTo { get; set; }

        [Display(Name = "Ref. No.")]
        public string RefNo { get; set; }

        [Display(Name = "Ref. Type")]
        public string RefType { get; set; }

        [Display(Name = "New Accountable Officer")]
        public string Officer { get; set; }

        [Display(Name = "Accountable Officer")]
        public string OldOfficer { get; set; }
        public string Condition { get; set; }
        public string Remarks { get; set; }
        public string Annex { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }
}