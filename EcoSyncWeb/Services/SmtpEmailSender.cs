using System.Net;
using System.Net.Mail;

namespace EcoSyncWeb.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetAsync(string email, string resetLink)
        {
            var settings = _configuration.GetSection("Smtp");
            var host = settings["Host"];
            var userName = settings["UserName"];
            var password = settings["Password"];
            var fromEmail = settings["FromEmail"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail) ||
                !int.TryParse(settings["Port"], out var port) ||
                port is < 1 or > 65535)
            {
                throw new InvalidOperationException("Thiếu cấu hình SMTP bắt buộc.");
            }

            MailAddress fromAddress;
            try
            {
                fromAddress = new MailAddress(fromEmail, settings["FromName"] ?? "EcoSync");
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Địa chỉ email gửi SMTP không hợp lệ.", ex);
            }

            using var message = new MailMessage
            {
                From = fromAddress,
                Subject = "Khôi phục mật khẩu EcoSync",
                Body = $"""
                    <p>Xin chào,</p>
                    <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu tài khoản EcoSync của bạn.</p>
                    <p><a href="{WebUtility.HtmlEncode(resetLink)}">Đặt lại mật khẩu</a></p>
                    <p>Liên kết chỉ có hiệu lực trong 15 phút và chỉ sử dụng được một lần.</p>
                    <p>Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.</p>
                    """,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = settings.GetValue("EnableSsl", true),
                Credentials = new NetworkCredential(userName, password)
            };
            await client.SendMailAsync(message);
        }
    }
}
