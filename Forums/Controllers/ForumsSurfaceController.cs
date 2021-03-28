using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Forums
{
    /// <summary>
    /// Summary description for ForumsSurfaceController
    /// </summary>
    public class ForumsSurfaceController : SurfaceController
    {
        public static event ForumPreEventHandler OnNewPost;
        public static event ForumEventHandler OnPostSaved;

        [HttpPost]
        public ActionResult PostReply([Bind(Prefix="Post")]ForumsPostModel model)
        {
            IEnumerable<ILanguage> languages = Services.LocalizationService.GetAllLanguages();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Reply", "Error posting (invalid model)");
                return CurrentUmbracoPage();
            }

            if (!CanPost(model))
            {
                ModelState.AddModelError("Reply", "You do not have permissions to post here");
                return CurrentUmbracoPage();
            }

            // fire the pre save event.
            // here you could put in things like spam protection. 
            // new PostEvent returns false if one ofthe delegated events sets cancel = true; 
            if (!NewPostEvent(model))
            {
                ModelState.AddModelError("Reply", "Error posting (blocked)");
                return CurrentUmbracoPage();
            }

            var posttype = model.IsTopic ? "topic" : "reply";

            var _contentService = Services.ContentService;

            var postName =
                $"reply_{DateTime.UtcNow:yyyyMMddhhmmss}";

            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                postName = model.Title;
            }


            var parent = _contentService.GetById(model.ParentId);
            bool newPost = false;
            if (parent != null)
            {
                IContent post = null;
                if (model.Id > 0)
                    post = _contentService.GetById(model.Id);

                if (post == null)
                {
                    post = _contentService.Create(postName, parent, "forumPost");

                    if (post.AvailableCultures.Any())
                    {
                        foreach (var language in languages)
                        {
                            post.SetCultureName(postName,language.IsoCode);
                        }

                    }

                    newPost = true;
                }

                // unlikely but possible we still don't have a node.
                if (post != null )
                {
                    post.SetValue("postTitle", model.Title);
                    post.SetValue("postBody", model.Body);

                    var author = Members.GetById(model.AuthorId);
                    if (author != null)
                    {
                        post.SetValue("postCreator", author.Name);
                        post.SetValue("postAuthor", author.Id);
                    }

                    if (parent.ContentType.Alias != "Forum")
                    {
                        // posts that are in a forum, are allowed replies 
                        // thats how the threads work.
                        post.SetValue("allowReplies", true);
                    }

                    post.SetValue("postType", model.IsTopic);
                    if (model.IsTopic)
                    {
                        post.SetValue("intPageSize",parent.GetValue<int>("intPageSize"));
                    }

                    if (!newPost)
                    {
                        post.SetValue("editDate",DateTime.UtcNow);
                    }
                    _contentService.SaveAndPublish(post);
                    
                    // notifications - handled by events
                    // you can write your own handler here,
                    // to be notified when any posts are made
                    ForumsEventArgs e = new ForumsEventArgs {NewPost = newPost};
                    PostSavedEvent(post, e);

                    return RedirectToCurrentUmbracoPage();
                }
            }
            ModelState.AddModelError("Post", "Error creating the post");
            return RedirectToCurrentUmbracoPage();
        }

        // double check the current user can post to this forum...
        private bool CanPost(ForumsPostModel model)
        {
            if (!Members.IsLoggedIn())
                return false;
            
            if ( model.ParentId > 0 ) 
            {
                var parent = Umbraco.Content(model.ParentId);
                if ( parent != null )
                {
                    var canPostGroups = parent.Value<string>("canPostGroups");

                    // default is any one logged on...
                    if (string.IsNullOrWhiteSpace(canPostGroups))
                        return true;

                    // is the user in any of those groups ?
                    var allowedGroupList = new List<string>();
                    foreach (var memberGroupStr in canPostGroups.Split(','))
                    {
                        var memberGroup = Services.MemberGroupService.GetById(Convert.ToInt32(memberGroupStr));
                        if (memberGroup != null)
                        {
                            allowedGroupList.Add(memberGroup.Name);
                        }
                    }
                    return Members.IsMemberAuthorized(allowGroups: allowedGroupList);
                }
            }

            return false;
        }

        /// <summary>
        ///  events - fired before and after a post is created
        /// </summary>
        protected void PostSavedEvent(IContent item, ForumsEventArgs e)
        {
            if (OnPostSaved != null)
                OnPostSaved(item, e);
        }

        protected bool NewPostEvent(ForumsPostModel model)
        {
            var e = new ForumsEventArgs();

            if (OnNewPost != null)
                OnNewPost(model, e);

            return !e.Cancel;
        }

    }
}