using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Forums
{
    /// <summary>
    ///  constants for view names and keys, to reduce my fat fingers.
    ///  
    ///  tweak this stuff, if you want to have things in diffrent places, or
    ///  use diffrent views for things.
    /// </summary>
    public static class ForumAuthConstants
    {
        public const string ForgotPasswordView = "Member/ForumAuth.ForgotPassword";
        public const string ForgotPasswordKey = "ForgotPasswordForm";

        public const string LoginView = "Member/ForumAuth.Login";
        public const string LoginKey = "LoginForm";

        public const string ResetPasswordView = "Member/ForumAuth.ResetPassword";
        public const string ResetPasswordKey = "ResetPasswordForm";

        public const string RegisterView = "Member/ForumAuth.Register";
        public const string RegisterKey = "RegisterForm";

        public const string ProfileView = "Member/ForumAuth.ViewProfile";
        public const string ProfileKey = "ProfileForm";

        public const string ProfileEditView = "Member/ForumAuth.EditProfile";
        public const string ProfileEditKey = "ProfileEditForm";

        /// <summary>
        ///  properties on the member ....
        /// </summary>

        public const string ResetRequestGuidPropery = "resetGuid";
        public const string AccountVerifiedProperty = "hasVerifiedAccount";
        public const string AccountJoinedDateProperty = "joinedDate";
        // public const string MemberDisplayNameProperty = "displayName";

        public const string NewAccountMemberType = "Member";

        public const string LoginUrl = "/login/";
        public const string ResetUrl = "/reset/";
        public const string RegisterUrl = "/register/";
        public const string VerifyUrl = "/verify/";

    }
}