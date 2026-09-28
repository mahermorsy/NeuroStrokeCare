using Microsoft.AspNetCore.Identity;
using System;

namespace NeuroStrokeCare.Data.UserApplication
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? NationalId { get; set; }
        public bool IsRootSuperAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // تعطيل الحساب بدل حذفه (متسق مع سياسة الـ Restrict على البيانات الطبية)
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}