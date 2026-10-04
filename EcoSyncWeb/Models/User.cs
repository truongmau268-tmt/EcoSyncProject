namespace EcoSyncWeb.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; } // Cho phép để trống hoặc nhận số điện thoại
        public string Role { get; set; } = "Collaborator";

        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        // PB19: khóa/mở khóa tài khoản CTV vi phạm
        public bool IsBlocked { get; set; } = false;

        // PB06: hồ sơ năng lực của cộng tác viên
        public string? Skills { get; set; }
        public string? Experience { get; set; }
    }
}
