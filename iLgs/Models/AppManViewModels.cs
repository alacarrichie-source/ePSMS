using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace iLgs.Models
{

    public class MenubaseVM
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

        public bool IsAllowed { get; set; }
        public string Result { get; set; }
        public Guid? AccessId { get; set; }
    }

    [MetadataType(typeof(MenubaseMetadata))]
    public partial class Menubase
    {
        //public Menubase()
        //{
        //    this.SubMenu = new HashSet<Menubase>();
        //    this.Accessfiles = new HashSet<Accessfile>();
        //}
        public bool IsAllowed { get; set; }
        public string Result { get; set; }
    }

    public class MenubaseMetadata
    {
        public string SysCode { get; set; }
        public Nullable<int> Sequence { get; set; }
        public Nullable<int> ParentId { get; set; }

        public int? ChildId { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }

        [Display(Name = "Group")]
        public string ObjectParam { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string MenuId { get; set; }

        //public virtual SysCode SysCode1 { get; set; }
        //public virtual ICollection<Menubase> SubMenu { get; set; }
        //public virtual Menubase MainMenu { get; set; }
        //public virtual ICollection<Accessfile> Accessfiles { get; set; }

        //public SysCode SysCode1 { get; set; }

        public ICollection<Menubase> SubMenu { get; set; }
        public Menubase MainMenu { get; set; }
        //public ICollection<MenuAccess> MenuAccess { get; set; }
    }


    public class Menubase_VM
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


        // transient
        public bool IsAllowed { get; set; }

    }

    [MetadataType(typeof(MenubaseAccessMetadata))]
    public partial class MenubaseAccess
    {
        //public MenubaseAccess()
        //{
        //    this.SubMenu = new HashSet<MenubaseAccess>();
        //}

    }

    public class MenubaseAccessMetadata
    {
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

        //public Accessfile Accessfile { get; set; }
        public ICollection<MenubaseAccess> SubMenu { get; set; }
        public MenubaseAccess MainMenu { get; set; }

        //public virtual Accessfile Accessfile { get; set; }
        //public virtual ICollection<MenubaseAccess> SubMenu { get; set; }
        //public virtual MenubaseAccess MainMenu { get; set; }
    }


    public class Access
    {
        public Access()
        {
            IsAdmin = false;
            IsAllowed = false;
            AllowAdd = false;
            AllowEdit = false;
            AllowDelete = false;
            AllowPost = false;
            AllowUnpost = false;
            AllowPrint = false;
            AllowTransfer = false;
        }
        public bool IsAdmin { get; set; }
        public bool IsAllowed { get; set; }
        public bool AllowAdd { get; set; }
        public bool AllowEdit { get; set; }
        public bool AllowDelete { get; set; }
        public bool AllowPost { get; set; }
        public bool AllowUnpost { get; set; }
        public bool AllowPrint { get; set; }
        public bool AllowTransfer { get; set; }

        //public bool AllowAdd
        //{
        //    get
        //    {
        //        return IsAdmin || Actions.Any(w => w.IsAllowed == true && w.MenuAction?.ActionCode == "ADD");
        //    }
        //}
        //public bool AllowEdit
        //{
        //    get
        //    {
        //        return IsAdmin || Actions.Any(w => w.IsAllowed == true && w.MenuAction?.ActionCode == "EDIT");
        //    }
        //}

        //public bool AllowDelete
        //{
        //    get
        //    {
        //        return IsAdmin || Actions.Any(w => w.IsAllowed == true && w.MenuAction?.ActionCode == "DELETE");
        //    }
        //}

        //public bool AllowPost
        //{
        //    get
        //    {
        //        return IsAdmin || Actions.Any(w => w.IsAllowed == true && w.MenuAction?.ActionCode == "POST");
        //    }
        //}
        //public bool AllowUnpost
        //{
        //    get
        //    {
        //        return IsAdmin || Actions.Any(w => w.IsAllowed == true && w.MenuAction?.ActionCode == "UNPOST");
        //    }
        //}

        //public bool AllowPrint
        //{
        //    get
        //    {
        //        return IsAdmin || Actions.Any(w => w.IsAllowed == true && w.MenuAction?.ActionCode == "PRINT");
        //    }
        //}

        public List<MenuAccessAction> Actions { get; set; }
    }    

    public class CopyAccessVM
    {
        [Required]
        [Display(Name = "User Id")]
        public string TargetUserId { get; set; }
        [Display(Name = "User Name")]
        public string TargetUserName { get; set; }

        [Required]
        [Display(Name = "User Id")]
        public string SourceUserId { get; set; }

        [Display(Name = "Full Name")]
        public string TargetNameFull { get; set; }

        [Display(Name = "User Name")]
        public string SourceUserName { get; set; }
        [Display(Name = "Full Name")]
        public string SourceNameFull { get; set; }
        [Display(Name = "System")]
        public string SysCode { get; set; }
    }


    //[MetadataType(typeof(AccessfileMetadata))]
    //public partial class Accessfile
    //{

    //}

    //public class AccessfileMetadata
    //{
    //    public System.Guid RecId { get; set; }
    //    public string SysCode { get; set; }
    //    public string UserId { get; set; }
    //    public Nullable<int> ChildId { get; set; }
    //    public bool AllowAdd { get; set; }
    //    public bool AllowEdit { get; set; }
    //    public bool AllowDelete { get; set; }
    //    public bool AllowPost { get; set; }
    //    public bool AllowUnpost { get; set; }
    //    public string InsertedBy { get; set; }
    //    public Nullable<System.DateTime> InsertedDt { get; set; }
    //    public string UpdatedBy { get; set; }
    //    public Nullable<System.DateTime> UpdatedDt { get; set; }

    //    //public virtual Menubase Menubase { get; set; }
    //    //public virtual ICollection<MenubaseAccess> MenubaseAccesses { get; set; }

    //    public Menubase Menubase { get; set; }
    //    public ICollection<MenubaseAccess> MenubaseAccesses { get; set; }
    //}

    [MetadataType(typeof(AspNetRoleMetadata))]
    public partial class AspNetRole
    {
        //public AspNetRole()
        //{
        //    this.AspNetUserRoles = new HashSet<AspNetUserRole>();
        //}

    }

    public class AspNetRoleMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }

        //public virtual ICollection<AspNetUserRole> AspNetUserRoles { get; set; }

        public ICollection<AspNetUserRole> AspNetUserRoles { get; set; }
    }

    [MetadataType(typeof(AspNetUserMetadata))]
    public partial class AspNetUser
    {
        //public AspNetUser()
        //{
        //    this.AspNetUserRoles = new HashSet<AspNetUserRole>();
        //    this.UserCodes = new HashSet<UserCode>();
        //}        
    }

    public class AspNetUserMetadata
    {
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

        //public virtual ICollection<AspNetUserRole> AspNetUserRoles { get; set; }
        //public virtual UserProfile UserProfile { get; set; }
        //public virtual ICollection<UserCode> UserCodes { get; set; }
        //public virtual UserInfo UserInfo { get; set; }

        public ICollection<AspNetUserRole> AspNetUserRoles { get; set; }
        public UserProfile UserProfile { get; set; }
        public ICollection<UserCode> UserCodes { get; set; }
        public UserInfo UserInfo { get; set; }
    }

    [MetadataType(typeof(AspNetUserRoleMetadata))]
    public partial class AspNetUserRole
    {
        public string CompKeyId { get; set; }

    }

    public class AspNetUserRoleMetadata
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }

        public AspNetUser AspNetUser { get; set; }
        public AspNetRole AspNetRole { get; set; }

        //public virtual AspNetUser AspNetUser { get; set; }
        //public virtual AspNetRole AspNetRole { get; set; }
    }

    [MetadataType(typeof(AspNetUserRoles_ViewMetadata))]
    public partial class AspNetUserRoles_View
    {
        public string CompKeyId { get; set; }

    }

    public class AspNetUserRoles_ViewMetadata
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string NameFull { get; set; }
        public string UserName { get; set; }
    }

    [MetadataType(typeof(AspNetUsers_ViewMetadata))]
    public partial class AspNetUsers_View
    {

    }

    public class AspNetUsers_ViewMetadata
    {
        public string Id { get; set; }
        public string Email { get; set; }

        [Display(Name = "User Id")]
        public string UserId { get; set; }

        [Display(Name = "Last Name")]
        public string NameLast { get; set; }

        [Display(Name = "First Name")]
        public string NameFirst { get; set; }

        [Display(Name = "Middle Initial")]
        public string NameMid { get; set; }

        [Display(Name = "Full Name")]
        public string NameFull { get; set; }
        public Nullable<System.DateTime> Birthday { get; set; }
        public string Sex { get; set; }

        [Display(Name = "Position")]
        public string TelNo { get; set; }

        [Display(Name = "Mobile No")]
        public string MobileNo { get; set; }

        [Display(Name = "House No")]
        public string AddressHouseNo { get; set; }

        [Display(Name = "Street")]
        public string AddressStreet { get; set; }

        [Display(Name = "Subdivision")]
        public string AddressSubdivision { get; set; }

        [Display(Name = "Barangay")]
        public string AddressBarangay { get; set; }

        [Display(Name = "City")]
        public string AddressCity { get; set; }

        [Display(Name = "Province")]
        public string AddressProvince { get; set; }

        [Display(Name = "Zip Code")]
        public string AddressZipCode { get; set; }
        public string InsertedBy { get; set; }
        public Nullable<System.DateTime> InsertedDt { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDt { get; set; }
        public string UserName { get; set; }

        [Display(Name = "Group")]
        public string Department { get; set; }
        public string Division { get; set; }
        public string Section { get; set; }

        [Display(Name = "User Code")]
        public string UserCode { get; set; }

        public bool Active { get; set; }
    }

    [MetadataType(typeof(UserCodeMetadata))]
    public partial class UserCode
    {

    }

    public class UserCodeMetadata
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

        //public virtual AspNetUser AspNetUser { get; set; }

        public AspNetUser AspNetUser { get; set; }
    }

    [MetadataType(typeof(UserCodes_ViewMetadata))]
    public partial class UserCodes_View
    {

    }

    public class UserCodes_ViewMetadata
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

    [MetadataType(typeof(UserInfoMetadata))]
    public partial class UserInfo
    {

    }

    public class UserInfoMetadata
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

        public AspNetUser AspNetUser { get; set; }

        //public virtual AspNetUser AspNetUser { get; set; }
    }

    [MetadataType(typeof(UserProfileMetadata))]
    public partial class UserProfile
    {

    }

    public class UserProfileMetadata
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

        //public virtual AspNetUser AspNetUser { get; set; }

        public AspNetUser AspNetUser { get; set; }
    }

    //[MetadataType(typeof(SysCode))]
    //public partial class SysCode
    //{
    //    public string SysCodeOld { get; set; }
    //    internal sealed class Metadata
    //    {
    //        [Display(Name = "System Code")]
    //        public string SysCode1 { get; set; }
    //        [Display(Name = "Description")]
    //        public string SysDescription { get; set; }
    //    }

    //}
}