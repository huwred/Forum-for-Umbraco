using System;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Security;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

namespace MediaWizards.Forums
{
    public class SubscribeToContentServiceSavingComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // Append our component to the collection of Components
            // It will be the last one to be run
            composition.Components().Append<ForumPostSaveEvent>();
        }
    }

    public class ForumPostSaveEvent : IComponent
    {

        // initialize: runs once when Umbraco starts
        public void Initialize()
        {
            ContentService.Saving += ForumPost_Saving;
        }

        // terminate: runs once when Umbraco stops
        public void Terminate()
        {
            //unsubscribe during shutdown
            ContentService.Saving -= ForumPost_Saving;
        }

        private void ForumPost_Saving(IContentService sender, ContentSavingEventArgs e)
        {
            //if it is a forumPost then check if it is from the back office or UI and cancel if back office.
            foreach (var content in e.SavedEntities.Where(c => c.ContentType.Alias.InvariantEquals("forumPost")))
            {

                var postinguser = System.Web.HttpContext.Current.User.Identity;
                if (postinguser is UmbracoBackOfficeIdentity)
                {
                    if (content.IsPropertyDirty("postBody"))
                    {
                        e.Messages.Add(new EventMessage("Forum","Forum posts can not be edited in the back office", EventMessageType.Info));
                        e.Cancel = true;
                    }

                }

            }
        }
    }

}