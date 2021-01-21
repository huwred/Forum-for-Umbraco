using System;
using System.Linq;
using System.Xml.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging.Serilog;
using Umbraco.Core.Models;
using Umbraco.Core.PackageActions;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Services;

namespace Forums
{
    public class ForumInstallHandler : IPackageAction
    {

        public bool Execute(string packageName, XElement xmlData)
        {
            
            try
            {
                Current.Logger.Info(System.Reflection.MethodBase.GetCurrentMethod().GetType(), "Executing ForumInstallHandler:Adding Forum admin groups");
                AddMemberGroups();
                Current.Logger.Info(System.Reflection.MethodBase.GetCurrentMethod().GetType(), "Executing ForumInstallHandler:Create Custom Member Properties");
                AddMemberProperties();

                Current.Logger.Info(System.Reflection.MethodBase.GetCurrentMethod().GetType(), "Executing ForumInstallHandler:Publish Forum Content");
                PublishContentPages();

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
            return "ForumInstallHandler";
        }

        public bool Undo(string packageName, XElement xmlData)
        {
            return true;
        }
        /// <summary>
        /// Add the custome properties to the Default Member Type
        /// receiveNotifications
        /// hasVerifiedAccount
        /// resetGuid
        /// joinedDate
        /// </summary>
        /// <returns></returns>
        private bool AddMemberProperties()
        {

            var _memberTypeService = Current.Services.MemberTypeService;
            var _dataTypeService = Current.Services.DataTypeService;
            string groupname = "Forum Settings";

            IMemberType memberContentType = _memberTypeService.Get(ForumAuthConstants.NewAccountMemberType);

            var dataTypeDefinitions = _dataTypeService.GetAll().ToArray(); //.ToArray() because arrays are fast and easy.

            var truefalse = dataTypeDefinitions.FirstOrDefault(p => p.EditorAlias.ToLower() == "umbraco.truefalse"); //we want the TrueFalse data type.
            var textbox = dataTypeDefinitions.FirstOrDefault(p => p.EditorAlias.ToLower() == "umbraco.textbox"); //we want the TextBox data type.

            try
            {            
                memberContentType.AddPropertyGroup(groupname); //add a property group, not needed, but I wanted it
            
                memberContentType.AddPropertyType(new PropertyType(truefalse)
                {
                    Name = "Receive Notifications",
                    Alias = "receiveNotifications",
                    Description = "Get an email when someone posts in a topic you are participating.",
                    Mandatory = false
                }, groupname); 
                memberContentType.AddPropertyType(new PropertyType(truefalse)
                {
                    Name = "Has verified Email",
                    Alias = "hasVerifiedAccount",
                    Description = "User has verified their account.",
                    Mandatory = false
                }, groupname); 
                
                memberContentType.AddPropertyType(new PropertyType(textbox)
                {
                    Name = "Reset Guid",
                    Alias = "resetGuid",
                    Description = "Guid set when user requests a password reset.",
                    Mandatory = false
                }, groupname); 
                memberContentType.AddPropertyType(new PropertyType(textbox)
                {
                    Name = "Joined date",
                    Alias = "joinedDate",
                    Description = "Date the user joined (validated email).",
                    Mandatory = false
                }, groupname);

                _memberTypeService.Save(memberContentType); //save the content type
                return true;
            }
            catch (Exception e)
            {
                Current.Logger.Error(System.Reflection.MethodBase.GetCurrentMethod().GetType(), e, "Executing ForumInstallHandler:Add member types");
                throw;
            }

        }

        private void PublishContentPages()
        {
            var _contentService = Current.Services.ContentService;
            long total = 0;
            
            var forumHome = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "forum");
            if (forumHome == null)
            {
                var home = _contentService.GetRootContent().FirstOrDefault();
                if (home != null)
                {
                    var test = _contentService.GetPagedChildren(home.Id, 0, 50, out total, null);
                    forumHome = test.FirstOrDefault(x => x.ContentType.Alias == "forum");
                }
            }
            if (forumHome != null)
            { _contentService.SaveAndPublishBranch(forumHome, true);}
            
        }


        private void AddMemberGroups()
        {
            var _memberService = Current.Services.MemberGroupService;
            IMemberGroup admingroup = new MemberGroup();
            admingroup.Name = "ForumAdministrator";
            IMemberGroup modgroup = new MemberGroup();
            modgroup.Name = "ForumModerator";
            _memberService.Save(admingroup);
            _memberService.Save(modgroup);

        }
    }
}