using System;

namespace Forums
{
    public class ForumCacheItem : IComparable
    {
        public int Count { get; set; }
        public string lastpostAuthor { get; set; }
        public DateTime latestPost { get; set; }
        public DateTime latestEdit { get; set; }
        public int CompareTo(object obj)
        {
            return latestPost.CompareTo(((ForumCacheItem)obj).latestPost);
        }
    }
}