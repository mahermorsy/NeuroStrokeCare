using System;

namespace NeuroStrokeCare.Core.Features.LabResults.Dtos
{
    public class LabResultsResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public Guid RecordedById { get; set; }
        public DateTime RecordedAt { get; set; }

        public decimal? GlucoseMmol { get; set; }
        public decimal? INR { get; set; }
        public decimal? PT { get; set; }
        public decimal? Platelets { get; set; }
        public decimal? Sodium { get; set; }
        public decimal? Potassium { get; set; }
        public decimal? Creatinine { get; set; }
        public decimal? Hemoglobin { get; set; }
        public decimal? LDL { get; set; }
        public decimal? HbA1c { get; set; }
        public decimal? aPTT { get; set; }
        public decimal? ALT { get; set; }
        public decimal? AST { get; set; }

        public bool? ECGAtrialFibrillation { get; set; }

        public bool INRAlert { get; set; }
        public bool GlucoseAlert { get; set; }
        public bool PlateletsAlert { get; set; }
    }
}
