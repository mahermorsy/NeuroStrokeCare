using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.Data.Entities
{
    public class LabResults : BaseEntity
    {
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

        public bool INRAlert => INR.HasValue && INR > 1.7m;
        public bool GlucoseAlert => GlucoseMmol.HasValue && GlucoseMmol < 2.8m || GlucoseMmol > 15m;
        public bool PlateletsAlert => Platelets.HasValue && Platelets < 100;

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser RecordedBy { get; set; }
    }
}
