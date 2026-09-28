using System;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Data.Entities.Assessments
{
    public class MorseAssessment : BaseEntity
    {
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public bool HistoryOfFalling { get; set; }   // لا=0 / نعم=25
        public bool SecondaryDiagnosis { get; set; } // لا=0 / نعم=15
        public int AmbulatoryAid { get; set; }       // 0=مفيش/سرير/ممرضة، 15=عكاز/إطار مشي، 30=يتمسك بالأثاث
        public bool IVOrHeparinLock { get; set; }    // لا=0 / نعم=20
        public int Gait { get; set; }                // 0=طبيعي/سرير/كرسي متحرك، 10=ضعيف، 20=معتل
        public bool MentalStatus { get; set; }        // لا=0 (مدرك لحدوده) / نعم=15 (ناسي حدوده)

        // المجموع 0-125
        public int TotalScore =>
            (HistoryOfFalling ? 25 : 0) +
            (SecondaryDiagnosis ? 15 : 0) +
            AmbulatoryAid +
            (IVOrHeparinLock ? 20 : 0) +
            Gait +
            (MentalStatus ? 15 : 0);

        public MorseRiskLevel RiskLevel => TotalScore switch
        {
            <= 24 => MorseRiskLevel.LowRisk,
            <= 44 => MorseRiskLevel.ModerateRisk,
            _ => MorseRiskLevel.HighRisk
        };

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}