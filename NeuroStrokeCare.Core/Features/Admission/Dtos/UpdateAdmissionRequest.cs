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

        // FINAL RELEASE-CANDIDATE PASS (section 4): optional on purpose - no current frontend
        // caller sends this (admissionsApi.update() has no call sites at all, confirmed by grep),
        // so making it required would break nothing today but would also gain nothing. If a
        // caller echoes back the RowVersion it read from AdmissionResponse, AdmissionController.Update
        // uses that as the optimistic-concurrency check's expected value (true protection against
        // edits made after this caller's own last read). If omitted, the controller falls back to
        // the RowVersion it reads itself at the very start of this request (still real protection
        // against a genuinely concurrent write landing mid-request, just not against staleness
        // that predates this request).
        public byte[]? RowVersion { get; set; }
    }
}
