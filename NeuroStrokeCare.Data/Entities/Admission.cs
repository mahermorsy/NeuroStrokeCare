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

        // Thrombolysis administration — timestamps the 24h antithrombotic lockout window.
        public DateTime? ThrombolysisGivenAt { get; set; }
        public string? ThrombolysisDrug { get; set; }   // "Alteplase" or "Tenecteplase"
        public decimal? ThrombolysisDoseMg { get; set; }
        public Guid? ThrombolysisRecordedById { get; set; }

        // FINAL RELEASE-CANDIDATE PASS (section 4): optimistic concurrency token. Not a SQL
        // Server-native computed `rowversion` column on purpose - NeuroFlowDbContext stamps a
        // fresh value itself in SaveChanges/SaveChangesAsync for every Added/Modified Admission,
        // so the exact same behavior works whether the real provider is SQL Server (production)
        // or SQLite (this test suite's CustomWebApplicationFactory) - see the DbContext comment
        // next to IsConcurrencyToken() for the full reasoning. Never set this by hand outside
        // that one place.
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

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
