using System;

namespace NeuroStrokeCare.Core.Features.CanadianTIAAssessment.Dtos
{
    public class CanadianTIAAssessmentResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
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

        public int TotalScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
    }
}
