using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.NIHSSAssessment.Dtos
{
    // ملحوظة: AssessedById متسحوبة عمدًا، بتتحط من الكنترولر من actingUserId
    public class CreateNIHSSAssessmentRequest
    {
        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        [Range(0, 3)] public int LOC { get; set; }
        [Range(0, 2)] public int LOCQuestions { get; set; }
        [Range(0, 2)] public int LOCCommands { get; set; }
        [Range(0, 2)] public int BestGaze { get; set; }
        [Range(0, 3)] public int Visual { get; set; }
        [Range(0, 3)] public int FacialPalsy { get; set; }
        [Range(0, 4)] public int MotorArmLeft { get; set; }
        [Range(0, 4)] public int MotorArmRight { get; set; }
        [Range(0, 4)] public int MotorLegLeft { get; set; }
        [Range(0, 4)] public int MotorLegRight { get; set; }
        [Range(0, 2)] public int LimbAtaxia { get; set; }
        [Range(0, 2)] public int Sensory { get; set; }
        [Range(0, 3)] public int BestLanguage { get; set; }
        [Range(0, 2)] public int Dysarthria { get; set; }
        [Range(0, 2)] public int Extinction { get; set; }
    }
}
