using System.Text;
using System.Text.Json;

namespace EcoSyncWeb.Services
{
    /// <summary>
    /// Client gọi Grok của xAI qua REST API chuẩn OpenAI-compatible:
    /// POST {BaseUrl}/chat/completions (mặc định https://api.x.ai/v1).
    /// Cấu hình Model + BaseUrl trong appsettings.json và khóa API qua XAI_API_KEY
    /// (hoặc cấu hình Grok:ApiKey bên ngoài appsettings.json).
    /// </summary>
    public class GrokClient : IAiClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<GrokClient> _logger;

        public GrokClient(HttpClient http, IConfiguration config, ILogger<GrokClient> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public string ProviderName => "Grok";

        public string ApiKey
        {
            get
            {
                var configuredKey = _config["Grok:ApiKey"];
                return !string.IsNullOrWhiteSpace(configuredKey)
                    ? configuredKey
                    : Environment.GetEnvironmentVariable("XAI_API_KEY") ?? string.Empty;
            }
        }

        public string Model => string.IsNullOrWhiteSpace(_config["Grok:Model"])
            ? "grok-4.7"
            : _config["Grok:Model"]!;

        public string BaseUrl => string.IsNullOrWhiteSpace(_config["Grok:BaseUrl"])
            ? "https://api.x.ai/v1"
            : _config["Grok:BaseUrl"]!.TrimEnd('/');

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
                _logger.LogWarning("Grok API chưa cấu hình key. Hãy đặt biến môi trường XAI_API_KEY.");
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
                    _logger.LogError("Grok API lỗi {Status}: {Body}", (int)response.StatusCode, body);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var reply = choices[0].GetProperty("message").GetProperty("content").GetString()?.Trim();
                    return string.IsNullOrEmpty(reply) ? null : reply;
                }

                _logger.LogWarning("Grok phản hồi không có choices: {Body}", body);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối Grok API.");
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
