using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.Data.Entities
{
    // إقفاري فقط - بعد CT
    public class ASPECTSAssessment : BaseEntity
    {
        public Guid AdmissionId { get; set; }
        public Guid AssessedById { get; set; }
        public DateTime AssessedAt { get; set; }

        // المناطق العشر (كل منطقة = 1 لو سليمة، 0 لو فيها تغير)
        public int C { get; set; }   // Caudate
        public int P { get; set; }   // Putamen (Lentiform)
        public int IC { get; set; }  // Internal Capsule
        public int I { get; set; }   // Insular Ribbon
        public int M1 { get; set; }
        public int M2 { get; set; }
        public int M3 { get; set; }
        public int M4 { get; set; }
        public int M5 { get; set; }
        public int M6 { get; set; }

        // المجموع 0-10
        public int TotalScore => C + P + IC + I + M1 + M2 + M3 + M4 + M5 + M6;

        // Navigation
        public Admission Admission { get; set; }
        public ApplicationUser AssessedBy { get; set; }
    }
}
