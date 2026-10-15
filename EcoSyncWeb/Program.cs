using Microsoft.EntityFrameworkCore;
using EcoSyncWeb.Data;
using EcoSyncWeb.Models;
using EcoSyncWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Kết nối Database SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

// Chỉ sử dụng Groq API cho các tính năng AI.
builder.Services.AddHttpClient<GroqClient>();
builder.Services.AddScoped<IAiClient>(sp => sp.GetRequiredService<GroqClient>());

builder.Services.AddSession();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- TỰ ĐỘNG TẠO BẢNG & DATABASE NẾU CHƯA CÓ (DÙNG MIGRATIONS HOẶC ENSURECREATED) ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Tự động áp dụng mọi Migration sang SQL Server hoặc tạo database mới
    dbContext.Database.Migrate();

    SeedData(dbContext);
}

// Seed tài khoản demo chuẩn (mật khẩu đã băm BCrypt) + tin tức mẫu
static void SeedData(ApplicationDbContext db)
{
    if (!db.Users.Any(u => u.Email == "Admin@gmail.com"))
    {
        db.Users.Add(new User
        {
            FullName = "Quản trị viên EcoSync",
            Email = "Admin@gmail.com",
            Phone = "0931270979",
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123")
        });
    }
    if (!db.Users.Any(u => u.Email == "khach@gmail.com"))
    {
        db.Users.Add(new User
        {
            FullName = "Khách Tình Nguyện Viên",
            Email = "khach@gmail.com",
            Phone = "0934892302",
            Role = "Collaborator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123")
        });
    }
    if (!db.News.Any())
    {
        db.News.AddRange(
            new News
            {
                Title = "EcoSync ra mắt nền tảng quản lý chiến dịch môi trường tích hợp AI",
                Content = "Ngày 04/10/2026, đội ngũ EcoSync chính thức ra mắt nền tảng số hóa toàn bộ quy trình tổ chức chiến dịch tình nguyện môi trường: từ đăng ký, phỏng vấn sơ loại bằng AI Chatbot, đến cấp giấy chứng nhận điện tử tự động. Hệ thống ứng dụng Groq nhằm giảm tải cho ban tổ chức và nâng cao trải nghiệm của tình nguyện viên."
            },
            new News
            {
                Title = "5 cách phân loại rác tại nguồn bạn có thể bắt đầu ngay hôm nay",
                Content = "Phân loại rác tại nguồn là bước đơn giản nhất để bảo vệ môi trường: (1) Rác hữu cơ - thức ăn thừa, rau củ hỏng có thể ủ compost; (2) Rác tái chế - giấy, carton, chai nhựa, lon kim loại nên làm sạch và ép phẳng; (3) Rác thải khác - túi nilon bẩn, đồ vỡ cần bọc kỹ trước khi vứt; (4) Pin và bóng đèn cũ phải bỏ vào điểm thu gom riêng; (5) Đồ điện tử cũ nên mang đến trung tâm thu hồi. Hãy bắt đầu từ căn bếp của bạn!"
            }
        );
    }
    db.SaveChanges();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();
 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Project}/{action=Index}/{id?}");

app.Run();