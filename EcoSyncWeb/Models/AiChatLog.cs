namespace EcoSyncWeb.Models
{
    /// <summary>
    /// Lưu toàn bộ hội thoại giữa người dùng và Trợ lý AI (PB14 - ai_chat_logs).
    /// </summary>
    public class AiChatLog
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Mode { get; set; } = "advice"; // advice = tư vấn | interview = phỏng vấn sơ loại
        public string Message { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public string Source { get; set; } = "Groq"; // Groq | Fallback
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
