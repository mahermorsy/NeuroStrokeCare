using System;
using System.Collections.Generic;

namespace NeuroStrokeCare.Service.Auth.Dtos
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public IEnumerable<string>? Errors { get; set; }

        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }

        public string? Token { get; set; }
        public DateTime? ExpiresOn { get; set; }

        // بيتملي بس في ForgotPassword مؤقتًا لحد ما نضيف خدمة إيميل حقيقية -
        // في الإنتاج المفروض التوكن ده يترسل بإيميل مش يرجع في الـ Response
        public string? ResetToken { get; set; }
    }
}
