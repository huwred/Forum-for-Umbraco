using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MediaWizards.Forums.Models
{
    /// <summary>
    ///  constants for view names and keys, to reduce my fat fingers.
    ///  
    ///  tweak this stuff, if you want to have things in diffrent places, or
    ///  use diffrent views for things.
    /// </summary>
    public static class ForumAuthConstants
    {
        public const string ForgotPasswordKey = "ForgotPasswordForm";

        public const string LoginKey = "LoginForm";

        public const string ResetPasswordKey = "ResetPasswordForm";

        public const string RegisterKey = "RegisterForm";

        public const string ProfileKey = "ProfileForm";

        public const string ProfileEditKey = "ProfileEditForm";

        /// <summary>
        ///  properties on the member ....
        /// </summary>

        public const string ResetRequestGuidPropery = "resetGuid";
        public const string AccountVerifiedProperty = "hasVerifiedAccount";
        public const string AccountJoinedDateProperty = "joinedDate";
        public const string AccountNotifyProperty = "receiveNotifications";


    }
}