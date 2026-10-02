using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.Admission.Dtos
{
    public class AdmissionResponse
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public DateTime AdmissionTime { get; set; }
        public PatientStatus Status { get; set; }
        public StrokeType? StrokeType { get; set; }
        public DateTime? StrokeTypeSetAt { get; set; }
        public Guid? BedId { get; set; }
        public DateTime? DischargeTime { get; set; }
        public string? DischargeNotes { get; set; }
        public Guid AdmittedById { get; set; }
        public Guid? StrokeTypeSetById { get; set; }
        public bool CTDone { get; set; }
        public bool MRIDone { get; set; }
        public bool CTADone { get; set; }
        public string? CTFindings { get; set; }
        public string? MRIFindings { get; set; }
        public string? CTAFindings { get; set; }
        public DateTime? ThrombolysisGivenAt { get; set; }
        public string? ThrombolysisDrug { get; set; }
        public decimal? ThrombolysisDoseMg { get; set; }
        public Guid? ThrombolysisRecordedById { get; set; }

        // FINAL RELEASE-CANDIDATE PASS (section 4): callers that want real end-to-end
        // optimistic-concurrency protection (not just the narrower same-request race window the
        // backend already protects on its own) should echo this value back on their next
        // PUT/PATCH for this admission - see UpdateAdmissionRequest.RowVersion.
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
