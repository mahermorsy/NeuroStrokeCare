using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.Entities.Assessments;

namespace NeuroStrokeCare.Data.Entities
{
    // NeuroFlow.Core/Entities/Admission.cs
    public class Admission : BaseEntity
    {
        public Guid PatientId { get; set; }
        public DateTime AdmissionTime { get; set; }
        public PatientStatus Status { get; set; }
        public StrokeType? StrokeType { get; set; }
        public DateTime? StrokeTypeSetAt { get; set; }
        public Guid? BedId { get; set; }
        public DateTime? DischargeTime { get; set; }
        public string? DischargeNotes { get; set; }

        // Identity Guid IDs
        public Guid AdmittedById { get; set; }
        public Guid? StrokeTypeSetById { get; set; }
        public bool CTDone { get; set; }
        public bool MRIDone { get; set; }
        public bool CTADone { get; set; }
        public string? CTFindings { get; set; }   // النتيجة نصية
        public string? MRIFindings { get; set; }
        public string? CTAFindings { get; set; }
        // Navigation
        public Patient Patient { get; set; }
        public ApplicationUser AdmittedBy { get; set; }
        public ApplicationUser? StrokeTypeSetBy { get; set; }
        public Bed? Bed { get; set; }
        public DoorTiming DoorTiming { get; set; }
        public NIHSSAssessment NIHSSAssessment { get; set; }
        public ASPECTSAssessment? ASPECTSAssessment { get; set; }
        public ICHAssessment? ICHAssessment { get; set; }
        public CanadianTIAAssessment? CanadianTIAAssessment { get; set; }
        public LabResults? LabResults { get; set; }
        public BradenAssessment? BradenAssessment { get; set; }
        public GCSAssessment? GCSAssessment { get; set; }
        public GUSSAssessment? GUSSAssessment { get; set; }
        public MorseAssessment? MorseAssessment { get; set; }
    }
}
