using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.WebApi;
using Umbraco.Core.Services;
using Umbraco.Web;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.IO;

namespace Forums
{
    public class upload
    {
        public string location;
    }

    /// <summary>
    /// Summary description for SimplyForumsApiController
    /// </summary>
    public class ForumsApiController : UmbracoApiController
    {
        
        private IContentService _contentService;
        private IMemberTypeService _memberTypeService;
        private IMemberGroupService _memberGroupService;
        private IDataTypeService _dataTypeService;
        private ILocalizationService _localizationService;
        private IContentTypeService _contenttypeservice;
        private readonly IMediaFileSystem _mediaFileSystem;

        public ForumsApiController(IContentService contentservice,IContentTypeService contenttypeservice, IMemberTypeService memberservice,IMemberGroupService memberGroupService, IDataTypeService dataTypeService, ILocalizationService localizationService,IMediaFileSystem mediaFileSystem)
        {
            _contentService = contentservice;
            _memberTypeService = memberservice;
            _memberGroupService = memberGroupService;
            _dataTypeService = dataTypeService;
            _localizationService = localizationService;
            _contenttypeservice = contenttypeservice;
            _mediaFileSystem = mediaFileSystem;
        }

        /// <summary>
        /// used by the front end to delete posts via ajax.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [System.Web.Http.HttpGet]
        public bool DeletePost(int id)
        {
            var post = _contentService.GetById(id);

            if (post != null)
            {
                var author = post.GetValue<int>("postAuthor");

                if (author > 0 && author == Members.GetCurrentMemberId())
                {
                    //Logger.Info<ForumsApiController>("Deleting post {0}", id);
                    if ( post.HasProperty("umbracoNaviHide")) 
                        post.SetValue("umbracoNaviHide", true);

                    if ( post.HasProperty("deletedByAuthor"))
                        post.SetValue("deletedByAuthor", true);

                    _contentService.SaveAndPublish(post);
                    //Logger.Info<ForumsApiController>("Deleting post {0}", id);
                    return true;
                }
                else if(Members.GetCurrentUserRoles().Contains("ForumAdministrator") || Members.GetCurrentUserRoles().Contains("ForumModerator"))
                {
                    //Logger.Info<ForumsApiController>("Deleting post {0}", id);
                    if ( post.HasProperty("umbracoNaviHide")) 
                        post.SetValue("umbracoNaviHide", true);

                    if ( post.HasProperty("deletedByAuthor"))
                        post.SetValue("deletedByAuthor", false);

                    _contentService.SaveAndPublish(post);
                    //Logger.Info<ForumsApiController>("Deleting post {0}", id);
                    return true;
                }
            }
            return false;
        }

        [System.Web.Http.HttpGet]
        public bool PublishedContentPages()
        {
            var test = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("forum").Any() && Umbraco.ContentAtRoot().DescendantsOrSelfOfType("forumSearch").Any();
            return test;
        }

        [System.Web.Http.HttpGet]
        public bool PublishedMembershipPages()
        {
            var test = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("forumAuthHolder");
            return test.Any();
        }
        [System.Web.Http.HttpGet]
        public bool MembershipInstalled()
        {
            return _contenttypeservice.GetAllContentTypeIds(new string[] {"forumAuthHolder"}).Any();
        }
        [System.Web.Http.HttpGet]
        public string MemberTypes()
        {
            AddDictionaryItems();

            var newaccountType = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.NewAccountMemberType");
            IMemberType memberContentType = _memberTypeService.Get(newaccountType.GetDefaultValue());
            var test = memberContentType.PropertyTypes.Where(p => p.Alias == MediaWizards.Forums.Models.ForumAuthConstants.ResetRequestGuidPropery);
            if (test.Any())
            {
                return "Properties already added.";
            }
            AddMemberGroups();

            return AddMemberProperties();
        }

