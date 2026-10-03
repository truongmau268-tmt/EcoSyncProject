using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoSyncWeb.Data;
using EcoSyncWeb.Models;
using System.Security.Claims;

namespace EcoSyncWeb.Controllers
{
    /// <summary>
    /// PB20: Tin tức & bài viết truyền thông môi trường.
    /// Khách/CTV xem danh sách + chi tiết; Admin quản lý CRUD.
    /// </summary>
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- DANH SÁCH BÀI VIẾT (công khai) ---
        public IActionResult Index(string? keyword)
        {
            var query = _context.News.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim().ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(k) || n.Content.ToLower().Contains(k));
            }
            ViewBag.Keyword = keyword;
            return View(query.OrderByDescending(n => n.CreatedAt).ToList());
        }

        // --- CHI TIẾT BÀI VIẾT (công khai) ---
        public IActionResult Details(int id)
        {
            var news = _context.News.FirstOrDefault(n => n.Id == id);
            if (news == null) return RedirectToAction("Index");

            var related = _context.News
                .Where(n => n.Id != id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(3)
                .ToList();
            ViewBag.Related = related;

            return View(news);
        }

        private bool IsAdmin() => User.FindFirstValue(ClaimTypes.Role) == "Admin";

        // --- TRANG QUẢN LÝ (ADMIN) ---
        [Authorize]
        public IActionResult Manage()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index");
            }
            return View(_context.News.OrderByDescending(n => n.CreatedAt).ToList());
        }

        // --- THÊM BÀI VIẾT (ADMIN) ---
        [Authorize]
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Create(News news)
        {
            if (!IsAdmin()) return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                news.CreatedAt = DateTime.Now;
                _context.News.Add(news);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã đăng bài viết mới thành công!";
                return RedirectToAction("Manage");
            }
            return View(news);
        }

        // --- SỬA BÀI VIẾT (ADMIN) ---
        [Authorize]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index");
            var news = _context.News.FirstOrDefault(n => n.Id == id);
            if (news == null) return RedirectToAction("Manage");
            return View(news);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(News model)
        {
            if (!IsAdmin()) return RedirectToAction("Index");
            var news = _context.News.FirstOrDefault(n => n.Id == model.Id);
            if (news == null) return RedirectToAction("Manage");

            if (ModelState.IsValid)
            {
                news.Title = model.Title;
                news.Content = model.Content;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã cập nhật bài viết thành công!";
                return RedirectToAction("Manage");
            }
            return View(model);
        }

        // --- XÓA BÀI VIẾT (ADMIN) ---
        [Authorize]
        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index");
            var news = _context.News.FirstOrDefault(n => n.Id == id);
            if (news != null)
            {
                _context.News.Remove(news);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa bài viết thành công!";
            }
            return RedirectToAction("Manage");
        }
    }
}
