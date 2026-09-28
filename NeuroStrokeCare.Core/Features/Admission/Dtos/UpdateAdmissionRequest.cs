using System;
using System.ComponentModel.DataAnnotations;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.Admission.Dtos
{
    // ملحوظة: AdmittedById متسحوبة عمدًا، بتتحط من الكنترولر من actingUserId زي باقي الحقول المشابهة
    // ملحوظة: StrokeType / StrokeTypeSetAt / StrokeTypeSetById اتحطوا هنا (بعكس Create) عشان الـ Update
    // الحالي بيستبدل الـ Entity كامل، فلو متسحبوش هيتصفروا كل تحديث. الوضع المثالي إنهم يترحلوا
    // لـ Command خاص بيهم بعدين (SetStrokeTypeCommand)، لحد ما يتعمل نخليهم كدى عشان منفقدش البيانات.
    public class UpdateAdmissionRequest
    {
        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "معرف المريض مطلوب")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "وقت الدخول مطلوب")]
        public DateTime AdmissionTime { get; set; }

        [Required(ErrorMessage = "حالة المريض مطلوبة")]
        public PatientStatus Status { get; set; }

        public StrokeType? StrokeType { get; set; }
        public DateTime? StrokeTypeSetAt { get; set; }
        public Guid? StrokeTypeSetById { get; set; }

        public Guid? BedId { get; set; }
        public DateTime? DischargeTime { get; set; }
        public string? DischargeNotes { get; set; }

        public bool CTDone { get; set; }
        public bool MRIDone { get; set; }
        public bool CTADone { get; set; }

        public string? CTFindings { get; set; }
        public string? MRIFindings { get; set; }
        public string? CTAFindings { get; set; }
    }
}
