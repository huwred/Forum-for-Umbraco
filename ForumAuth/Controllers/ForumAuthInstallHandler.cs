using System;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.PackageActions;
using Language = Umbraco.Web.Models.ContentEditing.Language;

namespace Forums
{
    public class ForumAuthInstallHandler : IPackageAction
    {

        public bool Execute(string packageName, XElement xmlData)
        {
            
            try
            {
                Current.Logger.Info(System.Reflection.MethodBase.GetCurrentMethod().GetType(), "Executing ForumInstallHandler:Adding Dictionary Items");
                AddDictionaryItems();

                Current.Logger.Info(System.Reflection.MethodBase.GetCurrentMethod().GetType(), "Executing ForumInstallHandler:Adding Forum admin groups");
                AddMemberGroups();
                Current.Logger.Info(System.Reflection.MethodBase.GetCurrentMethod().GetType(), "Executing ForumInstallHandler:Create Custom Member Properties");
                AddMemberProperties();

                return true;
            }
            catch (Exception ex)
            {
                Current.Logger.Error(System.Reflection.MethodBase.GetCurrentMethod().GetType(), "INSTALL Package Error", ex);
                return false;
            }
        }

        public string Alias()
        {
            return "ForumAuthInstallHandler";
        }

        public bool Undo(string packageName, XElement xmlData)
        {
            return true;
        }

        public void AddDictionaryItems()
        {
            var ds = Current.Services.LocalizationService;
            var defLang = ds.GetDefaultLanguageIsoCode();
            ILanguage lang = ds.GetLanguageByIsoCode(defLang);

            if (!ds.DictionaryItemExists("ForumAuthConstants.ProfileView"))
            {
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.ForgotPasswordView"),lang, "Member/ForumAuth.ForgotPassword" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.LoginView"),lang,"Member/ForumAuth.Login" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.ResetPasswordView"),lang,"Member/ForumAuth.ResetPassword" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.RegisterView"),lang,"Member/ForumAuth.Register" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.ProfileView"),lang,"Member/ForumAuth.ViewProfile" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.ProfileEditView"),lang,"Member/ForumAuth.EditProfile" );

                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.LoginUrl"),lang,"/login" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.ResetUrl"),lang,"/reset" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.RegisterUrl"),lang,"/register" );
                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.VerifyUrl"),lang,"/verify" );

                ds.AddOrUpdateDictionaryValue(new DictionaryItem("ForumAuthConstants.NewAccountMemberType"),lang,"Member" );
                
            }


        }
        /// <summary>
        /// Add the custom properties to the Default Member Type
        /// receiveNotifications
        /// hasVerifiedAccount
        /// resetGuid
        /// joinedDate
        /// </summary>
        /// <returns></returns>
        private void AddMemberProperties()
        {
            bool saveMemberContent = false;
           
            var _memberTypeService = Current.Services.MemberTypeService;
            var _dataTypeService = Current.Services.DataTypeService;
            string groupname = "Forum Settings";
            string mType = "Member";
            var ds = Current.Services.LocalizationService;
            
            var newaccountType = ds.GetDictionaryItemByKey("ForumAuthConstants.NewAccountMemberType");
            if (newaccountType != null)
            {
                mType = newaccountType.GetDefaultValue();
            }
            IMemberType memberContentType = _memberTypeService.Get(mType);

            var dataTypeDefinitions = _dataTypeService.GetAll().ToArray(); //.ToArray() because arrays are fast and easy.

            var truefalse = dataTypeDefinitions.FirstOrDefault(p => p.EditorAlias.ToLower() == "umbraco.truefalse"); //we want the TrueFalse data type.
            var textbox = dataTypeDefinitions.FirstOrDefault(p => p.EditorAlias.ToLower() == "umbraco.textbox"); //we want the TextBox data type.

            try
            {
                if (!memberContentType.PropertyGroups.Contains(groupname))
                {
                    memberContentType.AddPropertyGroup(groupname); //add a property group, not needed, but I wanted it
                    saveMemberContent = true;
                }
                if(!memberContentType.PropertyTypeExists("receiveNotifications"))
                {
                    memberContentType.AddPropertyType(new PropertyType(truefalse)
                    {
                        Name = "Receive Notifications",
                        Alias = "receiveNotifications",
                        Description = "Get an email when someone posts in a topic you are participating.",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                } 
                if(!memberContentType.PropertyTypeExists("hasVerifiedAccount"))
                {
                    memberContentType.AddPropertyType(new PropertyType(truefalse)
                    {
                        Name = "Has verified Email",
                        Alias = "hasVerifiedAccount",
                        Description = "User has verified their account.",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                } 
                if(!memberContentType.PropertyTypeExists("resetGuid"))
                {
                    memberContentType.AddPropertyType(new PropertyType(textbox)
                    {
                        Name = "Reset Guid",
                        Alias = "resetGuid",
                        Description = "Guid set when user requests a password reset.",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                } 
                if(!memberContentType.PropertyTypeExists("joinedDate"))
                {
                    memberContentType.AddPropertyType(new PropertyType(textbox)
                    {
                        Name = "Joined date",
                        Alias = "joinedDate",
                        Description = "Date the user joined (validated email).",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                }

                if(saveMemberContent)
                    _memberTypeService.Save(memberContentType); //save the content type

            }
            catch (Exception e)
            {
                Current.Logger.Error(System.Reflection.MethodBase.GetCurrentMethod().GetType(), e, "Executing ForumInstallHandler:Add member types");
            }

        }

        private void AddMemberGroups()
        {
            var _memberService = Current.Services.MemberGroupService;

            if (_memberService.GetByName("ForumAdministrator") == null)
            {
                IMemberGroup admingroup = new MemberGroup();
                admingroup.Name = "ForumAdministrator";
                _memberService.Save(admingroup);
            }
            if (_memberService.GetByName("ForumModerator") == null)
            {
                IMemberGroup modgroup = new MemberGroup();
                modgroup.Name = "ForumModerator";
                _memberService.Save(modgroup);
            }            

        }
    }

}