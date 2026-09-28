using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.Data.Entities
{
    public class NIHSSAssessment : BaseEntity   
    {
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int LOC { get; set; }
        public int LOCQuestions { get; set; }
        public int LOCCommands { get; set; }
        public int BestGaze { get; set; }
        public int Visual { get; set; }
        public int FacialPalsy { get; set; }
        public int MotorArmLeft { get; set; }
        public int MotorArmRight { get; set; }
        public int MotorLegLeft { get; set; }
        public int MotorLegRight { get; set; }
        public int LimbAtaxia { get; set; }
        public int Sensory { get; set; }
        public int BestLanguage { get; set; }
        public int Dysarthria { get; set; }
        public int Extinction { get; set; }

        // Computed
        public int TotalScore =>
            LOC + LOCQuestions + LOCCommands + BestGaze + Visual +
            FacialPalsy + MotorArmLeft + MotorArmRight + MotorLegLeft +
            MotorLegRight + LimbAtaxia + Sensory + BestLanguage +
            Dysarthria + Extinction;

        public string Severity => TotalScore switch
        {
            0 => "None",
            <= 4 => "Minor",
            <= 15 => "Moderate",
            <= 20 => "ModSevere",
            _ => "Severe"
        };

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}
