using System.ComponentModel.DataAnnotations;

namespace EcoSyncWeb.Models
{
    /// <summary>
    /// Tin tức & bài viết truyền thông môi trường (PB20 - news).
    /// </summary>
    public class News
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
