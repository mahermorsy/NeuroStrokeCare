using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.GUSSAssessment.Dtos
{
    // ملحوظة: AssessedById متسحوبة عمدًا، بتتحط من الكنترولر من actingUserId
    // ملحوظة: الاختبار المباشر (Semisolid/Liquid/Solid) بيتنفذ بس لو IndirectScore = 5،
    // فاتسيبوا اختياريين هنا، والتحقق من ترتيب التنفيذ (workflow) هيتعمل بعدين لو احتجناه
    public class CreateGUSSAssessmentRequest
    {
        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        public bool Vigilance { get; set; }
        public bool VoluntaryCough { get; set; }
        public bool SalivaSwallowSuccessful { get; set; }
        public bool NoDrooling { get; set; }
        public bool NoVoiceChange { get; set; }

        [Range(0, 5)] public int? SemisolidScore { get; set; }
        [Range(0, 5)] public int? LiquidScore { get; set; }
        [Range(0, 5)] public int? SolidScore { get; set; }
    }
}
