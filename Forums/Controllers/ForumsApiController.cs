using System.Linq;
using System.Web.Http;
using Umbraco.Core.Models;
using Umbraco.Web.WebApi;
using Umbraco.Core.Services;

namespace Forums
{

    /// <summary>
    /// Summary description for SimplyForumsApiController
    /// </summary>
    public class ForumsApiController : UmbracoApiController
    {
        
        private IContentService _contentService;
        private IMemberTypeService _memberTypeService;

        public ForumsApiController(IContentService contentservice, IMemberTypeService memberservice)
        {
            _contentService = contentservice;
            _memberTypeService = memberservice;
        }

        /// <summary>
        /// used by the front end to delete posts via ajax.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
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
            }
            return false;
        }

        [HttpGet]
        public bool PublishedContentPages()
        {

            long total = 0;

            var forumHome = _contentService.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "forum");
            if (forumHome == null)
            {
                var home = _contentService.GetRootContent().FirstOrDefault();
                var test = _contentService.GetPagedChildren(home.Id, 0, 50, out total, null);
                forumHome = test.FirstOrDefault(x => x.ContentType.Alias == "forum");
            }

            if (forumHome != null)
            {
                return forumHome.Published;
            }

            return false;
        }

        [HttpGet]
        public bool MemberTypes()
        {

            IMemberType memberContentType = _memberTypeService.Get(ForumAuthConstants.NewAccountMemberType);
            var test = memberContentType.PropertyTypes.Where(p => p.Alias == "resetGuid");
            return test.Any();
        }
    }
}