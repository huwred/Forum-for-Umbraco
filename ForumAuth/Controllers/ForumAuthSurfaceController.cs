﻿using System;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Umbraco.Web;
using Umbraco.Core.Logging;

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

            Logger.Info<ForumAuthSurfaceController>("Login URL: {0}", HttpContext.Request.Url.AbsolutePath);

            login.ReturnUrl = HttpContext.Request.Url.ToString() ;
            if ( HttpContext.Request.Url.AbsolutePath == ForumAuthConstants.LoginUrl.TrimEnd('/'))
            {
                login.ReturnUrl = "/forums";
            }

            return PartialView( ForumAuthConstants.LoginView, login);
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
                                GetDictionaryOrDefault("MemberAuth.Login.NotVerified", "Email has not been verified"));

                            Members.Logout();
                            return CurrentUmbracoPage();
                        }
                    }
                    else
                    {
                        // can't find the user...?
                        ModelState.AddModelError(ForumAuthConstants.LoginKey, 
                            GetDictionaryOrDefault("MemberAuth.Login.NoUser", "Invalid Details"));
                    }
                }
                else
                {
                    // can't login this person...
                    ModelState.AddModelError(ForumAuthConstants.LoginKey, 
                        GetDictionaryOrDefault("MemberAuth.Login.LoginFail","Invalid Details"));
                }

            }
            catch (Exception ex)
            {
                // somethig big time went boom...
                Logger.Error<ForumAuthSurfaceController>("Error logging on {0}", ex.ToString());
                ModelState.AddModelError(ForumAuthConstants.LoginKey, "Error logging on" + ex.ToString());
            }

            return CurrentUmbracoPage();
        }

        public ActionResult Logout()
        {
            if (Members.IsLoggedIn())
            {
                Members.Logout();
                TempData["returnUrl"] = "/forums";
                return Content( GetDictionaryOrDefault("MemberAuth.Logout.LoggedOut", "You have been logged out"));
            }
            else
            {
                return Content( GetDictionaryOrDefault("MemberAuth.Logout.NotLoggedIn", "Wasn't logged in"));
            }
        }


        public ActionResult RenderForgotPassword()
        {
            return PartialView( ForumAuthConstants.ForgotPasswordView , new ForumForgotPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleForgotPassword(ForumForgotPasswordModel model)
        {
            TempData["ResetSent"] = false;
            if (!ModelState.IsValid)
            {
                return PartialView(ForumAuthConstants.ForgotPasswordView, model);
            }

            // var membershipHelper = new MembershipHelper(UmbracoContext.Current);
            var memberService = Services.MemberService;

            // var member = membershipHelper.GetByEmail(model.EmailAddress);
            var member = memberService.GetByEmail(model.EmailAddress);

            if (member != null)
            {
                // we found a user with that email ....
                DateTime expires = DateTime.Now.AddMinutes(20);
                
                member.SetValue(ForumAuthConstants.ResetRequestGuidPropery, expires.ToString("ddMMyyyyHmmssFFFF"));
                memberService.Save(member);

                // send email....
                ForumEmailHelper helper = new ForumEmailHelper();
                helper.SendResetPassword(Umbraco,member.Email, expires.ToString("ddMMyyyyHmmssFFFF"));

                TempData["ResetSent"] = true;
            }
            else
            {
                ModelState.AddModelError(ForumAuthConstants.ForgotPasswordKey, 
                    GetDictionaryOrDefault("MemberAuth.Reset.NoUser", "No user found"));
                return PartialView(ForumAuthConstants.ForgotPasswordView);
            }

            return CurrentUmbracoPage();
        }


        public ActionResult RenderResetPassword()
        {
            return PartialView(ForumAuthConstants.ResetPasswordView, new ForumPasswordResetModel());
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

                            DateTime expiry = DateTime.ParseExact(resetGuid, "ddMMyyyyHHmmssFFFF", null);

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
                                    GetDictionaryOrDefault("MemberAuth.Reset.Expired", "The reset request has expired"));
                                return CurrentUmbracoPage();
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey, 
                                GetDictionaryOrDefault("MemberAuth.Reset.NoRequest", "No reset request has been found"));
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey, 
                            GetDictionaryOrDefault("MemberAuth.Reset.NoAccount", "Cannot find account"));
                    }
                }
                else
                {
                    ModelState.AddModelError(ForumAuthConstants.ResetPasswordKey,
                        GetDictionaryOrDefault("MemberAuth.Reset.NoAccount", "Cannot find account"));
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
            return PartialView(ForumAuthConstants.RegisterView, new ForumRegisterViewModel());
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
                    GetDictionaryOrDefault("MemberAuth.Register.ExistingEmail", "Email Address is already in use"));
                return CurrentUmbracoPage();
            }

            if ( !IsValidEmailDomain(model.EmailAddress ))
            {
                ModelState.AddModelError(ForumAuthConstants.RegisterKey, 
                    GetDictionaryOrDefault("MemberAuth.Register.InvalidDomain", "You cannot register for this site with that email address"));
                return CurrentUmbracoPage();
            }

            var memberTypeService = Services.MemberTypeService;
            var memberType = memberTypeService.Get(ForumAuthConstants.NewAccountMemberType);

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
                ModelState.AddModelError(ForumAuthConstants.RegisterKey, "Error creating account:" + ex.ToString());
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

            Guid userKey;
            if (Guid.TryParse(guid, out userKey))
            {
                var member = memberService.GetByKey(userKey);

                if (member != null)
                {
                    member.SetValue(ForumAuthConstants.AccountVerifiedProperty, true);
                    memberService.Save(member);
                    TempData["returnUrl"] = "/login";
                    return Content(GetDictionaryOrDefault("MemberAuth.Verfiy.Verified", "Account has been verified"));
                }
                else
                {
                    return Content(
                        GetDictionaryOrDefault("MemberAuth.Verfiy.NoAccount", "Can't find account for guid"));
                }
            }
            else
            {
                return Content(
                    GetDictionaryOrDefault("MemberAuth.Verify.NoAccount", "Not a valid account guid"));
            }
        }

        public ActionResult RenderProfile()
        {

            return PartialView(ForumAuthConstants.ProfileView);
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
                return Json(string.Format("The email {0} is already in use", emailAddress),JsonRequestBehavior.AllowGet);
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

            //Logger.Info<ForumAuthSurfaceController>("Domain WhiteList: {0}", whitelist);
            //Logger.Info<ForumAuthSurfaceController>("Domain Blacklist: {0}", blacklist);

            if (emailAddress.Contains("@"))
            {
                var domain = emailAddress.Substring(emailAddress.IndexOf("@")).ToLower();
                //Logger.Info<ForumAuthSurfaceController>("Domain Check: {0}", domain);

                if (!string.IsNullOrWhiteSpace(whitelist) && !whitelist.Contains(domain))
                {
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(blacklist) && blacklist.Contains(domain))
                {
                    return false;
                }
            }

            return true;
        }

        public JsonResult CheckForValidEmail(string emailAddress)
        {
            if (IsValidEmailDomain(emailAddress))
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json("you cannot register for this site with that email address", JsonRequestBehavior.AllowGet);
            }
        }

        private string GetDictionaryOrDefault(string key, string defaultValue)
        {
            var dictionaryValue = Umbraco.GetDictionaryValue(key);
            if ( string.IsNullOrEmpty(dictionaryValue))
                return defaultValue;

            return dictionaryValue;
        }
    }
}