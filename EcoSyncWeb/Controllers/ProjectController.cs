using Microsoft.AspNetCore.Mvc;
using EcoSyncWeb.Data;
using EcoSyncWeb.Models;
using EcoSyncWeb.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace EcoSyncWeb.Controllers
{
    public class ProjectController : Controller
    {
        private const long MaxProjectImageSize = 5 * 1024 * 1024;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly IAiClient _ai;

        public ProjectController(ApplicationDbContext context, IAiClient ai, IWebHostEnvironment environment)
        {
            _context = context;
            _ai = ai;
            _environment = environment;
        }

        // --- DANH SÁCH DỰ ÁN + PB08: TÌM KIẾM & LỌC + TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI ---
        public IActionResult Index(string? keyword, string? location, string? status)
        {
            if (!_context.Projects.Any())
            {
                _context.Projects.AddRange(
                    new Project {
                        Title = "Chiến dịch Clean Up Việt Nam lần 8",
                        Description = "Phối hợp cùng LCĐ khoa Khoa học Xã hội & Nhân văn, Đại học Duy Tân và Đồn Biên phòng Sơn Trà. Thu gom thành công 80 bao rác với sự tham gia của 110 tình nguyện viên.",
                        Location = "Xanh Đà Nẵng, Sơn Trà",
                        StartTime = new DateTime(2026, 9, 20, 8, 0, 0),
                        EndTime = new DateTime(2026, 9, 20, 12, 0, 0),
                        TargetVolunteers = 110,
                        Status = "Đã kết thúc"
                    },
                    new Project {
                        Title = "Chiến dịch Earth Day Việt Nam 2026",
                        Description = "Hoạt động quy mô lớn với 800 tình nguyện viên, phối hợp cùng Đồn Biên phòng Hải Vân và nhiều cơ quan ban ngành, thu gom được 461 bịch rác nhằm lan tỏa tinh thần sống xanh.",
                        Location = "Đầu cầu Xanh, Đà Nẵng",
                        StartTime = new DateTime(2026, 4, 19, 7, 30, 0),
                        EndTime = new DateTime(2026, 4, 19, 11, 30, 0),
                        TargetVolunteers = 800,
                        Status = "Đã kết thúc"
                    },
                    new Project {
                        Title = "Ra quân làm sạch Âu thuyền Thọ Quang",
                        Description = "Hoạt động thu gom rác thải nhựa trôi nổi và ven bờ tại khu vực Âu thuyền. Cần tuyển 50 bạn tình nguyện viên tham gia cùng Ban chấp hành Đoàn trường.",
                        Location = "Âu thuyền Thọ Quang, Sơn Trà",
                        StartTime = new DateTime(2026, 10, 25, 8, 0, 0),
                        EndTime = new DateTime(2026, 10, 25, 11, 0, 0),
                        TargetVolunteers = 50,
                        Status = "Sắp diễn ra"
                    },
                    new Project {
                        Title = "Đổi rác lấy quà - Khu vực Hòa Khánh",
                        Description = "Khuyến khích phân loại rác tại nguồn. Hỗ trợ người dân thu gom giấy, nhựa, kim loại để đổi lấy các nhu yếu phẩm xanh.",
                        Location = "Chợ Hòa Khánh, Liên Chiểu",
                        StartTime = new DateTime(2026, 11, 15, 7, 0, 0),
                        EndTime = new DateTime(2026, 11, 15, 17, 0, 0),
                        TargetVolunteers = 30,
                        Status = "Sắp diễn ra"
                    }
                );
                _context.SaveChanges();
            }

            var projects = _context.Projects.OrderByDescending(p => p.StartTime).ToList();

            // LOGIC AUTO: Tự động cập nhật trạng thái theo giờ thực tế (DateTime.Now)
            foreach (var p in projects)
            {
                if (DateTime.Now >= p.EndTime)
                {
                    p.Status = "Đã kết thúc";
                }
                else if (DateTime.Now >= p.StartTime && DateTime.Now < p.EndTime)
                {
                    p.Status = "Đang diễn ra";
                }
                else
                {
                    p.Status = "Sắp diễn ra";
                }
            }
            _context.SaveChanges();

            // --- PB08: Tìm kiếm theo từ khóa & lọc theo địa điểm, trạng thái ---
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                projects = projects.Where(p =>
                    (p.Title ?? "").ToLower().Contains(k) ||
                    (p.Description ?? "").ToLower().Contains(k)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(location))
            {
                var loc = location.Trim().ToLower();
                projects = projects.Where(p => (p.Location ?? "").ToLower().Contains(loc)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                projects = projects.Where(p => p.Status == status).ToList();
            }

            ViewBag.Keyword = keyword;
            ViewBag.Location = location;
            ViewBag.Status = string.IsNullOrWhiteSpace(status) ? "Tất cả" : status;

            return View(projects);
        }

        // --- CHI TIẾT DỰ ÁN & ĐÁNH GIÁ ---
        public IActionResult Details(int id)
        {
            var project = _context.Projects.FirstOrDefault(p => p.Id == id);
            if (project == null) return RedirectToAction("Index");

            if (DateTime.Now >= project.EndTime)
            {
                project.Status = "Đã kết thúc";
            }
            else if (DateTime.Now >= project.StartTime && DateTime.Now < project.EndTime)
            {
                project.Status = "Đang diễn ra";
            }
            else
            {
                project.Status = "Sắp diễn ra";
            }
            _context.SaveChanges();

            var feedbacks = _context.Feedbacks
                .Where(f => f.ProjectId == id)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var currentUserName = User.Identity?.Name;
            var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);
            var registration = string.IsNullOrWhiteSpace(currentUserEmail)
                ? null
                : _context.ProjectRegistrations.FirstOrDefault(r =>
                    r.ProjectId == id && r.Email == currentUserEmail);

            var hasSubmittedFeedback = registration?.IsEvaluated == true ||
                feedbacks.Any(f =>
                    !string.IsNullOrWhiteSpace(currentUserEmail) && f.AuthorEmail == currentUserEmail ||
                    f.AuthorEmail == null && !string.IsNullOrWhiteSpace(currentUserName) && f.AuthorName == currentUserName);
            var canSubmitFeedback = User.Identity?.IsAuthenticated == true &&
                project.EndTime <= DateTime.Now &&
                registration?.Status == "Đã duyệt" &&
                !hasSubmittedFeedback;

            var feedbackEligibilityMessage = User.Identity?.IsAuthenticated != true
                ? "Vui lòng đăng nhập để gửi đánh giá."
                : project.EndTime > DateTime.Now
                    ? "Bạn có thể đánh giá sau khi chiến dịch kết thúc."
                    : registration == null || registration.Status != "Đã duyệt"
                        ? "Chỉ tình nguyện viên có hồ sơ tham gia đã được duyệt mới có thể đánh giá chiến dịch này."
                        : hasSubmittedFeedback
                            ? "Bạn đã gửi đánh giá cho chiến dịch này."
                            : string.Empty;

            ViewBag.HasSubmittedFeedback = hasSubmittedFeedback;
            ViewBag.CanSubmitFeedback = canSubmitFeedback;
            ViewBag.FeedbackEligibilityMessage = feedbackEligibilityMessage;
            ViewBag.LoginReturnUrl = Url.Action("Details", "Project", new { id });
            ViewBag.Feedbacks = feedbacks;
            return View(project);
        }

        // --- DANH SÁCH CÁ NHÂN (PB18: kèm trạng thái hồ sơ) ---
        [Authorize]
        public IActionResult MyRegistrations()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var myRegistrations = _context.ProjectRegistrations
                .Where(r => r.Email == userEmail)
                .ToList();

            var projectIds = myRegistrations.Select(r => r.ProjectId).ToList();
            var myProjects = _context.Projects
                .Where(p => projectIds.Contains(p.Id))
                .ToList();

            ViewBag.Registrations = myRegistrations;
            return View(myProjects);
        }

        // --- ĐĂNG KÝ THAM GIA ---
        [Authorize]
        public IActionResult RegisterForm(int projectId)
        {
            var project = _context.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null || DateTime.Now >= project.EndTime)
            {
                TempData["ErrorMessage"] = "Chiến dịch này đã kết thúc hoặc không tồn tại!";
                return RedirectToAction("Index");
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var fullName = User.Identity?.Name ?? "";

            var model = new ProjectRegistration
            {
                ProjectId = projectId,
                Email = userEmail ?? "",
                FullName = fullName
            };

            ViewBag.ProjectTitle = project.Title;
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public IActionResult RegisterForm(ProjectRegistration model)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            model.Email = userEmail ?? "";

            bool alreadyRegistered = _context.ProjectRegistrations
                .Any(r => r.ProjectId == model.ProjectId && r.Email == userEmail);

            if (alreadyRegistered)
            {
                TempData["ErrorMessage"] = "Bạn đã đăng ký tham gia chiến dịch này rồi!";
                return RedirectToAction("Index");
            }

            model.Status = "Chờ duyệt"; // PB10: hồ sơ mới luôn ở trạng thái chờ duyệt
            _context.ProjectRegistrations.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Hồ sơ của bạn đang chờ Admin duyệt. Hãy thử trò chuyện với AI Phỏng vấn viên (nút 🤖 góc phải) để hoàn thiện hồ sơ năng lực nhé!";
            return RedirectToAction("Index");
        }

        // --- DANH SÁCH TÌNH NGUYỆN VIÊN (ADMIN) + PB15: hiển thị AI Summary ---
        [Authorize]
        public IActionResult Volunteers(int id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index");
            }

            var project = _context.Projects.FirstOrDefault(p => p.Id == id);
            if (project == null) return RedirectToAction("Index");

            var volunteers = _context.ProjectRegistrations
                .Where(r => r.ProjectId == id)
                .OrderByDescending(r => r.RegisteredAt)
                .ToList();

            // Map AI Summary theo registration để hiển thị
            var summaries = _context.AiSummaries
                .GroupBy(s => s.ProjectRegistrationId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Id).First());
            ViewBag.Summaries = summaries;
            ViewBag.AiReady = _ai.IsConfigured;
            ViewBag.AiProviderName = _ai.ProviderName;

            ViewBag.Project = project;
            return View(volunteers);
        }

        // --- PB10: DUYỆT / LOẠI HỒ SƠ ỨNG TUYỂN ---
        [Authorize]
        public IActionResult SetRegistrationStatus(int id, string status)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index");
            }

            var registration = _context.ProjectRegistrations.FirstOrDefault(r => r.Id == id);
            if (registration != null && (status == "Đã duyệt" || status == "Đã loại" || status == "Chờ duyệt"))
            {
                registration.Status = status;
                _context.SaveChanges();
                TempData["SuccessMessage"] = status == "Đã duyệt"
                    ? $"Đã duyệt hồ sơ của {registration.FullName}!"
                    : (status == "Đã loại" ? $"Đã loại hồ sơ của {registration.FullName}!" : $"Đã chuyển hồ sơ {registration.FullName} về chờ duyệt!");
            }
            return RedirectToAction("Volunteers", new { id = registration?.ProjectId });
        }

        // --- PB15: TẠO AI SUMMARY ĐÁNH GIẢ NĂNG LỰC ỨNG VIÊN ---
        [Authorize]
        public async Task<IActionResult> GenerateAiSummary(int id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index");
            }

            var registration = _context.ProjectRegistrations.FirstOrDefault(r => r.Id == id);
            if (registration == null) return RedirectToAction("Index");

            if (!_ai.IsConfigured)
            {
                TempData["ErrorMessage"] = $"Chưa cấu hình API key cho provider AI '{_ai.ProviderName}' (appsettings.json) nên chưa tạo được AI Summary.";
                return RedirectToAction("Volunteers", new { id = registration.ProjectId });
            }

            // Thu thập dữ liệu: hồ sơ đăng ký + user + lịch sử chat AI + phản hồi đã gửi
            var project = _context.Projects.FirstOrDefault(p => p.Id == registration.ProjectId);
            var profile = _context.Users.FirstOrDefault(u => u.Email == registration.Email);
            var chatLogs = _context.AiChatLogs
                .Where(l => l.UserEmail == registration.Email)
                .OrderBy(l => l.Id)
                .Take(40)
                .ToList();
            var feedbacks = _context.Feedbacks
                .Where(f => f.ProjectId == registration.ProjectId && f.AuthorName == registration.FullName)
                .ToList();

            var transcript = string.Join("\n", chatLogs.Select(l => $"Người dùng: {l.Message}\nAI: {l.Reply}"));
            if (string.IsNullOrWhiteSpace(transcript)) transcript = "(Ứng viên chưa trò chuyện với AI phỏng vấn viên)";

            var system = "Bạn là trợ lý tuyển dụng của nền tảng tình nguyện EcoSync. " +
                         "Nhiệm vụ: đánh giá năng lực ứng viên tình nguyện dựa trên hồ sơ và transcript phỏng vấn AI. " +
                         "CHỈ trả về JSON thuần: {\"strengths\": \"...\", \"weaknesses\": \"...\", \"matchPercent\": 0, \"overall\": \"...\"} " +
                         "trong đó strengths/weaknesses là 2-3 ý ngắn tiếng Việt phân tách bằng dấu chấm phẩy, " +
                         "matchPercent là số nguyên 0-100 thể hiện mức độ phù hợp với hoạt động môi trường, " +
                         "overall là nhận xét chung 1-2 câu. Không thêm chữ nào khác ngoài JSON.";
            var user = $"Chiến dịch ứng tuyển: {project?.Title} ({project?.Location})\n" +
                       $"Hồ sơ: {registration.FullName} - SĐT {registration.Phone} - Ghi chú: {registration.Note}\n" +
                       $"Kỹ năng khai báo: {profile?.Skills ?? "(trống)"}\nKinh nghiệm: {profile?.Experience ?? "(trống)"}\n" +
                       $"Phản hồi sau chiến dịch: {(feedbacks.Any() ? string.Join(" | ", feedbacks.Select(f => f.Content)) : "(chưa có)")}\n\n" +
                       $"Transcript phỏng vấn AI:\n{transcript}";

            var json = await _ai.GenerateJsonAsync(system, user, maxOutputTokens: 1024);
            if (json == null)
            {
                TempData["ErrorMessage"] = $"Gọi AI ({_ai.ProviderName}) thất bại (kiểm tra API key/hạn mức). Vui lòng thử lại!";
                return RedirectToAction("Volunteers", new { id = registration.ProjectId });
            }

            var summary = new AiSummary
            {
                ProjectRegistrationId = registration.Id,
                VolunteerName = registration.FullName,
                Strengths = json.Value.TryGetProperty("strengths", out var s) ? (s.GetString() ?? "") : "",
                Weaknesses = json.Value.TryGetProperty("weaknesses", out var w) ? (w.GetString() ?? "") : "",
                MatchPercent = json.Value.TryGetProperty("matchPercent", out var m) && m.TryGetInt32(out var pct) ? pct : 0,
                Overall = json.Value.TryGetProperty("overall", out var o) ? (o.GetString() ?? "") : ""
            };

            // Xóa summary cũ nếu có rồi ghi bản mới
            var old = _context.AiSummaries.Where(x => x.ProjectRegistrationId == registration.Id).ToList();
            if (old.Any()) _context.AiSummaries.RemoveRange(old);
            _context.AiSummaries.Add(summary);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Đã phân tích xong hồ sơ của cộng tác viên: {registration.FullName}.Mức độ phù hợp {summary.MatchPercent}%.";
            return RedirectToAction("Volunteers", new { id = registration.ProjectId });
        }

        // --- QUẢN LÝ & CẤP GIẤY CHỨNG NHẬN (ADMIN) ---
        [Authorize]
        public IActionResult CertificatesManagement(int id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index");
            }

            var project = _context.Projects.FirstOrDefault(p => p.Id == id);
            if (project == null) return RedirectToAction("Index");

            var feedbacks = _context.Feedbacks
                .Where(f => f.ProjectId == id)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Truyền danh sách các registration đã được chứng nhận trong chiến dịch này
            var certifiedNames = _context.ProjectRegistrations
                .Where(r => r.ProjectId == id && r.IsCertified)
                .Select(r => r.FullName)
                .ToList();

            ViewBag.Project = project;
            ViewBag.CertifiedNames = certifiedNames;
            return View(feedbacks);
        }

        // --- CẤP GCN CHO PHẢN HỒI ---
        [Authorize]
        public IActionResult CertifyFeedback(int id) // id là Id của Feedback
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return RedirectToAction("Index");

            var feedback = _context.Feedbacks.FirstOrDefault(f => f.Id == id);
            if (feedback != null)
            {
                var registration = _context.ProjectRegistrations
                    .FirstOrDefault(r => r.ProjectId == feedback.ProjectId && r.FullName == feedback.AuthorName);

                if (registration != null)
                {
                    registration.IsCertified = true;
                    registration.IsEvaluated = true;
                    if (string.IsNullOrWhiteSpace(registration.CertificateCode))
                    {
                        registration.CertificateCode = GenerateCertificateCode(registration);
                    }
                }
                else
                {
                    // Nếu chưa có, tạo mới bản ghi đăng ký chính thức với trạng thái đã chứng nhận
                    var newReg = new ProjectRegistration
                    {
                        ProjectId = feedback.ProjectId,
                        FullName = feedback.AuthorName,
                        Email = "tnv@ecosync.vn",
                        IsEvaluated = true,
                        IsCertified = true,
                        Status = "Đã duyệt"
                    };
                    newReg.CertificateCode = GenerateCertificateCode(newReg);
                    _context.ProjectRegistrations.Add(newReg);
                }

                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã cấp Giấy chứng nhận thành công cho tình nguyện viên {feedback.AuthorName}!";
                return RedirectToAction("CertificatesManagement", new { id = feedback.ProjectId });
            }

            return RedirectToAction("Index");
        }

        // --- CẤP GCN ---
        [Authorize]
        public IActionResult CertifyVolunteer(int id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return RedirectToAction("Index");

            var registration = _context.ProjectRegistrations.FirstOrDefault(r => r.Id == id);
            if (registration != null)
            {
                registration.IsCertified = true;
                if (string.IsNullOrWhiteSpace(registration.CertificateCode))
                {
                    registration.CertificateCode = GenerateCertificateCode(registration);
                }
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"Đã cấp Giấy chứng nhận cho {registration.FullName}!";
            }

            return RedirectToAction("CertificatesManagement", new { id = registration?.ProjectId });
        }

        /// <summary>
        /// PB11: sinh mã định danh duy nhất cho giấy chứng nhận, ví dụ ECO-261003-7QF2.
        /// </summary>
        private static string GenerateCertificateCode(ProjectRegistration r)
        {
            var random = Guid.NewGuid().ToString("N")[..4].ToUpper();
            return $"ECO-{DateTime.Now:yyMMdd}-{r.ProjectId}{random}";
        }

        // --- GCN CỦA TÔI (PB12) ---
        [Authorize]
        public IActionResult MyCertificates()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var currentUserName = User.Identity?.Name;

            // Lọc linh hoạt theo Email hoặc FullName để đảm bảo TNV luôn nhận được GCN đã cấp
            var certifiedRegistrations = _context.ProjectRegistrations
                .Where(r => r.IsCertified && (r.Email == userEmail || r.FullName == currentUserName))
                .ToList();

            var projectIds = certifiedRegistrations.Select(r => r.ProjectId).ToList();
            var projects = _context.Projects
                .Where(p => projectIds.Contains(p.Id))
                .ToList();

            ViewBag.Registrations = certifiedRegistrations;
            return View(projects);
        }

        // --- XEM CHI TIẾT 1 GIẤY CHỨNG NHẬN ---
        [Authorize]
        public IActionResult Certificate(int projectId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var currentUserName = User.Identity?.Name;

            // Tìm bản ghi chứng nhận khớp với user hiện tại (lọc theo Email hoặc Tên)
            var registration = _context.ProjectRegistrations
                .FirstOrDefault(r => r.ProjectId == projectId && r.IsCertified && (r.Email == userEmail || r.FullName == currentUserName));

            if (registration == null)
            {
                TempData["ErrorMessage"] = "Bạn chưa được cấp Giấy chứng nhận cho chiến dịch này!";
                return RedirectToAction("MyCertificates");
            }

            var project = _context.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return RedirectToAction("MyCertificates");

            ViewBag.Registration = registration;
            return View(project);
        }

        // --- TẠO CHIẾN DỊCH (ADMIN) ---
        [Authorize]
        public IActionResult Create()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Project project)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return RedirectToAction("Index");

            var imageError = await ValidateProjectImageAsync(project.ImageFile);
            if (imageError != null) ModelState.AddModelError(nameof(project.ImageFile), imageError);
            if (!ModelState.IsValid) return View(project);

            if (project.EndTime <= project.StartTime)
            {
                TempData["ErrorMessage"] = "Thời gian kết thúc phải sau thời gian bắt đầu!";
                return View(project);
            }

            if (project.ImageFile != null)
            {
                project.ImageUrl = await SaveProjectImageAsync(project.ImageFile);
            }

            project.Status = ComputeStatus(project);
            _context.Projects.Add(project);
            _context.SaveChanges();
            TempData["SuccessMessage"] = project.ImageUrl != null
                ? "Đã phát động chiến dịch mới thành công và tải ảnh lên!"
                : "Đã phát động chiến dịch mới thành công! Bạn có thể thêm ảnh bằng nút Sửa.";
            return RedirectToAction("Index");
        }

        // --- SỬA CHIẾN DỊCH ---
        [Authorize]
        public IActionResult Edit(int id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return RedirectToAction("Index");

            var project = _context.Projects.FirstOrDefault(p => p.Id == id);
            if (project == null) return RedirectToAction("Index");

            return View(project);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(Project project)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return RedirectToAction("Index");

            var existingProject = _context.Projects.FirstOrDefault(p => p.Id == project.Id);
            if (existingProject == null) return RedirectToAction("Index");

            var imageError = await ValidateProjectImageAsync(project.ImageFile);
            if (imageError != null) ModelState.AddModelError(nameof(project.ImageFile), imageError);
            if (!ModelState.IsValid)
            {
                project.ImageUrl = existingProject.ImageUrl;
                return View(project);
            }

            if (project.EndTime <= project.StartTime)
            {
                project.ImageUrl = existingProject.ImageUrl;
                TempData["ErrorMessage"] = "Thời gian kết thúc phải sau thời gian bắt đầu!";
                return View(project);
            }

            var previousImageUrl = existingProject.ImageUrl;
            existingProject.Title = project.Title;
            existingProject.Description = project.Description;
            existingProject.StartTime = project.StartTime;
            existingProject.EndTime = project.EndTime;
            existingProject.Location = project.Location;
            existingProject.Status = ComputeStatus(existingProject);

            if (project.ImageFile != null)
            {
                existingProject.ImageUrl = await SaveProjectImageAsync(project.ImageFile);
            }

            _context.SaveChanges();
            if (project.ImageFile != null) DeleteProjectImage(previousImageUrl);
            TempData["SuccessMessage"] = "Cập nhật chiến dịch thành công!";
            return RedirectToAction("Index");
        }

        private static string ComputeStatus(Project project)
        {
            if (DateTime.Now >= project.EndTime) return "Đã kết thúc";
            if (DateTime.Now >= project.StartTime) return "Đang diễn ra";
            return "Sắp diễn ra";
        }

        // --- XÓA CHIẾN DỊCH (PB07: chặn xóa khi đã có CTV đăng ký) ---
        [Authorize]
        public IActionResult Delete(int id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return RedirectToAction("Index");

            var project = _context.Projects.FirstOrDefault(p => p.Id == id);
            if (project != null)
            {
                bool hasRegistrations = _context.ProjectRegistrations.Any(r => r.ProjectId == id);
                if (hasRegistrations)
                {
                    TempData["ErrorMessage"] = "Không thể xóa chiến dịch đã có tình nguyện viên nộp đơn ứng tuyển! Hãy chuyển trạng thái sang 'Đã kết thúc' thay vì xóa.";
                    return RedirectToAction("Index");
                }

                _context.Projects.Remove(project);
                _context.SaveChanges();
                DeleteProjectImage(project.ImageUrl);
                TempData["SuccessMessage"] = "Đã xóa chiến dịch thành công!";
            }
            return RedirectToAction("Index");
        }

        private static async Task<string?> ValidateProjectImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0) return null;
            if (image.Length > MaxProjectImageSize)
            {
                return "Ảnh không được vượt quá 5 MB.";
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (extension is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
            {
                return "Chỉ chấp nhận ảnh JPG, PNG, GIF hoặc WEBP.";
            }

            var header = new byte[12];
            await using var stream = image.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory());
            var isJpeg = extension is ".jpg" or ".jpeg"
                && bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var isPng = extension == ".png"
                && bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            var isGif = extension == ".gif"
                && bytesRead >= 6 && (header.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || header.AsSpan(0, 6).SequenceEqual("GIF89a"u8));
            var isWebp = extension == ".webp"
                && bytesRead >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);

            return isJpeg || isPng || isGif || isWebp
                ? null
                : "Nội dung tệp không khớp với định dạng ảnh đã chọn.";
        }

        private async Task<string> SaveProjectImageAsync(IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "projects");
            Directory.CreateDirectory(uploadDirectory);

            var filePath = Path.Combine(uploadDirectory, fileName);
            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
            await image.CopyToAsync(stream);

            return $"/uploads/projects/{fileName}";
        }

        private void DeleteProjectImage(string? imageUrl)
        {
            const string uploadPrefix = "/uploads/projects/";
            if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith(uploadPrefix, StringComparison.Ordinal))
            {
                return;
            }

            var fileName = imageUrl[uploadPrefix.Length..];
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
            {
                return;
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "projects", fileName);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
    }
}
