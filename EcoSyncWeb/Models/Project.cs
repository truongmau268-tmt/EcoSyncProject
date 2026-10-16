using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace EcoSyncWeb.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } // Tên chiến dịch

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        
        public string Description { get; set; } // Mô tả ngắn gọn
        
        [Required]
        public DateTime StartTime { get; set; } // Thời gian bắt đầu (Ngày + Giờ)

        [Required]
        public DateTime EndTime { get; set; } // Thời gian kết thúc (Ngày + Giờ)

        // Giữ lại StartDate để tương thích ngược với các đoạn code cũ nếu cần
        public DateTime StartDate => StartTime.Date;
        
        public string Location { get; set; } // Địa điểm

        public int? TargetVolunteers { get; set; } // Chỉ tiêu số lượng tình nguyện viên (PB07)

        public string Status { get; set; } = "Sắp diễn ra"; // Trạng thái: Sắp diễn ra, Đang diễn ra, Đã kết thúc
    }
}
