using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Forums
{
    public class ForumRegisterViewModel
    {
        [DisplayName("Name")]
        [Required(ErrorMessage = "Enter your name")]
        public string Name { get; set; }

        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid Email address")]
        [Remote("CheckForEmailAddress", "ForumAuthSurface", ErrorMessage = "The email address is already in use")]
        public string EmailAddress { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter your password")]
        [MinLength(10)]
        public string Password { get; set; }

        [DisplayName("Confirm Password")]
        [Required(ErrorMessage = "Please enter your password")]
        // [EqualTo("Password", ErrorMessage = "You're passwords must match")]
        public string ConfirmPassword { get; set; }
    }
}