using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.Patient.Dtos
{
    public class CreatePatientRequest
    {
        [StringLength(20, ErrorMessage = "الرقم القومي لازم يكون أقل من 20 رقم")]
        public string? NationalId { get; set; }

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأوسط مطلوب")]
        [StringLength(50)]
        public string MiddleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "النوع مطلوب")]
        public string Gender { get; set; } = string.Empty;

        [Range(0.5, 500, ErrorMessage = "الوزن لازم يكون رقم منطقي")]
        public decimal WeightKg { get; set; }

        [Required(ErrorMessage = "الشكوى الرئيسية مطلوبة")]
        public string ChiefComplaint { get; set; } = string.Empty;
    }
}
