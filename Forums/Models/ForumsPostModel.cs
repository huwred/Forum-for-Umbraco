using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Forums
{
    public class ForumsPostModel
    {
        public int Id { get; set; }
        public int ParentId { get; set; }

        [DisplayName("Title")]
        public string Title { get; set; }

        [Required]
        [AllowHtml]
        [DisplayName("Reply")]
        public string Body { get; set; }

        [Required]
        public int AuthorId { get; set; }

        public bool IsTopic { get; set; }
    }
}