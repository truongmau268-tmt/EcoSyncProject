namespace EcoSyncWeb.Models
{
    public class ProjectRegistration
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        // PB10/PB18: trạng thái xét duyệt hồ sơ ứng tuyển
        public string Status { get; set; } = "Chờ duyệt"; // Chờ duyệt | Đã duyệt | Đã loại

        // Thêm trường đánh dấu đã được cấp giấy chứng nhận
        public bool IsCertified { get; set; } = false;
        public bool IsEvaluated { get; set; } = false; // Đã làm khảo sát hay chưa

        // PB11: mã định danh duy nhất của giấy chứng nhận
        public string? CertificateCode { get; set; }
    }
}
