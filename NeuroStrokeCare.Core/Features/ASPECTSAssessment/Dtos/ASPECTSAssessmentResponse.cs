using System;

namespace NeuroStrokeCare.Core.Features.ASPECTSAssessment.Dtos
{
    public class ASPECTSAssessmentResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int C { get; set; }
        public int P { get; set; }
        public int IC { get; set; }
        public int I { get; set; }
        public int M1 { get; set; }
        public int M2 { get; set; }
        public int M3 { get; set; }
        public int M4 { get; set; }
        public int M5 { get; set; }
        public int M6 { get; set; }

        public int TotalScore { get; set; }
    }
}
