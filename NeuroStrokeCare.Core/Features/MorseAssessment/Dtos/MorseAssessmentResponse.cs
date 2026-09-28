using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.MorseAssessment.Dtos
{
    public class MorseAssessmentResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public bool HistoryOfFalling { get; set; }
        public bool SecondaryDiagnosis { get; set; }
        public int AmbulatoryAid { get; set; }
        public bool IVOrHeparinLock { get; set; }
        public int Gait { get; set; }
        public bool MentalStatus { get; set; }

        public int TotalScore { get; set; }
        public MorseRiskLevel RiskLevel { get; set; }
    }
}
