using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.MorseAssessment.Dtos
{
    public class UpdateMorseAssessmentRequest
    {
        [Required(ErrorMessage = "معرف التقييم مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        public bool HistoryOfFalling { get; set; }
        public bool SecondaryDiagnosis { get; set; }

        [Range(0, 30, ErrorMessage = "القيم المسموحة: 0، 15، 30")]
        public int AmbulatoryAid { get; set; }

        public bool IVOrHeparinLock { get; set; }

        [Range(0, 20, ErrorMessage = "القيم المسموحة: 0، 10، 20")]
        public int Gait { get; set; }

        public bool MentalStatus { get; set; }
    }
}
