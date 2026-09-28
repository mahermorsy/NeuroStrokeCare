using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.Data.Entities
{
    public class CanadianTIAAssessment : BaseEntity
    {
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

        public int TotalScore =>
            (FirstTIAInLifetime ? 2 : 0) +
            (SymptomsOver10Min ? 2 : 0) +
            (HistoryCarotidStenosis ? 2 : 0) +
            (OnAntiplateletTherapy ? 3 : 0) +
            (GaitDisturbance ? 1 : 0) +
            (UnilateralWeakness ? 1 : 0) +
            (HistoryOfVertigo ? -3 : 0) +
            (DiastolicBPOver110 ? 3 : 0) +
            (DysarthriaOrAphasia ? 1 : 0) +
            (AtrialFibrillationOnECG ? 2 : 0) +
            (InfarctionOnCT ? 1 : 0) +
            (PlateletOver400 ? 2 : 0) +
            (GlucoseOver15 ? 3 : 0);

        public string RiskLevel => TotalScore switch
        {
            <= 3 => "Low",
            <= 8 => "Intermediate",
            _ => "High"
        };

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}