        public IEnumerable<KeyValuePair<int,string>> GetAllContentPages()
        {
            var pages = new List<KeyValuePair<int, string>>();
            
            var home = _contentService.GetRootContent().FirstOrDefault();
            pages.Add(new KeyValuePair<int, string>(home.Id,home.Name));
            var allFolders = Umbraco.ContentAtRoot().FirstOrDefault().Children<IPublishedContent>().Where(x => x.IsVisible());
            pages.AddRange(allFolders.Select(x=>new KeyValuePair<int,string>(x.Id, x.Name) ));
            return pages;
        }

        [System.Web.Http.HttpGet]
        public string CreateMembershipContent(int rootId)
        {
            StringBuilder contentStatus = new StringBuilder();

            if (Umbraco.ContentAtRoot().DescendantsOrSelfOfType("forumAuthHolder").Any())
            {
                contentStatus.Append("<ul>");
                contentStatus.Append("<li>Content already exists</li>");
                contentStatus.Append("</ul>");
                return contentStatus.ToString();
            }
            if (rootId == -1)
            {
                var home = _contentService.GetRootContent().FirstOrDefault();
                rootId = home.Id;
            }

            var rootNode =  _contentService.GetById(rootId);
            if (rootNode == null)
            {
                contentStatus.Append("<p>Can't find that node </p>");
                return contentStatus.ToString();
            }

            contentStatus.Append("<ul>");
            contentStatus.Append("<li>Creating Membership holder: ");
            if (CreateContentNode("Members", "forumAuthHolder", rootNode, "Membership Holder page, does not do anything.", "empty"))
            {
                var content = Umbraco.Content(rootNode.Id);
                contentStatus.Append("<strong>done</strong>");
                if (content.Children != null)
                {
                    IPublishedContent first = null;
                    foreach (var c in content.Children)
                    {
                        if (c.Name == "Members")
                        {
                            first = c;
                            break;
                        }
                    }

                    if (first != null)
                    {
                        rootNode = _contentService.GetById(first.Id);
                        rootNode.SetValue("umbracoNaviHide",true);
                        _contentService.SaveAndPublish(rootNode);
                    }
                }
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Login page: ");
            if (CreateContentNode("Login", "login", rootNode, "Login", "Don't have an account? You can register for one <a href=\"/register\" title=\"Register\">here</a>","login"))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Logout page: ");
            if (CreateContentNode("Logout", "logout", rootNode, "Logout", "See you next time."))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Register page: ");
            if (CreateContentNode("Register", "register", rootNode, "Register", 
                $@"<p>Registration is not required to view current topics on the Forum; however, if you wish to post a new topic or reply to an existing topic registration is required.
                    Registration is free and only takes a few minutes. The only required fields are your Username, which may be your real name or a nickname, and a valid e-mail address.
                    </p><p>
                    The information you provide during registration is not outsourced or used for any advertising by us.
                    If you believe someone is sending you advertisements as a result of the information you provided through your registration, please notify us immediately.</p>","register"))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Reset page: ");
            if (CreateContentNode("Reset", "reset", rootNode, "Reset your password", "Please provide the email you used when registering and we will send you a link to complete the password reset request.","reset"))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Verify page: ");
            if (CreateContentNode("Verify", "verify", rootNode, "Verify Email", "Thank you for registering, you may now <a href=\"/login\" title=\"Login\">login</a> and participate in the forum discussions.","varify"))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("<li>Creating Profile page: ");
            if (CreateContentNode("Profile", "profile", rootNode, "Member Profile", "If you change your email address an email will be sent to the new address to validate it is real, your account will be locked until you use the verification link.","profile"))
            {
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");
            contentStatus.Append("</li></ul>");

            contentStatus.Append("<p><strong>Basic authentication pages created</strong></p>");

            return contentStatus.ToString();
        }
        [System.Web.Http.HttpGet]
        public string CreateForumContent(int rootId)
        {

            StringBuilder contentStatus = new StringBuilder();


            if (rootId == -1)
            {
                var home = _contentService.GetRootContent().FirstOrDefault();
                rootId = home.Id;
            }

            var rootNode =  _contentService.GetById(rootId);
            if (rootNode == null)
            {
                contentStatus.Append("<p>Can't find that node </p>");
                return contentStatus.ToString();
            }

            var test = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("forum").ToList();

            if (test.Any())
            {
                contentStatus.Append("<ul>");                
                if (Umbraco.ContentAtRoot().DescendantsOrSelfOfType("forumSearch") == null)
                {
                    rootNode = _contentService.GetById(test.FirstOrDefault().Id);
                    contentStatus.Append("<li>Creating Search holder: ");
                    var srchnode = _contentService.Create("ForumSearch", rootNode, "forumSearch");
                    if (srchnode != null)
                    {
                        srchnode.SetValue("intPageSize", 5);
                        _contentService.SaveAndPublish(srchnode);
                        contentStatus.Append("<strong>done</strong>");
                    }
                    contentStatus.Append("</li>");
                }

                contentStatus.Append("<li>Forum Content already exists</li>");
                contentStatus.Append("</ul>");
                return contentStatus.ToString();
            }
            contentStatus.Append("<ul>");

            contentStatus.Append("<li>Creating Forums Node: ");
            var rootnode = _contentService.Create("Forums", rootNode, "forum");
            if (rootnode != null)
            {
                rootnode.SetValue("forumName", "Media Wizard Forums for Umbraco");
                rootnode.SetValue("forumIntro", 
                    @"<p>Thank you for installing the MediaWizards forum package. We hope you enjoy this great tool to support your organization!</p>
                    <p>Many thanks go out to <a href=''>Kevin Jump</a> for the original source code and to all the members of Our Umbraco at <a href=''>http://our.umbraco.com/forum</a> for helping to test.</p>");
                rootnode.SetValue("forumActive", true);
                rootnode.SetValue("hideChildrenFromNav", true);
                rootnode.SetValue("intPageSize", 10);
                _contentService.SaveAndPublish(rootnode);
                contentStatus.Append("<strong>done</strong>");
                rootNode = rootnode;
            }
            contentStatus.Append("</li>");
            contentStatus.Append("<li>Creating Default Forum: ");
            var testnode = _contentService.Create("Test Forum", rootNode, "forum");
            if (testnode != null)
            {
                testnode.SetValue("forumName", "Test Forum");
                testnode.SetValue("forumIntro", "This forum gives you a chance to become more familiar with how this product responds to different features and keeps testing in one place instead of posting tests all over. Happy Posting!");
                testnode.SetValue("forumActive", true);
                testnode.SetValue("postAtRoot", true);
                testnode.SetValue("intPageSize", 10);

                _contentService.SaveAndPublish(testnode);
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Private Forum: ");
            var privnode = _contentService.Create("Private Forum", rootNode, "forum");
            if (privnode != null)
            {
                privnode.SetValue("forumName", "ForumSearch");
                privnode.SetValue("forumIntro", "This is a private forum, it can only be seen by certain Member Groups, defined under 'Can View groups'");
                privnode.SetValue("forumActive", true);
                privnode.SetValue("postAtRoot", true);
                privnode.SetValue("intPageSize", 20);
                privnode.SetValue("membersOnly", true);
                var moderator = _memberGroupService.GetByName("ForumModerator");
                var admin = _memberGroupService.GetByName("ForumAdministrator");

                privnode.SetValue("canViewGroups", $"{moderator.Id.ToString()},{admin.Id.ToString()}");
                privnode.SetValue("canPostGroups", moderator.Id.ToString());
                _contentService.SaveAndPublish(privnode);
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");

            contentStatus.Append("<li>Creating Search holder: ");
            var node = _contentService.Create("ForumSearch", rootNode, "forumSearch");
            if (node != null)
            {
                node.SetValue("intPageSize", 5);
                _contentService.SaveAndPublish(node);
                contentStatus.Append("<strong>done</strong>");
            }
            contentStatus.Append("</li>");
            contentStatus.Append("</ul>");

            contentStatus.Append("<p><strong>Basic Forum pages created</strong></p>");
            string data = "{\"root\": \"" + rootNode.Id + "\", \"content\": \""+ contentStatus.ToString() + "\"}";
            return data;
        }

        /// <summary>
        /// File upload handler for TinyMCE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public upload TinyMceUpload()
        {
            string path = "/forumuploads";
            var file = HttpContext.Current.Request.Files["file"];

            var uploadpath = _localizationService.GetDictionaryItemByKey("ForumSettings.UploadPath");
            if (uploadpath != null)
            {
                path = uploadpath.GetDefaultValue();
            }

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            return new upload() {location = SaveFile(path,file)};;
        }

        /// <summary>
        /// Saves the contents of an uploaded image file.
        /// </summary>
        /// <param name="targetFolder">Location where to save the image file.</param>
        /// <param name="file">The uploaded image file.</param>
        /// <exception cref="InvalidOperationException">Invalid MIME content type.</exception>
        /// <exception cref="InvalidOperationException">Invalid file extension.</exception>
        /// <exception cref="InvalidOperationException">File size limit exceeded.</exception>
        /// <returns>The relative path where the file is stored.</returns>
        private static string SaveFile(string targetFolder, HttpPostedFile file)
        {
            const int megabyte = 1024 * 1024;
            string[] extensions = { ".gif", ".jpg", ".png", ".svg", ".webp" };
            var fs = new PhysicalFileSystem("~" + targetFolder);

            if (!fs.DirectoryExists(fs.GetFullPath("")))
            {
                Directory.CreateDirectory(fs.GetFullPath(""));
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                throw new InvalidOperationException("Invalid MIME content type.");
            }

            var extension = Path.GetExtension(file.FileName.ToLowerInvariant());
            if (!extensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid file extension.");
            }
            
            if (file.ContentLength > (8 * megabyte))
            {
                throw new InvalidOperationException("File size limit exceeded.");
            }

            var fileName = Guid.NewGuid() + extension;
            var path = Path.Combine(fs.GetFullPath(""), fileName);
            file.SaveAs(path);

            return Path.Combine(targetFolder, fileName).Replace('\\', '/');
        }

        private bool CreateContentNode(string name, string contentType, IContent parent, string title, string body , string UrlAlias = "")
        {
            var content = Umbraco.Content(parent.Id); 
            var exsitingNode = content.Children(x=>x.Name == name);
            if (exsitingNode.Any())
                return false;

            var node = _contentService.Create(name, parent, contentType);
            if (node != null)
            {
                node.SetValue("title", title);
                node.SetValue("bodyText", body);
                if (!String.IsNullOrWhiteSpace(UrlAlias) && node.HasProperty("umbracoUrlAlias"))
                {
                    node.SetValue("umbracoUrlAlias", UrlAlias);
                }
                
                _contentService.SaveAndPublish(node);

                return true;
            }
            return false;
        }
        private bool CreateRootNode(string name, string contentType, IContent parent, string title, string body )
        {
            var content = Umbraco.Content(parent.Id); 
            var exsitingNode = content.Children(x=>x.Name == name);
            if (exsitingNode.Any())
                return false;

            var node = _contentService.Create(name, parent, contentType);
            if (node != null)
            {
                node.SetValue("title", title);
                node.SetValue("bodyText", body);
                _contentService.SaveAndPublish(node);

                return true;
            }
            return false;
        }

        private string AddMemberProperties()
        {
            bool saveMemberContent = false;
            StringBuilder contentStatus = new StringBuilder();

            string groupname = "Forum Settings";
            string mType = "Member";
            contentStatus.Append("<ul>");

            var newaccountType = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.NewAccountMemberType");
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
                contentStatus.Append("<li>");
                
                if(!memberContentType.PropertyTypeExists("receiveNotifications"))
                {
                    contentStatus.Append("Adding property receiveNotifications");
                    memberContentType.AddPropertyType(new PropertyType(truefalse)
                    {
                        Name = "Receive Notifications",
                        Alias = "receiveNotifications",
                        Description = "Get an email when someone posts in a topic you are participating.",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                    contentStatus.Append("<strong>done</strong>");
                } 
                contentStatus.Append("</li><li>");
                
                if(!memberContentType.PropertyTypeExists("hasVerifiedAccount"))
                {
                    contentStatus.Append("Adding property hasVerifiedAccount");
                    memberContentType.AddPropertyType(new PropertyType(truefalse)
                    {
                        Name = "Has verified Email",
                        Alias = "hasVerifiedAccount",
                        Description = "User has verified their account.",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                    contentStatus.Append("<strong>done</strong>");
                } 
                contentStatus.Append("</li><li>");
                
                if(!memberContentType.PropertyTypeExists("resetGuid"))
                {
                    contentStatus.Append("Adding property resetGuid");
                    memberContentType.AddPropertyType(new PropertyType(textbox)
                    {
                        Name = "Reset Guid",
                        Alias = "resetGuid",
                        Description = "Guid set when user requests a password reset.",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                    contentStatus.Append("<strong>done</strong>");
                } 
                contentStatus.Append("</li><li>");
                
                if(!memberContentType.PropertyTypeExists("joinedDate"))
                {
                    contentStatus.Append("Adding property joinedDate");
                    memberContentType.AddPropertyType(new PropertyType(textbox)
                    {
                        Name = "Joined date",
                        Alias = "joinedDate",
                        Description = "Date the user joined (validated email).",
                        Mandatory = false
                    }, groupname);
                    saveMemberContent = true;
                    contentStatus.Append("<strong>done</strong>");
                }
                contentStatus.Append("</li>");
                if(saveMemberContent)
                    _memberTypeService.Save(memberContentType); //save the content type

            }
            catch (Exception e)
            {
                Current.Logger.Error(System.Reflection.MethodBase.GetCurrentMethod().GetType(), e, "Executing ForumInstallHandler:Add member types");
                contentStatus.Append($"<li>Error adding properties: {e.Message}</li>");
            }
            contentStatus.Append("</ul>");

            return contentStatus.ToString();
        }

        private void AddDictionaryItems()
        {
            try
            {
                var defLang = _localizationService.GetDefaultLanguageId();
                ILanguage lang = _localizationService.GetLanguageById(defLang.Value);
                Current.Logger.Debug(System.Reflection.MethodBase.GetCurrentMethod().GetType(),"Executing AddDictionaryItems" + defLang);
                var newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.NewAccountMemberType") ??
                              new DictionaryItem("ForumAuthConstants.NewAccountMemberType");

                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"Member" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.ForgotPasswordView") ?? new DictionaryItem("ForumAuthConstants.ForgotPasswordView");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang, "Member/ForumAuth.ForgotPassword" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.LoginView") ?? new DictionaryItem("ForumAuthConstants.LoginView");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"Member/ForumAuth.Login" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.ResetPasswordView") ?? new DictionaryItem("ForumAuthConstants.ResetPasswordView");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"Member/ForumAuth.ResetPassword" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.RegisterView") ?? new DictionaryItem("ForumAuthConstants.RegisterView");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"Member/ForumAuth.Register" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.ProfileView") ?? new DictionaryItem("ForumAuthConstants.ProfileView");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"Member/ForumAuth.ViewProfile" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.ProfileEditView") ?? new DictionaryItem("ForumAuthConstants.ProfileEditView");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"Member/ForumAuth.EditProfile" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.LoginUrl") ?? new DictionaryItem("ForumAuthConstants.LoginUrl");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"/login" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.ResetUrl") ?? new DictionaryItem("ForumAuthConstants.ResetUrl");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"/reset" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.RegisterUrl") ?? new DictionaryItem("ForumAuthConstants.RegisterUrl");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"/register" );
                _localizationService.Save(newitem);
                newitem = _localizationService.GetDictionaryItemByKey("ForumAuthConstants.VerifyUrl") ?? new DictionaryItem("ForumAuthConstants.VerifyUrl");
                _localizationService.AddOrUpdateDictionaryValue(newitem,lang,"/verify" );
                _localizationService.Save(newitem);
                _localizationService.Save(lang);
            }
            catch (Exception e)
            {
                Current.Logger.Error(System.Reflection.MethodBase.GetCurrentMethod().GetType(), e, "Executing AddDictionaryItems");

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