using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.GUSSAssessment.Dtos
{
    public class GUSSAssessmentResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public bool Vigilance { get; set; }
        public bool VoluntaryCough { get; set; }
        public bool SalivaSwallowSuccessful { get; set; }
        public bool NoDrooling { get; set; }
        public bool NoVoiceChange { get; set; }
        public int IndirectScore { get; set; }

        public int? SemisolidScore { get; set; }
        public int? LiquidScore { get; set; }
        public int? SolidScore { get; set; }

        public int TotalScore { get; set; }
        public GUSSSeverity Severity { get; set; }
    }
}
