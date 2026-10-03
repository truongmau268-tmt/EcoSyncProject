namespace EcoSyncWeb.Models
{
    /// <summary>
    /// Bản tóm tắt đánh giá năng lực ứng viên do AI trích xuất (PB15 - ai_summaries).
    /// </summary>
    public class AiSummary
    {
        public int Id { get; set; }
        public int ProjectRegistrationId { get; set; }
        public string VolunteerName { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string Weaknesses { get; set; } = string.Empty;
        public int MatchPercent { get; set; }
        public string Overall { get; set; } = string.Empty;
        public string Source { get; set; } = "Groq";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
