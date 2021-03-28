﻿ using System;
  using System.Collections.Generic;
  using System.Linq;
 using Umbraco.Web;
 using Examine;
 using System.Diagnostics;
 using System.Text;
 using System.Globalization;
 using Examine.Search;
 using Umbraco.Core;
 using Umbraco.Core.Cache;
  using Umbraco.Core.Composing;
  using Umbraco.Core.Composing.CompositionExtensions;
  using Umbraco.Core.Events;
  using Umbraco.Core.Logging;
  using Umbraco.Core.Models;
  using Umbraco.Core.Models.PublishedContent;
  using Umbraco.Core.Services;
  using Umbraco.Core.Services.Implement;
  using Umbraco.Examine;

  namespace Forums 
  {
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class ForumComponentComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<ForumCacheHandler>();
            composition.Components().Append<ForumNotificationMgr>();
        }
    }
    /// <summary>
    /// Caches things like last post date, and post count, so the site doesn't
    /// have to work it out all the time...
    /// 
    /// we attache to the publish / unpublish events, so we can clear the cache
    /// when a new post arrives or someone does something with the back end.
    /// </summary>
    public class ForumCacheHandler : IComponent
    {
        private int postContentTypeId;
        private int forumContentTypeId;
        private readonly IContentService _ContentService;
        private readonly IContentTypeService  _ContentTypeService;

        public ForumCacheHandler(
            IContentService content,
            IContentTypeService  contentype)
        {
            _ContentService = content;
            _ContentTypeService = contentype;
        }

        private List<string> AddParentForumCaches(IContent item, List<string> cacheList)
        {
            var parent = _ContentService.GetParent(item);

            if (parent != null)
            {
                if (parent.ContentTypeId == forumContentTypeId || parent.ContentTypeId == postContentTypeId)
                {
                    var cache = $"forum_{parent.Id}";
                    if (!cacheList.Contains(cache))
                        cacheList.Add(cache);

                    cacheList = AddParentForumCaches(parent, cacheList);
                }
            }

            return cacheList;
        }

        public void Initialize()
        {
            var postType = _ContentTypeService.Get("forumPost");
            if (postType != null)
                postContentTypeId = postType.Id;

            var forumType = _ContentTypeService.Get("forum");
            if (forumType != null)
                forumContentTypeId = forumType.Id;
            ContentService.Published += this.ContentServicePublished;
            ContentService.Unpublished += this.ContentServiceUnPublished;
        }

        public void Terminate()
        {
            ContentService.Published -= this.ContentServicePublished;
            ContentService.Unpublished -= this.ContentServiceUnPublished;
        }

        private void ContentServiceUnPublished(IContentService sender, PublishEventArgs<IContent> e)
        {
            // when something is unpublished, (if it's a ForumPost)
            // clear the relevant forum cache.
            // we do it in two steps because more than one post in a forum 
            // may have been published, so we only need to clear the cache
            // once.

            List<string> invalidCacheList = new List<string>();

            foreach (var item in e.PublishedEntities)
            {
                // is a forum post...
                if (item.ContentTypeId == postContentTypeId)
                {
                    // get parent Forum.
                    invalidCacheList = AddParentForumCaches(item, invalidCacheList);
                }
            }

            // clear the cache for any forums that have had child pages published...
            foreach (var cache in invalidCacheList)
            {
                //Logger.Info<ForumCacheHandler>("Clearing Forum Info Cache: {0}",  cache);
                Umbraco.Core.Composing.Current.AppCaches.RuntimeCache.ClearByKey(cache);
            }
        }

        private void ContentServicePublished(IContentService sender, ContentPublishedEventArgs e)
        {
            // when something is published, (if it's a ForumPost)
            // clear the relevant forum cache.
            // we do it in two steps because more than one post in a forum 
            // may have been published, so we only need to clear the cache
            // once.

            List<string> invalidCacheList = new List<string>();

            foreach (var item in e.PublishedEntities)
            {
                // is a forum post...
                if (item.ContentTypeId == postContentTypeId)
                {
                    // get parent Forum.
                    invalidCacheList = AddParentForumCaches(item, invalidCacheList);
                }
            }

            // clear the cache for any forums that have had child pages published...
            foreach (var cache in invalidCacheList)
            {
                //Logger.Info<ForumCacheHandler>("Clearing Forum Info Cache: {0}", cache);
                Umbraco.Core.Composing.Current.AppCaches.RuntimeCache.ClearByKey(cache);
            }
        }
    }

    public static class ForumCache 
    {
        /// <summary>
        ///  Get the Forum Info - using examine, because that can be faster when 
        ///  there are lots and lots of posts - although i've yet to see it 
        ///  really get faster than the traverse method (yet)
        /// </summary>
        /// <param name="item"></param>
        /// <param name="useExamine">true to use examine</param>
        public static ForumCacheItem GetForumInfo(this IPublishedContent item, bool useExamine)
        {
            if (!useExamine)
                return GetForumInfo(item);

            ExamineManager.Instance.TryGetSearcher("InternalSearcher", out _);

            var cacheName = $"forum_{item.Id}";
            var cache = Current.AppCaches.RuntimeCache;
            var forumInfo = cache.GetCacheItem<ForumCacheItem>(cacheName);

            if (forumInfo != null)
                return forumInfo;

            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            forumInfo = new ForumCacheItem();

            ISearchResults searchResults = queryLastPostIndex(item, item.Name);
            ISearchResults updateResults = queryLastEditIndex(item, item.Name);

            forumInfo.Count = searchResults.ToList().Count-1;
            forumInfo.latestPost = DateTime.MinValue;

            if (searchResults.Any())
            {
                var lastpostdate = new DateTime(Convert.ToInt64(searchResults.First().Values["createDate"]));
                if (lastpostdate > DateTime.MinValue)
                    forumInfo.latestPost = lastpostdate;
                forumInfo.lastpostAuthor = searchResults.First().Values["postCreator"];
            }
            if (updateResults.Any())
            {
                var latestedit = new DateTime(Convert.ToInt64(searchResults.First().Values["editDate"]));
                if (latestedit > DateTime.MinValue)
                    forumInfo.latestEdit = latestedit;
            }

            cache.InsertCacheItem<ForumCacheItem>(cacheName, () => forumInfo);

            return forumInfo;
        }
        private static ISearchResults queryLastPostIndex(IPublishedContent item, string searchTerm)
        {
            if (!ExamineManager.Instance.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out var index))
            {
                throw new InvalidOperationException($"No index found with name {Constants.UmbracoIndexes.ExternalIndexName}");
            }

            return index.GetSearcher().CreateQuery()
                .Field("path", item.Path.ToString().MultipleCharacterWildcard())
                .OrderByDescending(new SortableField("createDate", SortType.Long)).Execute();

        }
        private static ISearchResults queryLastEditIndex(IPublishedContent item, string searchTerm)
        {
            if (!ExamineManager.Instance.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out var index))
            {
                throw new InvalidOperationException($"No index found with name {Constants.UmbracoIndexes.ExternalIndexName}");
            }

            return index.GetSearcher().CreateQuery()
                .Field("path", item.Path.ToString().MultipleCharacterWildcard())
                .OrderByDescending(new SortableField("editDate", SortType.Long)).Execute();

        }
        public static ForumCacheItem GetForumInfo(this IPublishedContent item)
        {
            var cacheName = $"forum_{item.Id}";
            var cache = Current.AppCaches.RuntimeCache;
            var forumInfo = cache.GetCacheItem<ForumCacheItem>(cacheName);

            if (forumInfo != null)
                return forumInfo;

            // not in the cache, we have to make it.
            forumInfo = new ForumCacheItem();

            var posts = item.Descendants().Where(x => x.IsVisible() && x.IsDocumentType("forumPost")).ToList();

            forumInfo.Count = posts.Count();
            if (posts.Any())
            {
                var lastPost = posts.OrderByDescending(x => x.CreateDate).FirstOrDefault();
                if (lastPost != null) forumInfo.latestPost = lastPost.CreateDate;
                forumInfo.lastpostAuthor = lastPost.Value<string>(("postCreator"));

                var lastedit = posts.OrderByDescending(x => x.Value<DateTime>("editDate")).FirstOrDefault();
                if (lastedit != null) forumInfo.latestEdit = lastedit.Value<DateTime>("editDate");
            }

            cache.GetCacheItem<ForumCacheItem>(cacheName,  () => forumInfo);

            return forumInfo;
        }
    }
  }