using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.LabResults.Dtos
{
    // ملحوظة: RecordedById متسحوبة عمدًا، بتتحط من الكنترولر من actingUserId
    // كل نتائج التحاليل اختيارية (nullable) لأنها ممكن تتسجل تباعًا مش كلها مرة واحدة
    public class CreateLabResultsRequest
    {
        public Guid AdmissionId { get; set; }
        public DateTime RecordedAt { get; set; }

        // FINAL RELEASE-CANDIDATE PASS (section 7, "Lab validation"): CONFIRMED BUG, FIXED.
        // These 13 fields had NO server-side validation at all (not even a basic non-negative
        // check) - a negative glucose, a negative platelet count, etc. would all be accepted
        // silently. Only a lower bound (0) is enforced below, deliberately with no invented
        // upper clinical ceiling - the spec explicitly warns against inventing new clinical
        // ranges from general knowledge, and this project's own existing computed alert
        // properties (LabResults.INRAlert/GlucoseAlert/PlateletsAlert) are ALERT thresholds
        // ("flag this as concerning"), not validity ceilings ("reject this as impossible") -
        // reusing them as hard upper bounds here would incorrectly reject a real, if alarming,
        // reading (e.g. a genuinely critical INR). "Negative" is the one bound that is
        // impossible for every one of these fields regardless of clinical judgment.
        [Range(0, double.MaxValue, ErrorMessage = "GlucoseMmol لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? GlucoseMmol { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "INR لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? INR { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "PT لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? PT { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Platelets لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? Platelets { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Sodium لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? Sodium { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Potassium لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? Potassium { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Creatinine لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? Creatinine { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Hemoglobin لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? Hemoglobin { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "LDL لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? LDL { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "HbA1c لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? HbA1c { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "aPTT لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? aPTT { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "ALT لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? ALT { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "AST لازم يكون صفر أو رقم موجب - مفيش نتيجة تحليل سالبة")]
        public decimal? AST { get; set; }

        public bool? ECGAtrialFibrillation { get; set; }
    }
}
