using System.ComponentModel.DataAnnotations;

namespace EcoSyncWeb.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }
        
        public int ProjectId { get; set; } // Liên kết với chiến dịch
        
        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        public string Content { get; set; } // Nội dung phản hồi của TNV/Người dân
        
        public string Sentiment { get; set; } // Kết quả AI phân tích: "Tích cực", "Tiêu cực", "Trung tính"
        
        public int Rating { get; set; } // Số sao (1 đến 5)
        
        public string AuthorName { get; set; } // Người gửi

        public string? AuthorEmail { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}