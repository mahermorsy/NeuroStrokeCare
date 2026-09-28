using System;

namespace NeuroStrokeCare.Core.Features.LabResults.Dtos
{
    // ملحوظة: RecordedById متسحوبة عمدًا، بتتحط من الكنترولر من actingUserId
    // كل نتائج التحاليل اختيارية (nullable) لأنها ممكن تتسجل تباعًا مش كلها مرة واحدة
    public class CreateLabResultsRequest
    {
        public Guid AdmissionId { get; set; }
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
    }
}
