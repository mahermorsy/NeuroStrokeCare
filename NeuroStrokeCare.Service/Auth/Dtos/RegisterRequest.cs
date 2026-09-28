using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Service.Auth.Dtos
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور لازم تكون 6 أحرف على الأقل")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [Compare(nameof(Password), ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        // اختياري - لو متبعتش هيتحط "Staff" افتراضيًا
        public string? Role { get; set; }
    }
}
