using System;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Data.Entities.Assessments
{
    public class GCSAssessment : BaseEntity
    {
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int EyeResponse { get; set; }     // 1-4
        public int VerbalResponse { get; set; }  // 1-5
        public int MotorResponse { get; set; }   // 1-6

        // المجموع 3-15
        public int TotalScore => EyeResponse + VerbalResponse + MotorResponse;

        public GCSSeverity Severity => TotalScore switch
        {
            >= 13 => GCSSeverity.Mild,      // 13-15
            >= 9 => GCSSeverity.Moderate,   // 9-12
            _ => GCSSeverity.Severe          // 3-8
        };

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}