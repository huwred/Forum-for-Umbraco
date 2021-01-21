using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forums
{
    public class ForumPasswordResetModel
    {
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string EmailAddress { get; set; }

        [DisplayName("New Password")]
        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; }

        [DisplayName("Confirm Password")]
        [Required(ErrorMessage = "Please enter your password")]
        // [EqualTo("Password", ErrorMessage = "You're passwords must match")]
        public string ConfirmPassword { get; set; }
    }
}