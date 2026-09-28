using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.ASPECTSAssessment.Dtos
{
    public class UpdateASPECTSAssessmentRequest
    {
        [Required(ErrorMessage = "معرف التقييم مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        [Range(0, 1)] public int C { get; set; }
        [Range(0, 1)] public int P { get; set; }
        [Range(0, 1)] public int IC { get; set; }
        [Range(0, 1)] public int I { get; set; }
        [Range(0, 1)] public int M1 { get; set; }
        [Range(0, 1)] public int M2 { get; set; }
        [Range(0, 1)] public int M3 { get; set; }
        [Range(0, 1)] public int M4 { get; set; }
        [Range(0, 1)] public int M5 { get; set; }
        [Range(0, 1)] public int M6 { get; set; }
    }
}
