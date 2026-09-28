using System;

namespace NeuroStrokeCare.Core.Features.ICHAssessment.Dtos
{
    public class ICHAssessmentResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int GCSScore { get; set; }
        public decimal ICHVolumeMl { get; set; }
        public bool InfratentorialOrigin { get; set; }
        public bool IVHPresent { get; set; }
        public int AgeScore { get; set; }

        public int TotalScore { get; set; }
    }
}
