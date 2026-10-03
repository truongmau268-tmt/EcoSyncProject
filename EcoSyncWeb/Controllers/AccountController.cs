using Microsoft.AspNetCore.Mvc;
using EcoSyncWeb.Data;
using EcoSyncWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace EcoSyncWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- ĐĂNG NHẬP (GET) ---
        public IActionResult Login()
        {
            return View();
        }

        // --- ĐĂNG NHẬP (POST) ---
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ email và mật khẩu!";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null || !VerifyPassword(password, user))
            {
                TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác!";
                return View();
            }

            // PB01: chặn đăng nhập tài khoản đã bị khóa
            if (user.IsBlocked)
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa bởi quản trị viên. Vui lòng liên hệ EcoSync để được hỗ trợ!";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "Collaborator")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Project");
        }

        /// <summary>
        /// Xác thực mật khẩu: hỗ trợ hash BCrypt và tự nâng cấp các tài khoản cũ còn lưu plaintext.
        /// </summary>
        private bool VerifyPassword(string password, User user)
        {
            if (user.PasswordHash.StartsWith("$2")) // định dạng hash của BCrypt
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            if (user.PasswordHash == password)
            {
                // Tài khoản cũ lưu plaintext -> tự băm lại để nâng cấp bảo mật
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        // --- ĐĂNG KÝ (GET) ---
        public IActionResult Register()
        {
            return View();
        }

        // --- ĐĂNG KÝ (POST) ---
        [HttpPost]
        public IActionResult Register(string fullName, string email, string phone, string password)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin đăng ký!";
                return View();
            }

            // PB02: mật khẩu tối thiểu 8 ký tự (áp dụng cho tài khoản mới)
            if (password.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu phải có tối thiểu 6 ký tự!";
                return View();
            }

            // PB02: chuẩn hóa email + chặn trùng
            email = email.Trim().ToLower();
            bool exists = _context.Users.Any(u => u.Email == email);
            if (exists)
            {
                TempData["ErrorMessage"] = "Email này đã được đăng ký trước đó!";
                return View();
            }

            // PB02: validate số điện thoại chuẩn 10 số Việt Nam
            if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone.Trim(), @"^0\d{9}$"))
            {
                TempData["ErrorMessage"] = "Số điện thoại phải gồm đúng 10 số và bắt đầu bằng 0 (ví dụ: 0934892302)!";
                return View();
            }

            var newUser = new User
            {
                FullName = fullName.Trim(),
                Email = email,
                Phone = phone?.Trim() ?? "",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), // PB02: băm mật khẩu BCrypt
                Role = "Collaborator"
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // --- ĐĂNG XUẤT (PB05) ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Project");
        }

        // --- XEM HỒ SƠ CÁ NHÂN (PB06) ---
        [Authorize]
        public IActionResult Profile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            // Nếu đăng nhập bằng tài khoản mẫu chưa có trong DB thì hiển thị bản tạm
            if (user == null)
            {
                user = new User { FullName = User.Identity?.Name ?? "Tình nguyện viên", Email = userEmail ?? "", Role = "Collaborator" };
            }

            return View(user);
        }

        // --- CHỈNH SỬA HỒ SƠ (POST) - PB06: thêm kỹ năng, kinh nghiệm ---
        [HttpPost]
        [Authorize]
        public IActionResult EditProfile(User model)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản trong hệ thống!";
                return RedirectToAction("Profile");
            }

            // PB06: giới hạn kỹ năng & kinh nghiệm tối đa 1000 ký tự
            if ((model.Skills?.Length ?? 0) > 1000 || (model.Experience?.Length ?? 0) > 1000)
            {
                TempData["ErrorMessage"] = "Kỹ năng và kinh nghiệm mỗi mục chỉ được tối đa 1000 ký tự!";
                return RedirectToAction("Profile");
            }
            if (!string.IsNullOrWhiteSpace(model.Phone) && !Regex.IsMatch(model.Phone.Trim(), @"^0\d{9}$"))
            {
                TempData["ErrorMessage"] = "Số điện thoại phải gồm đúng 10 số và bắt đầu bằng 0!";
                return RedirectToAction("Profile");
            }

            user.FullName = model.FullName;
            user.Phone = model.Phone ?? "";
            user.Skills = model.Skills;
            user.Experience = model.Experience;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
            return RedirectToAction("Profile");
        }

        // --- ĐỔI MẬT KHẨU (PB04) ---
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản trong hệ thống!";
                return View();
            }
            if (string.IsNullOrWhiteSpace(oldPassword) || !VerifyPassword(oldPassword, user))
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác!";
                return View();
            }
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có tối thiểu 8 ký tự!";
                return View();
            }
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và mật khẩu xác nhận không khớp!";
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // --- QUẢN LÝ THÀNH VIÊN (PB19) - ADMIN ---
        [Authorize]
        public IActionResult Users(string? keyword)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Project");
            }

            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(k) || u.Email.ToLower().Contains(k) || (u.Phone ?? "").Contains(k));
            }

            ViewBag.Keyword = keyword;
            ViewBag.CurrentEmail = User.FindFirstValue(ClaimTypes.Email);
            return View(query.OrderByDescending(u => u.Id).ToList());
        }

        // --- KHÓA / MỞ KHÓA TÀI KHOẢN (PB19) ---
        [Authorize]
        public IActionResult ToggleBlock(int id)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Project");
            }

            var target = _context.Users.FirstOrDefault(u => u.Id == id);
            var currentEmail = User.FindFirstValue(ClaimTypes.Email);

            // PB19: Admin không được tự khóa chính mình
            if (target != null && target.Email == currentEmail)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa chính tài khoản của mình!";
                return RedirectToAction("Users");
            }

            if (target != null)
            {
                target.IsBlocked = !target.IsBlocked;
                _context.SaveChanges();
                TempData["SuccessMessage"] = target.IsBlocked
                    ? $"Đã khóa tài khoản {target.Email}!"
                    : $"Đã mở khóa tài khoản {target.Email}!";
            }
            return RedirectToAction("Users");
        }
    }
}
