using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.BradenAssessment.Dtos
{
    // ملحوظة: AssessedById متسحوبة عمدًا، بتتحط من الكنترولر من actingUserId
    public class CreateBradenAssessmentRequest
    {
        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        [Range(1, 4)] public int SensoryPerception { get; set; }
        [Range(1, 4)] public int Moisture { get; set; }
        [Range(1, 4)] public int Activity { get; set; }
        [Range(1, 4)] public int Mobility { get; set; }
        [Range(1, 4)] public int Nutrition { get; set; }
        [Range(1, 3)] public int FrictionShear { get; set; }
    }
}
