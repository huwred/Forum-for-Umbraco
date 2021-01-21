using System;
using Umbraco.Core.Models;

namespace Forums
{
    public delegate void ForumEventHandler(IContent sender, ForumsEventArgs e);
    public delegate bool ForumPreEventHandler(ForumsPostModel sender, ForumsEventArgs e);

    public class ForumsEventArgs : EventArgs
    {
        public bool NewPost { get; set; }
        public bool Cancel { get; set; }
        public string Message { get; set; }
    }

    
}