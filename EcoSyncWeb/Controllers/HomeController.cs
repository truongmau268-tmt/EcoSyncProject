using Microsoft.AspNetCore.Mvc;

namespace EcoSyncWeb.Controllers
{
    public class HomeController : Controller
    {
        // Trang chủ cho phép tất cả mọi người (kể cả khách) truy cập tự do
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Project");
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}