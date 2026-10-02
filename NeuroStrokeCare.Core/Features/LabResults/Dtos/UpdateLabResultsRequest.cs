using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.LabResults.Dtos
{
    public class UpdateLabResultsRequest
    {
        [Required(ErrorMessage = "معرف السجل مطلوب")]
        public Guid Id { get; set; }

        public Guid AdmissionId { get; set; }
        public DateTime RecordedAt { get; set; }

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
