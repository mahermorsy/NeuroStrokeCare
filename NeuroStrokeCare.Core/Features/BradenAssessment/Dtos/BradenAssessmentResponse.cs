using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.BradenAssessment.Dtos
{
    public class BradenAssessmentResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int SensoryPerception { get; set; }
        public int Moisture { get; set; }
        public int Activity { get; set; }
        public int Mobility { get; set; }
        public int Nutrition { get; set; }
        public int FrictionShear { get; set; }

        public int TotalScore { get; set; }
        public BradenRiskLevel RiskLevel { get; set; }
    }
}
