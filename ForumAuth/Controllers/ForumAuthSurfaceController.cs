using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Umbraco.Web;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Forums
{
    /// <summary>
    /// ForumAuthSurfaceController
    /// </summary>
    public class ForumAuthSurfaceController : SurfaceController
    {
        public ActionResult RenderLogin()
        {
            ForumLoginViewModel login = new ForumLoginViewModel();

            Logger.Info<ForumAuthSurfaceController>("Login URL: {0}", HttpContext.Request.Url?.AbsolutePath);

            login.ReturnUrl = HttpContext.Request.Url?.ToString() ;
            if ( HttpContext.Request.Url?.AbsolutePath == Umbraco.GetDictionaryValue("ForumAuthConstants.LoginUrl","/login").TrimEnd('/'))
            {
                login.ReturnUrl = "/forums";
            }

            return PartialView(Umbraco.GetDictionaryValue("ForumAuthConstants.LoginView","Member/ForumAuth.Login") , login);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleLogin(ForumLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }


            if (Members.IsLoggedIn())
            {
                return RedirectToRoute(model.ReturnUrl);
            }

            // login in the user
            try
            {
                if (Members.Login(model.EmailAddress, model.Password))
                {
                    // logged in
                    var member = Members.GetByEmail(model.EmailAddress);


                    if (member != null)
                    {
                        if (member.Value<bool>(ForumAuthConstants.AccountVerifiedProperty))
                        {
                            // a valid and verified user here be!
                            TempData["returnUrl"] = model.ReturnUrl;
                            return RedirectToCurrentUmbracoPage();
                        }
                        else
                        {
                            // we need to validate this account before they can logon.
                            ModelState.AddModelError(ForumAuthConstants.LoginKey, 
                                Umbraco.GetDictionaryValue("MemberAuth.Login.NotVerified", "Email has not been verified"));

                            Members.Logout();
                            return CurrentUmbracoPage();
                        }
                    }
                    else
                    {
                        // can't find the user...?
                        ModelState.AddModelError(ForumAuthConstants.LoginKey, 
                            Umbraco.GetDictionaryValue("MemberAuth.Login.NoUser", "Invalid Details"));
                    }
                }
                else
                {
                    // can't login this person...
                    ModelState.AddModelError(ForumAuthConstants.LoginKey, 
                        Umbraco.GetDictionaryValue("MemberAuth.Login.LoginFail","Invalid Details"));
                }

            }
            catch (Exception ex)
            {
                // somethig big time went boom...
                Logger.Error<ForumAuthSurfaceController>("Error logging on {0}", ex.ToString());
                ModelState.AddModelError(ForumAuthConstants.LoginKey, "Error logging on" + ex);
            }

            return CurrentUmbracoPage();
        }

        public ActionResult Logout()
        {
            if (Members.IsLoggedIn())
            {
                Members.Logout();
                TempData["returnUrl"] = "/forums";
                return Content( Umbraco.GetDictionaryValue("MemberAuth.Logout.LoggedOut", "You have been logged out"));
            }
            else
            {
                return Content( Umbraco.GetDictionaryValue("MemberAuth.Logout.NotLoggedIn", "Wasn't logged in"));
            }
        }


        public ActionResult RenderForgotPassword()
        {
            return PartialView( Umbraco.GetDictionaryValue("ForumAuthConstants.ForgotPasswordView","Member/ForumAuth.ForgotPassword") , new ForumForgotPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleForgotPassword(ForumForgotPasswordModel model)
        {
            TempData["ResetSent"] = false;
            if (!ModelState.IsValid)
            {
                return PartialView(Umbraco.GetDictionaryValue("ForumAuthConstants.ForgotPasswordView","Member/ForumAuth.ForgotPassword"), model);
            }

            // var membershipHelper = new MembershipHelper(UmbracoContext.Current);
            var memberService = Services.MemberService;

            // var member = membershipHelper.GetByEmail(model.EmailAddress);
            var member = memberService.GetByEmail(model.EmailAddress);

            if (member != null)
            {
                // we found a user with that email ....
                DateTime expires = DateTime.Now.AddMinutes(20);
                var guid = new Guid("00000000-0000-0000-0000-" + expires.ToString("ddMMyyHHmmss"));
                //00000000-0000-0000-0000-ddMMyyHHmmss
                member.SetValue(ForumAuthConstants.ResetRequestGuidPropery, guid);
                memberService.Save(member);

                // send email....
                ForumEmailHelper helper = new ForumEmailHelper();
                helper.SendResetPassword(Umbraco,member.Email, guid.ToString());

                TempData["ResetSent"] = true;
            }
            else
            {
                ModelState.AddModelError(ForumAuthConstants.ForgotPasswordKey, 
                    Umbraco.GetDictionaryValue("MemberAuth.Reset.NoUser", "No user found"));
                return PartialView(Umbraco.GetDictionaryValue("ForumAuthConstants.ForgotPasswordView","Member/ForumAuth.ForgotPassword"));
            }

            return CurrentUmbracoPage();
        }


        public ActionResult RenderResetPassword()
        {
            return PartialView(Umbraco.GetDictionaryValue("ForumAuthConstants.ResetPasswordView","Member/ForumAuth.ResetPassword"), new ForumPasswordResetModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleResetPassword(ForumPasswordResetModel model)
        {
            TempData["Success"] = false;

            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var memberService = Services.MemberService;

            try
            {
                var member = memberService.GetByEmail(model.EmailAddress);

                if (member != null)
                {
                    var resetGuid = Request.QueryString["resetGUID"];

                    if (!string.IsNullOrWhiteSpace(resetGuid))
                    {
                        if (member.GetValue<string>(ForumAuthConstants.ResetRequestGuidPropery) == resetGuid)
                        {
                            // ok so the match. check to see if it hasn't expired...
                            var guidTime = resetGuid.Replace("00000000-0000-0000-0000-","");
                            DateTime expiry = DateTime.ParseExact(guidTime, "ddMMyyHHmmss", null);

                            // is expiry less than now.
                            if (DateTime.Now.CompareTo(expiry) < 0)
                            {
                                memberService.SavePassword(member, model.Password);
                                member.SetValue(ForumAuthConstants.ResetRequestGuidPropery, string.Empty);
                                memberService.Save(member);

                                TempData["Success"] = true;
                                return CurrentUmbracoPage();
                            }
                            else
                            {
                                ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey,
                                    Umbraco.GetDictionaryValue("MemberAuth.Reset.Expired", "The reset request has expired"));
                                return CurrentUmbracoPage();
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey, 
                                Umbraco.GetDictionaryValue("MemberAuth.Reset.NoRequest", "No reset request has been found"));
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey, 
                            Umbraco.GetDictionaryValue("MemberAuth.Reset.NoAccount", "Cannot find account"));
                    }
                }
                else
                {
                    ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey,
                        Umbraco.GetDictionaryValue("MemberAuth.Reset.NoAccount", "Cannot find account"));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey, "Error Resetting Password: " + ex.Message);
            }

            return CurrentUmbracoPage();
        }

        public ActionResult RenderRegister()
        {
            return PartialView(Umbraco.GetDictionaryValue("ForumAuthConstants.RegisterView","Member/ForumAuth.Register"), new ForumRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleRegister(ForumRegisterViewModel model)
        {
            TempData["RegisterComplete"] = false;

            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var memberService = Services.MemberService;

            if ( EmailAddressExists(model.EmailAddress ))
            {
                ModelState.AddModelError(ForumAuthConstants.RegisterKey,
                    Umbraco.GetDictionaryValue("MemberAuth.Register.ExistingEmail", "Email Address is already in use"));
                return CurrentUmbracoPage();
            }

            if ( !IsValidEmailDomain(model.EmailAddress ))
            {
                ModelState.AddModelError(ForumAuthConstants.RegisterKey, 
                    Umbraco.GetDictionaryValue("MemberAuth.Register.InvalidDomain", "You cannot register for this site with that email address"));
                return CurrentUmbracoPage();
            }

            var memberTypeService = Services.MemberTypeService;
            var membertype = Services.LocalizationService.GetDictionaryItemByKey("ForumAuthConstants.NewAccountMemberType");
            var memberType = memberTypeService.Get(membertype.GetDefaultValue());

            try
            {
                var newMember = memberService.CreateMemberWithIdentity(
                    model.EmailAddress, model.EmailAddress, model.Name, memberType);

                memberService.SavePassword(newMember, model.Password);
                newMember.SetValue(ForumAuthConstants.AccountVerifiedProperty, false);
                memberService.Save(newMember);

                // add new user to any groups ?
                var groupsToAdd = CurrentPage.Value<string>("defaultMembership");

                if (!string.IsNullOrWhiteSpace(groupsToAdd))
                {
                    var _mgs = Services.MemberGroupService;
                    var groups = groupsToAdd.Split(',');

                    foreach (var group in groups)
                    {
                        var memberGroup = _mgs.GetByName(group);
                        if (memberGroup != null)
                        {
                            Logger.Info<ForumAuthSurfaceController>("Adding user to group {0}",group);
                            memberService.AssignRole(newMember.Id, group);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(ForumAuthConstants.RegisterKey, "Error creating account:" + ex);
                return CurrentUmbracoPage();
            }

            // now do the verification stuff.
            var member = memberService.GetByEmail(model.EmailAddress);
            if (member != null)
            {
                member.SetValue(ForumAuthConstants.AccountJoinedDateProperty, DateTime.Now.ToString("dd-MMM-yyyy @ HH:mm:ss"));
                memberService.Save(member);

                // send out the email (use the user key as the guid)
                ForumEmailHelper helper = new ForumEmailHelper();
                helper.SendVerifyAccount(Umbraco,model.EmailAddress, member.Key.ToString());
            }

            TempData["RegisterComplete"] = true;
            return CurrentUmbracoPage();
        }

        public ActionResult RenderVerifyEmail(string guid)
        {
            Logger.Info<ForumAuthSurfaceController>("Verifiing: {0}", guid);

            var memberService = Services.MemberService;

            if (Guid.TryParse(guid, out var userKey))
            {
                var member = memberService.GetByKey(userKey);

                if (member != null)
                {
                    member.SetValue(ForumAuthConstants.AccountVerifiedProperty, true);
                    memberService.Save(member);
                    TempData["returnUrl"] = "/login";
                    return Content(Umbraco.GetDictionaryValue("MemberAuth.Verfiy.Verified", "Account has been verified"));
                }
                else
                {
                    return Content(
                        Umbraco.GetDictionaryValue("MemberAuth.Verfiy.NoMatch", "Can't find an account that matches"));
                }
            }
            else
            {
                return Content(
                    Umbraco.GetDictionaryValue("MemberAuth.Verify.InvalidCode", "Not a valid account verification"));
            }
        }

        public ActionResult RenderProfile()
        {

            return PartialView(Umbraco.GetDictionaryValue("ForumAuthConstants.ProfileView","Member/ForumAuth.ViewProfile"));
        }
        private bool EmailAddressExists(string emailAddress)
        {
            var email = Members.GetByEmail(emailAddress);

            if (email != null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public JsonResult CheckForEmailAddress(string emailAddress)
        {
            if ( EmailAddressExists(emailAddress))
            {
                return Json(Umbraco.GetDictionaryValue("MemberAuth.Register.ExistingEmail", "Email Address is already in use"),JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }

        private bool IsValidEmailDomain(string emailAddress)
        {
            var whitelist = CurrentPage.Value<string>("domainWhitelist").ToLower();
            var blacklist = CurrentPage.Value<string>("domainBlacklist").ToLower();

            if (emailAddress.Contains("@"))
            {
                var domain = emailAddress.Substring(emailAddress.IndexOf("@", StringComparison.Ordinal)).ToLower();

                if (!string.IsNullOrWhiteSpace(whitelist) && whitelist.Split(new []{','},StringSplitOptions.RemoveEmptyEntries).Contains(domain))
                {
                        return true;
                }

                if (!string.IsNullOrWhiteSpace(blacklist) && blacklist.Split(new []{','},StringSplitOptions.RemoveEmptyEntries).Contains(domain))
                {
                    return false;
                }

                try
                { //check if the domain exists or not
                    var host = System.Net.Dns.GetHostEntry(domain);
                    if (host.AddressList.Any())
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info<ForumAuthSurfaceController>("Invalid domain: {0} - {1}", domain, ex.Message);
                    if (ex.Message == "No such host is known")
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        public JsonResult CheckForValidEmail(string emailAddress)
        {
            if (IsValidEmailDomain(emailAddress))
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(Umbraco.GetDictionaryValue("MemberAuth.Register.InvalidDomain", "There was a problem with your email address"), JsonRequestBehavior.AllowGet);
            }
        }

    }
}