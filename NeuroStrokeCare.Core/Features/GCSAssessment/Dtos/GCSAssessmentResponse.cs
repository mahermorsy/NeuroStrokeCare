using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.GCSAssessment.Dtos
{
    public class GCSAssessmentResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int EyeResponse { get; set; }
        public int VerbalResponse { get; set; }
        public int MotorResponse { get; set; }

        public int TotalScore { get; set; }
        public GCSSeverity Severity { get; set; }
    }
}
