using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Examine;
using Examine.Search;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Examine;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace MediaWizards.Forums.Controllers
{
    public class ForumSearchController : RenderMvcController
    {
        public ActionResult Index(ContentModel model, string query = "", string searchIn = "Subject", int page = 1)
        {
            ISearchResults results = null;

            var textFields = new List<string>();

            switch (searchIn)
            {
                case "Subject":
                    textFields.Add("postTitle");
                    break;
                case "Message":
                    textFields.Add("postBody");
                    break;
                case "Username":
                    textFields.Add("postCreator");
                    break;
            }

            int pageIndex = page - 1;
            int pageSize = 5;

            if (ExamineManager.Instance.TryGetIndex("ExternalIndex", out var index))
            {
                var searcher = index.GetSearcher();
                var value = "" + query + "*";

                results = searcher.CreateQuery("content")
                    .NodeTypeAlias("forumpost").And()
                    //.Field("postType","1").And()
                    .Field(textFields[0], value.MultipleCharacterWildcard())
                    //.GroupedOr(textFields, query)
                    .Execute(maxResults: pageSize*(pageIndex + 1));
                //.And().Field(textFields, new ExamineValue(Examineness.ComplexWildcard, "*" + query +"*"));
            }
            var pagedResults = results.Skip(pageIndex*pageSize).Take(pageSize);
            var totalResults = results.TotalItemCount;
            var pagedResultsAsContent = Umbraco.Content(pagedResults.Select(x => x.Id));

            SearchViewModel searchPageViewModel = new SearchViewModel(model.Content)
            {
                //do the search
                query = query,
                searchIn =  searchIn,
                TotalResults = totalResults,
                PagedResult = pagedResultsAsContent
            };
            //then return the custom model:
            return CurrentTemplate(searchPageViewModel);
        }
    }

    public class SearchViewModel : ContentModel
    {

        public string query { get; set; }
        public string searchIn { get; set; }
        public long TotalResults { get; set; }
        public IEnumerable<IPublishedContent> PagedResult { get; set; }

        public SearchViewModel(IPublishedContent content) : base(content)
        {
        }
    }
}