using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.GCSAssessment.Dtos
{
    // ملحوظة: AssessedById متسحوبة عمدًا، بتتحط من الكنترولر من actingUserId
    public class CreateGCSAssessmentRequest
    {
        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        [Range(1, 4)] public int EyeResponse { get; set; }
        [Range(1, 5)] public int VerbalResponse { get; set; }
        [Range(1, 6)] public int MotorResponse { get; set; }
    }
}
