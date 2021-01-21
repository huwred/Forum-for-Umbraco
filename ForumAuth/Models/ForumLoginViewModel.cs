using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Forums
{
    public class ForumLoginViewModel
    {
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string EmailAddress { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Enter your password")]
        public string Password { get; set; }

        [HiddenInput(DisplayValue = false)]
        public string ReturnUrl { get; set; }
    }
}