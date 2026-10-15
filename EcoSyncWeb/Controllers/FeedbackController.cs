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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int projectId, string content, int rating)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung nhận xét!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }
            if (content.Trim().Length > 1000)
            {
                TempData["ErrorMessage"] = "Nội dung nhận xét không được vượt quá 1000 ký tự!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }
            if (rating is < 1 or > 5)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn mức đánh giá từ 1 đến 5 sao!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }

            var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);
            var currentUserName = User.Identity?.Name ?? "Tình nguyện viên";
            var project = _context.Projects.FirstOrDefault(p => p.Id == projectId);
            var registration = _context.ProjectRegistrations.FirstOrDefault(r =>
                r.ProjectId == projectId && r.Email == currentUserEmail);

            if (project == null || project.EndTime > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh giá chiến dịch đã kết thúc!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }
            if (registration == null || registration.Status != "Đã duyệt")
            {
                TempData["ErrorMessage"] = "Chỉ tình nguyện viên có hồ sơ tham gia đã được duyệt mới có thể đánh giá!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }
            if (registration.IsEvaluated || _context.Feedbacks.Any(f =>
                    f.ProjectId == projectId && f.AuthorEmail == currentUserEmail))
            {
                TempData["ErrorMessage"] = "Bạn đã gửi đánh giá cho chiến dịch này rồi!";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }

            var fullContent = content.Trim();
            var sentiment = await AnalyzeSentimentAsync(fullContent, rating);

            var feedback = new Feedback
            {
                ProjectId = projectId,
                Content = fullContent,
                Rating = rating,
                Sentiment = sentiment,
                AuthorName = currentUserName,
                AuthorEmail = currentUserEmail
            };

            _context.Feedbacks.Add(feedback);
            registration.IsEvaluated = true;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá!";
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
