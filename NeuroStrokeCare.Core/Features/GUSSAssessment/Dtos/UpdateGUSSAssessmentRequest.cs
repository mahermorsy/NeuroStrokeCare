using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.GUSSAssessment.Dtos
{
    public class UpdateGUSSAssessmentRequest
    {
        [Required(ErrorMessage = "معرف التقييم مطلوب")]
        public Guid Id { get; set; }

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
