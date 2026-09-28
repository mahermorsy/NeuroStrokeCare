using System;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Data.Entities.Assessments
{
    public class GUSSAssessment : BaseEntity
    {
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        // الاختبار غير المباشر (Indirect) - كل بند نقطة، المجموع 0-5
        public bool Vigilance { get; set; }
        public bool VoluntaryCough { get; set; }
        public bool SalivaSwallowSuccessful { get; set; }
        public bool NoDrooling { get; set; }
        public bool NoVoiceChange { get; set; }

        public int IndirectScore =>
            (Vigilance ? 1 : 0) + (VoluntaryCough ? 1 : 0) + (SalivaSwallowSuccessful ? 1 : 0) +
            (NoDrooling ? 1 : 0) + (NoVoiceChange ? 1 : 0);

        // الاختبار المباشر - بيتنفذ بس لو IndirectScore = 5، وكل مرحلة (0-5) بتوقف التقدم لو فشلت
        public int? SemisolidScore { get; set; }
        public int? LiquidScore { get; set; }
        public int? SolidScore { get; set; }

        // المجموع الكلي 0-20 (5 غير مباشر + 5 لكل مرحلة من التلاتة)
        public int TotalScore =>
            IndirectScore < 5
                ? IndirectScore
                : IndirectScore + (SemisolidScore ?? 0) + (LiquidScore ?? 0) + (SolidScore ?? 0);

        public GUSSSeverity Severity => TotalScore switch
        {
            20 => GUSSSeverity.Normal,
            >= 15 => GUSSSeverity.Mild,
            >= 10 => GUSSSeverity.Moderate,
            _ => GUSSSeverity.Severe
        };

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}