using Microsoft.AspNetCore.Mvc;
using EcoSyncWeb.Data;
using EcoSyncWeb.Models;
using EcoSyncWeb.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace EcoSyncWeb.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiClient _ai;

        public FeedbackController(ApplicationDbContext context, IAiClient ai)
        {
            _context = context;
            _ai = ai;
        }

        // --- GỬI ĐÁNH GIÁ / KHẢO SÁT NHẬN GCN ---
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Submit(int projectId, string criteria1, string criteria2, string criteria3, string content, int rating)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung chia sẻ ngắn bằng lời!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }

            // Tổng hợp nội dung đánh giá chi tiết các tiêu chí kèm lời chia sẻ của TNV
            string fullContent = $"[TC1 - Tổ chức: {criteria1}] | [TC2 - Hậu cần: {criteria2}] | [TC3 - Ý nghĩa: {criteria3}] - Ý kiến: {content}";

            // --- PB16: AI SENTIMENT ANALYSIS (Groq) ---
            string sentiment = await AnalyzeSentimentAsync(fullContent, rating);

            var currentUserName = User.Identity?.Name ?? "Tình nguyện viên";
            var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);

            var feedback = new Feedback
            {
                ProjectId = projectId,
                Content = fullContent,
                Rating = rating,
                Sentiment = sentiment,
                AuthorName = currentUserName
            };

            _context.Feedbacks.Add(feedback);

            // --- TỰ ĐỘNG ĐÁNH DẤU TNV ĐÃ HOÀN THÀNH KHẢO SÁT ---
            var registration = _context.ProjectRegistrations
                .FirstOrDefault(r => r.ProjectId == projectId && (r.Email == currentUserEmail || r.FullName == currentUserName));

            if (registration != null)
            {
                registration.IsEvaluated = true;
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đã gửi đánh giá thành công! AI đã phân tích cảm xúc phản hồi của bạn để làm căn cứ cấp Giấy chứng nhận.";
            return RedirectToAction("Details", "Project", new { id = projectId });
        }

        /// <summary>
        /// PB16 - Gọi Groq phân loại cảm xúc phản hồi: Tích cực / Trung tính / Tiêu cực.
        /// Fallback về luật từ khóa nếu Groq không khả dụng.
        /// </summary>
        private async Task<string> AnalyzeSentimentAsync(string text, int rating)
        {
            if (_ai.IsConfigured)
            {
                var system = "Bạn là mô hình phân tích cảm xúc (sentiment analysis) cho nền tảng tình nguyện EcoSync. " +
                             "Nhiệm vụ: đọc phản hồi của tình nguyện viên sau chiến dịch và phân loại cảm xúc. " +
                             "CHỈ trả về JSON thuần đúng định dạng: {\"label\": \"...\", \"reason\": \"...\"} " +
                             "trong đó label là một trong đúng 3 giá trị: Tích cực, Trung tính, Tiêu cực; " +
                             "reason là 1 câu tiếng Việt ngắn giải thích. Không thêm chữ nào khác ngoài JSON.";
                var user = $"Đánh giá theo tiêu chí: {text}\nSố sao tự chấm: {rating}/5";

                var json = await _ai.GenerateJsonAsync(system, user, maxOutputTokens: 256);
                if (json != null && json.Value.TryGetProperty("label", out var labelEl))
                {
                    var label = labelEl.GetString() ?? "";
                    if (label.Contains("Tích cực")) return "Tích cực 🌟 (AI)";
                    if (label.Contains("Tiêu cực") || label.Contains("tiêu cực")) return "Tiêu cực ⚠️ (AI)";
                    if (label.Contains("Trung tính") || label.Contains("Trung lập")) return "Trung tính 🙂 (AI)";
                }
            }

            // Fallback: luật từ khóa
            string lower = text.ToLower();
            if (lower.Contains("không hài lòng") || lower.Contains("tệ") || lower.Contains("chán"))
            {
                return "Tiêu cực ⚠️";
            }
            if (lower.Contains("hài lòng") || lower.Contains("tốt") || lower.Contains("ý nghĩa"))
            {
                return "Tích cực 🌟";
            }
            return rating >= 4 ? "Tích cực 🌟" : "Trung tính 🙂";
        }

        // --- DASHBOARD PHÂN TÍCH AI (DÀNH CHO ADMIN) - PB13 + PB16 ---
        [Authorize]
        public async Task<IActionResult> Analytics()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang phân tích AI này!";
                return RedirectToAction("Index", "Home");
            }

            var feedbacks = _context.Feedbacks.OrderByDescending(f => f.CreatedAt).ToList();
            var projects = _context.Projects.ToList();
            var registrations = _context.ProjectRegistrations.ToList();

            ViewBag.TotalFeedbacks = feedbacks.Count;
            ViewBag.PositiveCount = feedbacks.Count(f => f.Sentiment.Contains("Tích cực"));
            ViewBag.NeutralCount = feedbacks.Count(f => f.Sentiment.Contains("Trung tính"));
            ViewBag.NegativeCount = feedbacks.Count(f => f.Sentiment.Contains("Cần cải thiện") || f.Sentiment.Contains("Tiêu cực"));
            ViewBag.AvgRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 0;

            // 4 thẻ KPI (PB13)
            ViewBag.KpiCampaigns = projects.Count;
            ViewBag.KpiVolunteers = registrations.Select(r => r.Email).Where(e => !string.IsNullOrEmpty(e)).Distinct().Count();
            ViewBag.KpiRegistrations = registrations.Count;
            ViewBag.KpiCertificates = registrations.Count(r => r.IsCertified);

            // Dữ liệu biểu đồ cột: lượt đăng ký theo chiến dịch
            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(
                projects.OrderBy(p => p.StartTime).Select(p => p.Title.Length > 22 ? p.Title[..22] + "…" : p.Title).ToList());
            ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(
                projects.OrderBy(p => p.StartTime).Select(p => registrations.Count(r => r.ProjectId == p.Id)).ToList());

            // Executive Summary bằng Groq (PB16 nâng cao) - có cache 10 phút để đỡ tốn quota
            string? summaryJson = null;
            var cacheKey = "AiExecSummary";
            var cached = HttpContext.Session?.GetString(cacheKey);
            if (cached != null)
            {
                summaryJson = cached;
            }
            else if (_ai.IsConfigured && feedbacks.Any())
            {
                var system = "Bạn là trợ lý phân tích dữ liệu khảo sát của nền tảng tình nguyện EcoSync. " +
                             "CHỈ trả về JSON thuần: {\"strengths\": [\"...\", \"...\"], \"weaknesses\": [\"...\", \"...\"], \"overall\": \"...\"} " +
                             "với strengths/weaknesses là mảng 2-3 câu tiếng Việt ngắn trích xuất từ phản hồi thật, " +
                             "overall là nhận xét chung 2 câu. Không thêm chữ nào khác ngoài JSON.";
                var sample = string.Join("\n---\n", feedbacks.Take(30).Select(f => $"[{f.Sentiment}] {f.Rating}/5 sao: {f.Content}"));
                var json = await _ai.GenerateJsonAsync(system, $"Danh sách phản hồi sau chiến dịch:\n{sample}", maxOutputTokens: 1024);
                if (json != null)
                {
                    summaryJson = json.Value.GetRawText();
                    HttpContext.Session?.SetString(cacheKey, summaryJson);
                }
            }
            ViewBag.AiSummaryJson = summaryJson;

            return View(feedbacks);
        }
    }
}
