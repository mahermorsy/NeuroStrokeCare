using System;

namespace NeuroStrokeCare.Core.Features.NIHSSAssessment.Dtos
{
    public class NIHSSAssessmentResponse
    {
        public Guid Id { get; set; }
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

        public int TotalScore { get; set; }
        public string Severity { get; set; } = string.Empty;
    }
}
