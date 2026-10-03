using Microsoft.AspNetCore.Mvc;
using EcoSyncWeb.Data;
using EcoSyncWeb.Models;
using EcoSyncWeb.Services;
using System.Security.Claims;

namespace EcoSyncWeb.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiClient _ai;

        public ChatbotController(ApplicationDbContext context, IAiClient ai)
        {
            _context = context;
            _ai = ai;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { reply = "Bạn vui lòng nhập câu hỏi để mình hỗ trợ nhé!", source = "fallback" });
            }

            string mode = request.Mode == "interview" ? "interview" : "advice";
            var history = (request.History ?? new List<ChatTurnDto>())
                .Select(h => new ChatTurn
                {
                    Role = h.Role == "model" ? "model" : "user",
                    Text = h.Text ?? string.Empty
                })
                .Where(h => !string.IsNullOrWhiteSpace(h.Text))
                .ToList();

            var projects = _context.Projects
                .OrderByDescending(p => p.StartTime)
                .ToList();

            string systemPrompt = mode == "interview"
                ? BuildInterviewPrompt(projects)
                : BuildAdvicePrompt(projects);

            string? reply = null;
            string source = "fallback";

            if (_ai.IsConfigured)
            {
                reply = await _ai.GenerateAsync(systemPrompt, history, request.Message, temperature: 0.8);
                if (!string.IsNullOrWhiteSpace(reply)) source = "groq";
            }

            // Fallback khi chưa cấu hình API key hoặc lỗi mạng/quota
            if (string.IsNullOrWhiteSpace(reply))
            {
                reply = mode == "interview"
                    ? "😔 Trợ lý AI phỏng vấn đang tạm thời không khả dụng (chưa cấu hình API key AI hoặc vượt hạn mức). Vui lòng thử lại sau, bạn vẫn có thể nộp đơn bình thường nhé!"
                    : KeywordFallback(request.Message, projects);
            }

            // PB14: lưu toàn bộ hội thoại vào ai_chat_logs
            var email = User.FindFirstValue(ClaimTypes.Email) ?? "Khách";
            _context.AiChatLogs.Add(new AiChatLog
            {
                UserEmail = email,
                Mode = mode,
                Message = request.Message,
                Reply = reply,
                Source = source == "groq" ? "Groq" : "Fallback"
            });
            _context.SaveChanges();

            return Json(new { reply, source });
        }

        private static string BuildAdvicePrompt(List<Project> projects)
        {
            var list = string.Join("\n", projects.Select(p =>
                $"- {p.Title} | {p.Status} | {p.StartTime:dd/MM/yyyy HH:mm} - {p.EndTime:dd/MM/yyyy HH:mm} | {p.Location} | {p.Description}"));

            return $"""
                Bạn là "Trợ lý AI EcoSync" — chatbot tư vấn của nền tảng quản lý dự án tình nguyện môi trường EcoSync tại Đà Nẵng, Việt Nam.

                Dữ liệu chiến dịch hiện có trong hệ thống:
                {list}

                Nhiệm vụ:
                1. Tư vấn, giải đáp mọi thắc mắc về các chiến dịch trên (thời gian, địa điểm, cách đăng ký).
                2. Tư vấn kiến thức môi trường: phân loại rác tại nguồn, sống xanh, reduce-reuse-recycle.
                3. Hướng dẫn sử dụng website EcoSync: đăng ký tài khoản, đăng ký tham gia chiến dịch, xem giấy chứng nhận.
                4. Trả lời bằng tiếng Việt, thân thiện, ngắn gọn (dưới 120 từ), có thể dùng emoji vừa phải và **in đậm** từ khóa quan trọng.
                5. Khi được hỏi tin tức, hãy gợi ý người dùng vào mục "Tin tức" trên menu.
                Chỉ trả lời trực tiếp câu hỏi, không lặp lại lời dẫn.
                """;
        }

        private static string BuildInterviewPrompt(List<Project> projects)
        {
            var list = string.Join("\n", projects.Select(p => $"- {p.Title} ({p.Status})"));

            return $"""
                Bạn là "AI Phỏng vấn viên EcoSync" — thực hiện phỏng vấn sơ loại tình nguyện viên cho các chiến dịch môi trường.

                Các chiến dịch đang tuyển:
                {list}

                Quy tắc phỏng vấn:
                1. Bạn đặt câu hỏi TỪNG CÂU MỘT LƯỢT (không hỏi dồn nhiều câu cùng lúc). Trọng tâm 5 chủ đề: (1) động lực tham gia, (2) kinh nghiệm tình nguyện/hoạt động cộng đồng, (3) kỹ năng bản thân (giao tiếp, tổ chức, ngoại ngữ...), (4) thời gian có thể cam kết, (5) một tình huống xử lý trong chiến dịch (ví dụ thời tiết xấu, thiếu dụng cụ, teammate bỏ về).
                2. Mở đầu bằng lời chào 1 câu ngắn rồi hỏi câu thứ nhất.
                3. Sau mỗi câu trả lời của ứng viên: đáp ngắn gọn 1 câu cảm ơn/nhận xét rồi hỏi câu tiếp theo.
                4. Sau khi đủ 5 câu trả lời (hoặc ứng viên xin kết thúc), bạn TỔNG HỢP: 2-3 điểm mạnh, 1-2 điểm cần cải thiện, nhận xét mức độ phù hợp với hoạt động môi trường (Thấp/Trung bình/Cao), và lời khuyên.
                5. Kết thúc bằng đúng dòng: [KẾT THÚC PHỎNG VẤN]
                6. Toàn bộ bằng tiếng Việt, giọng điệu chuyên nghiệp nhưng thân thiện.
                """;
        }

        // Logic phân tích từ khóa dự phòng khi Groq không khả dụng
        private static string KeywordFallback(string rawMessage, List<Project> projects)
        {
            string userMsg = rawMessage.ToLower().Trim();

            foreach (var p in projects)
            {
                string pNameShort = p.Title.ToLower();
                if ((userMsg.Contains("đổi rác") && pNameShort.Contains("đổi rác")) ||
                    (userMsg.Contains("sơn trà") && p.Location.ToLower().Contains("sơn trà")) ||
                    (userMsg.Contains("thọ quang") && p.Location.ToLower().Contains("thọ quang")) ||
                    (userMsg.Contains("earth day") && pNameShort.Contains("earth day")) ||
                    (userMsg.Contains("clean up") && pNameShort.Contains("clean up")) ||
                    (userMsg.Contains("thế giới sạch") && pNameShort.Contains("thế giới sạch")) ||
                    userMsg.Contains(pNameShort))
                {
                    return $"🌿 **{p.Title}**\n- Trạng thái: {p.Status}\n- Ngày diễn ra: {p.StartDate:dd/MM/yyyy}\n- Địa điểm: {p.Location}\n- Mô tả: {p.Description}";
                }
            }

            if (userMsg.Contains("chiến dịch") || userMsg.Contains("dự án") || userMsg.Contains("hoạt động") || userMsg.Contains("có gì"))
            {
                if (projects.Any())
                {
                    string listReply = "📋 Các chiến dịch hiện có của EcoSync tại Đà Nẵng:\n";
                    foreach (var p in projects)
                    {
                        listReply += $"\n• **{p.Title}** ({p.Status}) - 📅 {p.StartDate:dd/MM/yyyy}";
                    }
                    return listReply;
                }
            }

            if (userMsg.Contains("đăng ký") || userMsg.Contains("tham gia") || userMsg.Contains("đki"))
            {
                return "🚀 Để tham gia, bạn vào mục **'Dự án cộng đồng'**, chọn chiến dịch có trạng thái **'Sắp diễn ra'** và bấm nút **'Điền thông tin đăng ký'** nhé!";
            }

            if (userMsg.Contains("phân loại rác") || userMsg.Contains("rác") || userMsg.Contains("môi trường") || userMsg.Contains("sống xanh"))
            {
                return "♻️ **Hướng dẫn phân loại rác tại nguồn:**\n1. **Rác hữu cơ:** Thức ăn thừa, rau củ quả hỏng... (có thể làm phân compost).\n2. **Rác tái chế:** Giấy, bìa carton, chai nhựa, vỏ lon...\n3. **Rác thải khác (rác còn lại):** Đồ dùng hỏng, túi nilon bẩn...\n\nHãy cùng chung tay bảo vệ môi trường xanh - sạch - đẹp tại Đà Nẵng cùng EcoSync nhé!";
            }

            if (userMsg.Contains("chào") || userMsg.Contains("hi") || userMsg.Contains("hello"))
            {
                return "Xin chào! Mình là trợ lý AI EcoSync. Bạn muốn tìm hiểu thông tin về chiến dịch nào hay cần tư vấn về cách bảo vệ môi trường không?";
            }

            return "Cảm ơn câu hỏi của bạn! EcoSync luôn sẵn sàng đồng hành cùng bạn trong các hoạt động tình nguyện, bảo vệ môi trường tại Đà Nẵng. Bạn có muốn xem danh sách các chiến dịch sắp diễn ra không?";
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? Mode { get; set; } // advice | interview
        public List<ChatTurnDto>? History { get; set; }
    }

    public class ChatTurnDto
    {
        public string Role { get; set; } = "user";
        public string Text { get; set; } = string.Empty;
    }
}
