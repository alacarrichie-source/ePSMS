using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iLgs.Models
{
    #region Add-ons
    public class UserInfoViewModel : IValidatableObject
    {
        public string RecId { get; set; }

        [Required]
        public int Ownership { get; set; }
        public string UserId { get; set; }

        public string Title { get; set; }

        [Display(Name = "Corporation Name")]
        public string NameCorporate { get; set; }

        [Display(Name = "Last Name")]
        public string NameLast { get; set; }

        [Display(Name = "First Name")]
        public string NameFirst { get; set; }

        [Display(Name = "Middle Name")]
        public string NameMid { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Country { get; set; }

        [Required]
        [Display(Name = "Province")]
        public string Province { get; set; }

        [Required]
        [Display(Name = "City/Town")]
        public string City { get; set; }

        [Required]
        [Display(Name = "Street")]
        public string Street { get; set; }

        [Required]
        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; }

        [Required]
        [Display(Name = "Birthday")]
        public System.DateTime BirthDay { get; set; }

        public System.DateTime UpdatedDt { get; set; }
        

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Ownership == 0)
            {
                if (NameFirst == null || NameLast == null || NameMid == null)
                {
                    yield return new ValidationResult
                     ("Account name is required.", new[] { "NameFirst", "NameLast", "NameMid" });
                }
            }
            else
            {
                if (NameCorporate == null)
                {
                    yield return new ValidationResult
                     ("Name of corporation is required.", new[] { "NameCorporate" });
                }
            }
        }

    }

    #endregion
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        //[Required]
        //[Display(Name = "Email")]
        //[EmailAddress]
        //public string Email { get; set; }

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        //[Required]
        //[Display(Name = "Middle Name")]
        //public string NameMid { get; set; }

        //[Required]
        //[Display(Name = "Last Name")]
        //public string NameLast { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
