using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Models
{
    public class Menubase_
    {        
        public string SysCode { get; set; }
        public Nullable<int> Sequence { get; set; }
        public Nullable<int> ParentId { get; set; }
        public int ChildId { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public string ObjectParam { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string MenuId { get; set; }

        public virtual SysCode SysCode1 { get; set; }
        public virtual ICollection<Menubase> SubMenu { get; set; }
        public virtual Menubase MainMenu { get; set; }
        public virtual ICollection<Accessfile> Accessfiles { get; set; }
        public bool IsAllowed { get; set; }

    }


    public class MenubaseAccess_
    {
        //public MenubaseAccess()
        //{
        //    this.SubMenu = new HashSet<MenubaseAccess>();
        //}
        public System.Guid AccessFileId { get; set; }
        public string SysCode { get; set; }
        public Nullable<int> Sequence { get; set; }
        public Nullable<int> ParentId { get; set; }
        public int ChildId { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public string ObjectParam { get; set; }
        public string MenuId { get; set; }
        public string ChildIdAccess { get; set; }
        public string ParentIdAccess { get; set; }
        public string UserId { get; set; }

        public virtual Accessfile Accessfile { get; set; }
        public virtual ICollection<MenubaseAccess> SubMenu { get; set; }
        public virtual MenubaseAccess MainMenu { get; set; }

    }


    public class Accessfile_
    {
        //public Accessfile()
        //{
        //    this.MenubaseAccesses = new HashSet<MenubaseAccess>();
        //}
        public System.Guid RecId { get; set; }
        public string SysCode { get; set; }
        public string UserId { get; set; }
        public Nullable<int> ChildId { get; set; }
        public bool AllowAdd { get; set; }
        public bool AllowEdit { get; set; }
        public bool AllowDelete { get; set; }
        public bool AllowPost { get; set; }
        public bool AllowUnpost { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public virtual Menubase Menubase { get; set; }
        public virtual ICollection<MenubaseAccess> MenubaseAccesses { get; set; }
    }


    public class AspNetRole_
    {
        //public AspNetRole()
        //{
        //    this.AspNetUserRoles = new HashSet<AspNetUserRole>();
        //}
        public string Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<AspNetUserRole> AspNetUserRoles { get; set; }
    }


    public class AspNetUser_
    {
        //public AspNetUser()
        //{
        //    this.AspNetUserRoles = new HashSet<AspNetUserRole>();
        //    this.UserCodes = new HashSet<UserCode>();
        //}        
        public string Id { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public Nullable<System.DateTime> LockoutEndDateUtc { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string UserName { get; set; }
        public System.DateTime DtRegs { get; set; }

        public virtual ICollection<AspNetUserRole> AspNetUserRoles { get; set; }
        public virtual UserProfile UserProfile { get; set; }
        public virtual ICollection<UserCode> UserCodes { get; set; }
        public virtual UserInfo UserInfo { get; set; }
    }


    public class AspNetUserRole_
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }

        public virtual AspNetUser AspNetUser { get; set; }
        public virtual AspNetRole AspNetRole { get; set; }
    }


    public class AspNetUserRoles_View_
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string NameFull { get; set; }
        public string UserName { get; set; }
    }


    public class AspNetUsers_View_
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserId { get; set; }
        public string NameLast { get; set; }
        public string NameFirst { get; set; }
        public string NameMid { get; set; }
        public string NameFull { get; set; }
        public Nullable<System.DateTime> Birthday { get; set; }
        public string Sex { get; set; }
        public string TelNo { get; set; }
        public string MobileNo { get; set; }
        public string AddressHouseNo { get; set; }
        public string AddressStreet { get; set; }
        public string AddressSubdivision { get; set; }
        public string AddressBarangay { get; set; }
        public string AddressCity { get; set; }
        public string AddressProvince { get; set; }
        public string AddressZipCode { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string UserName { get; set; }
    }


    public class UserCode_
    {
        public string UserCode1 { get; set; }
        public string UserId { get; set; }
        public string Department { get; set; }
        public string Division { get; set; }
        public string Section { get; set; }
        public string UserGroup { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public virtual AspNetUser AspNetUser { get; set; }
    }


    public class SysCode_
    {
        //public SysCode()
        //{
        //    this.Menubases = new HashSet<Menubase>();
        //}

        public string SysCode1 { get; set; }
        public string SysDescription { get; set; }
        public string Insertedby { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public virtual ICollection<Menubase> Menubases { get; set; }
    }


    public class UserCodes_View_
    {
        public string UserCode { get; set; }
        public string UserId { get; set; }
        public string Department { get; set; }
        public string Division { get; set; }
        public string Section { get; set; }
        public string UserGroup { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string UserName { get; set; }
        public string NameFull { get; set; }
    }


    public class UserInfo_
    {
        public string RecId { get; set; }
        public Nullable<int> Ownership { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string NameCorporate { get; set; }
        public string NameFirst { get; set; }
        public string NameLast { get; set; }
        public string NameMid { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public Nullable<System.DateTime> BirthDay { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }

        public virtual AspNetUser AspNetUser { get; set; }
    }


    public class UserProfile_
    {
        public string UserId { get; set; }
        public string NameLast { get; set; }
        public string NameFirst { get; set; }
        public string NameMid { get; set; }
        public string NameFull { get; set; }
        public Nullable<System.DateTime> Birthday { get; set; }
        public string Sex { get; set; }
        public string TelNo { get; set; }
        public string MobileNo { get; set; }
        public string AddressHouseNo { get; set; }
        public string AddressStreet { get; set; }
        public string AddressSubdivision { get; set; }
        public string AddressBarangay { get; set; }
        public string AddressCity { get; set; }
        public string AddressProvince { get; set; }
        public string AddressZipCode { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string Department { get; set; }
        public string Division { get; set; }
        public string Section { get; set; }

        public string UserCode { get; set; }
        public virtual AspNetUser AspNetUser { get; set; }
    }
}