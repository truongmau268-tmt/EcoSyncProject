using System.Text;
using System.Text.Json;

namespace EcoSyncWeb.Services
{
    /// <summary>
    /// Client gọi Groq qua REST API tương thích OpenAI:
    /// POST {BaseUrl}/chat/completions (mặc định https://api.groq.com/openai/v1).
    /// Cấu hình trong appsettings.json hoặc dùng biến môi trường API_KEY/GROQ_API_KEY.
    /// </summary>
    public class GroqClient : IAiClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<GroqClient> _logger;

        public GroqClient(HttpClient http, IConfiguration config, ILogger<GroqClient> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public string ProviderName => "Groq";

        public string ApiKey
        {
            get
            {
                var configuredKey = _config["Groq:ApiKey"];
                return !string.IsNullOrWhiteSpace(configuredKey)
                    ? configuredKey
                    : Environment.GetEnvironmentVariable("API_KEY")
                        ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
                        ?? string.Empty;
            }
        }

        public string Model => string.IsNullOrWhiteSpace(_config["Groq:Model"])
            ? "openai/gpt-oss-120b"
            : _config["Groq:Model"]!;

        public string BaseUrl => string.IsNullOrWhiteSpace(_config["Groq:BaseUrl"])
            ? "https://api.groq.com/openai/v1"
            : _config["Groq:BaseUrl"]!.TrimEnd('/');

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

        public async Task<string?> GenerateAsync(
            string systemPrompt,
            List<ChatTurn> history,
            string userMessage,
            double temperature = 0.7,
            int maxOutputTokens = 1024)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("Groq API chưa cấu hình key. Hãy đặt Groq:ApiKey hoặc biến môi trường API_KEY/GROQ_API_KEY.");
                return null;
            }

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };
            foreach (var turn in history.TakeLast(12))
            {
                messages.Add(new { role = turn.Role == "model" ? "assistant" : "user", content = turn.Text });
            }
            messages.Add(new { role = "user", content = userMessage });

            var payload = new
            {
                model = Model,
                messages,
                temperature,
                max_tokens = maxOutputTokens
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);

                using var response = await _http.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Groq API lỗi {Status}: {Body}", (int)response.StatusCode, body);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var reply = choices[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
                    return string.IsNullOrEmpty(reply) ? null : reply;
                }

                _logger.LogWarning("Groq phản hồi không có choices: {Body}", body);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối Groq API.");
                return null;
            }
        }

        public async Task<JsonElement?> GenerateJsonAsync(string systemPrompt, string userMessage, int maxOutputTokens = 1024)
        {
            var raw = await GenerateAsync(systemPrompt, new List<ChatTurn>(), userMessage, temperature: 0.2, maxOutputTokens);
            return AiTextUtils.TryParseJsonElement(raw, _logger);
        }
    }
}
