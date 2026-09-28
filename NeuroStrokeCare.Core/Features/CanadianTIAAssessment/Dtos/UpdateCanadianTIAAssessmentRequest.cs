using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.CanadianTIAAssessment.Dtos
{
    public class UpdateCanadianTIAAssessmentRequest
    {
        [Required(ErrorMessage = "معرف التقييم مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت التقييم مطلوب")]
        public DateTime AssessedAt { get; set; }

        public bool FirstTIAInLifetime { get; set; }
        public bool SymptomsOver10Min { get; set; }
        public bool HistoryCarotidStenosis { get; set; }
        public bool OnAntiplateletTherapy { get; set; }
        public bool GaitDisturbance { get; set; }
        public bool UnilateralWeakness { get; set; }
        public bool HistoryOfVertigo { get; set; }
        public bool DiastolicBPOver110 { get; set; }
        public bool DysarthriaOrAphasia { get; set; }
        public bool AtrialFibrillationOnECG { get; set; }
        public bool InfarctionOnCT { get; set; }
        public bool PlateletOver400 { get; set; }
        public bool GlucoseOver15 { get; set; }
    }
}
