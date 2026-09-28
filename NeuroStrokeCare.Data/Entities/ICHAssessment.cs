using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.Data.Entities
{
    // نزيفي فقط
    public class ICHAssessment : BaseEntity
    {
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        public int GCSScore { get; set; }          // 3-15
        public decimal ICHVolumeMl { get; set; }   // حجم النزيف
        public bool InfratentorialOrigin { get; set; }
        public bool IVHPresent { get; set; }       // نزيف داخل البطين
        public int AgeScore { get; set; }          // 0 or 1 (≥80 = 1)

        // ICH Score 0-6
        public int TotalScore =>
            (GCSScore <= 4 ? 2 : GCSScore <= 12 ? 1 : 0) +
            (ICHVolumeMl >= 30 ? 1 : 0) +
            (InfratentorialOrigin ? 1 : 0) +
            (IVHPresent ? 1 : 0) +
            AgeScore;

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}
