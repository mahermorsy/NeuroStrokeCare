using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.ICHAssessment.Dtos
{
    public class UpdateICHAssessmentRequest
    {
        [Required(ErrorMessage = "معرف التقييم مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        [Range(3, 15, ErrorMessage = "GCS لازم يكون بين 3 و 15")]
        public int GCSScore { get; set; }

        [Range(0, 1000, ErrorMessage = "حجم النزيف لازم يكون رقم منطقي")]
        public decimal ICHVolumeMl { get; set; }

        public bool InfratentorialOrigin { get; set; }
        public bool IVHPresent { get; set; }

        [Range(0, 1)]
        public int AgeScore { get; set; }
    }
}
