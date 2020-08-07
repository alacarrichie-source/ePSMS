using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class REPORT_UserAccess_VM
    {
        [Display(Name = "System")]
        public string SysCode { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Save to Excel")]
        public int Save { get; set; }
        
    }

    public class REPORT_UserRoles_VM
    {
        [Display(Name = "Role")]
        public string RoleId { get; set; }
        
        [Display(Name = "Save to Excel")]
        public int Save { get; set; }

    }

}