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

        // Pending-approval self-registration workflow: accounts created by an Admin (or the
        // dev seeder) are approved by default; accounts created through the public
        // self-registration endpoint start unapproved and cannot log in until an Admin approves
        // them and assigns a real role (RequestedRole is only what the applicant asked for).
        public bool IsApproved { get; set; } = true;
        public string? RequestedRole { get; set; }

        // Staff ID card: a hospital-assigned employee/carnet number (set by an Admin,
        // not chosen by the employee) and a self-uploaded profile photo, served from
        // the API's own /uploads static path (see Program.cs UseStaticFiles).
        public string? EmployeeId { get; set; }
        public string? ProfilePhotoUrl { get; set; }
    }
}