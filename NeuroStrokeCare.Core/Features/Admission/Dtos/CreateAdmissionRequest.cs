using System;
using System.ComponentModel.DataAnnotations;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.Admission.Dtos
{
    // ملحوظة: AdmittedById متسحوبة من هنا عمدًا، بتتحط من الكنترولر من actingUserId
    // ملحوظة: StrokeType / StrokeTypeSetAt / StrokeTypeSetById متسحوبين لأنهم جزء من ووركفلو تحديد نوع السكتة
    // اللي هيتعمله Command خاص بعدين (مش وقت الإنشاء)
    public class CreateAdmissionRequest
    {
        [Required(ErrorMessage = "معرف المريض مطلوب")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "وقت الدخول مطلوب")]
        public DateTime AdmissionTime { get; set; }

        [Required(ErrorMessage = "حالة المريض مطلوبة")]
        public PatientStatus Status { get; set; }

        public Guid? BedId { get; set; }

        public bool CTDone { get; set; } = false;
        public bool MRIDone { get; set; } = false;
        public bool CTADone { get; set; } = false;

        public string? CTFindings { get; set; }
        public string? MRIFindings { get; set; }
        public string? CTAFindings { get; set; }
    }
}
