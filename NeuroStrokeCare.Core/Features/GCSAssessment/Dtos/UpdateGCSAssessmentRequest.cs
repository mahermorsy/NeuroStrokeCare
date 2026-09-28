using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.GCSAssessment.Dtos
{
    public class UpdateGCSAssessmentRequest
    {
        [Required(ErrorMessage = "معرف التقييم مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        [Range(1, 4)] public int EyeResponse { get; set; }
        [Range(1, 5)] public int VerbalResponse { get; set; }
        [Range(1, 6)] public int MotorResponse { get; set; }
    }
}
