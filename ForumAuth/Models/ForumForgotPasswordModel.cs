﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forums
{
    public class ForumForgotPasswordModel
    {
        [DisplayName("Email Address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string EmailAddress { get; set; }
    }
}