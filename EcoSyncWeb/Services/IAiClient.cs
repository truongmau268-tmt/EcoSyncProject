using System.Text.Json;

namespace EcoSyncWeb.Services
{
    /// <summary>
    /// Một lượt thoại trong hội thoại với AI (role: "user" hoặc "model").
    /// </summary>
    public class ChatTurn
    {
        public string Role { get; set; } = "user";
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface cho client AI của ứng dụng.
    /// Khi chưa có API key hoặc lỗi mạng/quota => trả về null để caller fallback.
    /// </summary>
    public interface IAiClient
    {
        /// <summary>Tên provider để hiển thị/log.</summary>
        string ProviderName { get; }

        /// <summary>Đã cấu hình API key hay chưa.</summary>
        bool IsConfigured { get; }

        /// <summary>Sinh văn bản với system prompt + lịch sử hội thoại.</summary>
        Task<string?> GenerateAsync(
            string systemPrompt,
            List<ChatTurn> history,
            string userMessage,
            double temperature = 0.7,
            int maxOutputTokens = 1024);

        /// <summary>Gọi AI yêu cầu trả về JSON thuần (dùng cho Sentiment / AI Summary).</summary>
        Task<JsonElement?> GenerateJsonAsync(string systemPrompt, string userMessage, int maxOutputTokens = 1024);
    }

    /// <summary>
    /// Tiện ích dùng chung: bóc JSON thuần khỏi phản hồi của model
    /// (tự xử lý code fence ```json ...``` và lời dẫn thừa).
    /// </summary>
    public static class AiTextUtils
    {
        public static JsonElement? TryParseJsonElement(string? raw, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var text = raw.Trim();
            if (text.StartsWith("```"))
            {
                var firstNewLine = text.IndexOf('\n');
                if (firstNewLine > 0) text = text[(firstNewLine + 1)..];
                var fenceEnd = text.LastIndexOf("```", StringComparison.Ordinal);
                if (fenceEnd >= 0) text = text[..fenceEnd];
                text = text.Trim();
            }
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                text = text[start..(end + 1)];
            }

            try
            {
                return JsonDocument.Parse(text).RootElement.Clone();
            }
            catch
            {
                logger.LogWarning("AI trả về JSON không parse được: {Text}", text);
                return null;
            }
        }
    }
}
