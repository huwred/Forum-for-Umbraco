using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web;
using System.Net.Mail;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Forums
{
    public class ForumNotificationMgr : IComponent 
    {
        private readonly ILogger _Logger;
        private readonly ILocalizationService _localisation;
        private readonly IUmbracoContextFactory  _context;
        private readonly IContentService _ContentService;

        public ForumNotificationMgr(
            ILogger logger,
            IContentService content,
            ILocalizationService  local,
            IUmbracoContextFactory context)
            {
                _Logger = logger;
                _ContentService = content;
                _localisation = local;
                _context = context;
            }

        public void Initialize()
        {
            ForumsSurfaceController.OnPostSaved += ForumsController_OnPostSaved;
        }

        public void Terminate()
        {
            ForumsSurfaceController.OnPostSaved -= ForumsController_OnPostSaved;
        }        
        
        private void ForumsController_OnPostSaved(IContent sender, ForumsEventArgs e)
        {
            // we get passed IContent - but as we are going to go get loads of 
            // other content, we will be quicker using IPublishedContent 
            using (var cref = _context.EnsureUmbracoContext())
            {
                var cache = cref.UmbracoContext.Content;

                var post = cache.GetById(sender.Id);
                if (post == null)
                    return;

                // for us - the current user will have been the author
                var author = post.Value("PostAuthor");

                // work out the root of this post (top of the thread)
                var postRoot = post;
                var parent = _ContentService.GetParent(sender);
                if (parent.ContentType.Alias== "forumPost")
                {
                    // if we have a parent post, then this is a reply 
                    postRoot = post.Parent;
                }
                _Logger.Info<ForumNotificationMgr>("Sending Notification for new post for {0}", postRoot.Name);

                List<string> receipients = GetRecipients(postRoot);

                // remove the author from the list.
                var postAuthor = GetAuthorEmail(post);
                if (receipients.Contains(postAuthor))
                    receipients.Remove(postAuthor);

                if (receipients.Any())
                {
                    SendNotificationEmail(postRoot, post, author, receipients, e.NewPost);
                }
            }


        }

        private List<string> GetRecipients(IPublishedContent item)
            {
                List<string> recipients = new List<string>();

                // get the orginal post author.
                var topicAuthorEmail = GetAuthorEmail(item);
                if (!string.IsNullOrWhiteSpace(topicAuthorEmail))
                {
                    recipients.Add(topicAuthorEmail);
                }

                foreach (var childPost in item.Children().Where(x => x.IsVisible()))
                {
                    var postAuthorEmail = GetAuthorEmail(childPost);
                    if (!string.IsNullOrWhiteSpace(postAuthorEmail) && !recipients.Contains(postAuthorEmail))
                    {
                        _Logger.Info<ForumNotificationMgr>("Adding: {0}", postAuthorEmail);
                        recipients.Add(postAuthorEmail);
                    }
                }
                return recipients;
            }

        private string GetAuthorEmail(IPublishedContent post)
        {
            if (post == null)
                return string.Empty;

            var author = post.Value<IPublishedContent>("PostAuthor");
            if (author.Id > 0 && author.Value<int?>("receiveNotifications") == 1)
            {
                
                return author.Value<string>("Email");
            }

            return string.Empty;
        }

        private void SendNotificationEmail(IPublishedContent root, IPublishedContent post, object author, List<string> recipients, bool newPost)
        {
            var threadTitle = root.Value<string>("postTitle");
            var updateBody = post.Value<HtmlString>("postBody");
            var fromAddress = Current.Configs.Settings().Content.NotificationEmailAddress;
            var authorName = author != null ? ((IPublishedContent)author).Name : "Someone";


            string siteUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            string postUrl = $"{siteUrl}{root.Url()}";

            // build the subject and body up
            var subjectTemplate = "{{newOld}} comment on [{{postTitle}}]";
            var bodyTemplate = "<p>{{author}} has posted a comment on {{postTitle}}</p>" +
                "<div style=\"border-left: 4px solid #444;padding:0.5em;font-size:1.3em\">{{body}}</div>" +
                "<p>you can view all the comments here: <a href=\"{{threadUrl}}\">{{threadUrl}}</a>";


            MailMessage message = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = GetEmailTemplate(subjectTemplate, "Forums.NotificationSubject",
                    threadTitle, updateBody.ToString(), authorName, postUrl, newPost),

                Body = GetEmailTemplate(bodyTemplate, "Forums.NotificationBody",
                    threadTitle, updateBody.ToString(), authorName, postUrl, newPost),
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                if (!string.IsNullOrWhiteSpace(recipient))
                    message.Bcc.Add(recipient);
            }

            try
            {
                //_Logger.Info<ForumNotificationMgr>("Sending Email {0} to {1} people", threadTitle, recipients.Count);
                // smtp (assuming you've set all this up)
                using ( SmtpClient smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }
                
            }
            catch (Exception ex)
            {
                _Logger.Error<ForumNotificationMgr>("Failed to send the email - probably because email isn't configured for this site\n {0}", ex.ToString());
            }
        }

        private string GetEmailTemplate(string template, string dictionaryString, string postTitle, string body, string author, string threadUrl, bool newPost)
        {
            using (var cref = _context.EnsureUmbracoContext())
            {
                var dictionaryTemplate = _localisation.GetDictionaryItemByKey(dictionaryString);
                if (dictionaryTemplate != null && !string.IsNullOrWhiteSpace(dictionaryTemplate.GetDefaultValue()))
                {
                    template = dictionaryTemplate.GetDefaultValue();
                }
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                {"{{author}}", author},
                {"{{postTitle}}", postTitle},
                {"{{body}}", body},
                {"{{threadUrl}}", threadUrl},
                {"{{newOld}}", newPost ? "New" : "Updated"}
            };

            return template.ReplaceMany(parameters); ;
        }


    }    

}
