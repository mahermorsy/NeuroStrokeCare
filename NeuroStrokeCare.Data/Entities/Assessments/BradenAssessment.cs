using System;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Data.Entities.Assessments
{
    public class BradenAssessment : BaseEntity
    {
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int SensoryPerception { get; set; } // 1-4
        public int Moisture { get; set; }          // 1-4
        public int Activity { get; set; }          // 1-4
        public int Mobility { get; set; }          // 1-4
        public int Nutrition { get; set; }         // 1-4
        public int FrictionShear { get; set; }     // 1-3

        // المجموع 6-23
        public int TotalScore =>
            SensoryPerception + Moisture + Activity + Mobility + Nutrition + FrictionShear;

        public BradenRiskLevel RiskLevel => TotalScore switch
        {
            >= 19 => BradenRiskLevel.NoRisk,        // 19-23
            >= 15 => BradenRiskLevel.MildRisk,      // 15-18
            >= 13 => BradenRiskLevel.ModerateRisk,  // 13-14
            >= 10 => BradenRiskLevel.HighRisk,      // 10-12
            _ => BradenRiskLevel.VeryHighRisk        // 6-9
        };

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}