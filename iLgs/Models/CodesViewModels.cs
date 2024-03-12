using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    [MetadataType(typeof(CodeMast.Metadata))]
    public partial class CodeMast
    {
        internal sealed class Metadata
        {

            public Nullable<System.Guid> Id { get; set; }

            [Required]
            public string Code { get; set; }
            [Required]
            public string Description { get; set; }
            public string InsertedBy { get; set; }
            public Nullable<System.DateTime> InsertedDt { get; set; }
            public string UpdatedBy { get; set; }
            public Nullable<System.DateTime> UpdatedDt { get; set; }
            public string Desc1Hdg { get; set; }
            public string Desc2Hdg { get; set; }
            public string Desc3Hdg { get; set; }
            public string Desc4Hdg { get; set; }

            [Display(Name = "For Group")]
            //[Required]
            public string Desc5Hdg { get; set; }
            public string CodeHdg { get; set; }
        }
    }

    public class CodextnVM
    {
        public string CodeHdg { get; set; }
        public string Desc1Hdg { get; set; }
        public string Desc2Hdg { get; set; }
        public string Desc3Hdg { get; set; }
        public string Desc4Hdg { get; set; }
        public string Desc5Hdg { get; set; }
        public Guid Id { get; set; }
        public Guid MastId { get; set; }
        //[Required]
        public string Code { get; set; }            
        [Required]
        public string Description { get; set; }
        public string Desc2 { get; set; }
        public string Desc3 { get; set; }
        public string Desc4 { get; set; }
        public string Desc5 { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }        
    }

    public class MenuActionSw
    {
        [Display(Name = "Action")]
        public string Code { get; set; }
        public string Description { get; set; }
        public bool IsAllowed { get; set; }
        public Guid? AccessId { get; set; }
        public Guid? ActionId { get; set; }
    }

    public class DepartmentUserVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> DeptId { get; set; }
        public string UserId { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        // transients

        public string Email { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Full Name")]
        public string NameFull { get; set; }
    }

    public class AccountableOfficerVM
    {
        public System.Guid Id { get; set; }
        public Nullable<System.Guid> DeptId { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }

        [Display(Name = "Date of Assumption")]
        [DisplayFormat(NullDisplayText = "", DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> DateAssumption { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        
    }
}