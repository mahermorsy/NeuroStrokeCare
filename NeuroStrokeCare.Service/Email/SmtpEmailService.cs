using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeuroStrokeCare.Service.Email
{
    // إرسال إيميل عن طريق SMTP عادي (System.Net.Mail المدمجة في .NET - مفيش NuGet package
    // إضافي محتاج تنزيل، عشان معندناش Compiler هنا نتأكد بيه إن التثبيت نجح).
    // الإعدادات بتتقرأ من قسم "Email" في appsettings.json (أو Environment Variables في
    // الإنتاج - الأفضل متحطش الباسورد الحقيقي في appsettings.json نفسه على الـ VPS).
    // لو الإعدادات فاضية (بيئة تطوير من غير SMTP حقيقي)، بيكتب الإيميل في الـ Logs بدل
    // ما يحاول يبعته فعليًا - عشان الفلو يفضل شغال في التطوير من غير ما يكسر.
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _configuration["Email:SmtpHost"];
            var fromAddress = _configuration["Email:FromAddress"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            {
                _logger.LogWarning(
                    "Email:SmtpHost / Email:FromAddress مش متظبطين - هيتسجل الإيميل في الـ Log بس بدل ما يترسل فعليًا. To={To} Subject={Subject}",
                    toEmail, subject);
                return;
            }

            var port = int.TryParse(_configuration["Email:SmtpPort"], out var p) ? p : 587;
            var username = _configuration["Email:SmtpUsername"];
            var password = _configuration["Email:SmtpPassword"];
            var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) || ssl;
            var fromName = _configuration["Email:FromName"] ?? "NeuroStrokeCare";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrWhiteSpace(username)
                    ? null
                    : new NetworkCredential(username, password),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}
