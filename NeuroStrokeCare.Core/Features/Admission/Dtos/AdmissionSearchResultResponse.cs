using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.Admission.Dtos
{
    // ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 1): the small, read-only shape
    // returned by GET /api/admission/search — just enough for a typeahead result row
    // (patient name, masked National ID, current ward/bed, admission state) without
    // exposing full Patient/Admission records or anything from a soft-deleted patient.
    public class AdmissionSearchResultResponse
    {
        public Guid AdmissionId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;

        // PHASE 11 (area 1/14): the real hospital-assigned identifier, now that it exists -
        // shown unmasked (it's not a sensitive national ID), and the primary operational
        // identifier clinical staff should use day to day. May be null if not yet assigned.
        public string? HospitalNumber { get; set; }

        // Masked except for the last 4 characters (e.g. "•••••••1234"). Shown only where the
        // existing masking rules call for it (ordinary search/list/context) - the formal
        // clinical report keeps NationalId unmasked, per the existing privacy decision this
        // phase preserves rather than changes.
        public string? NationalIdMasked { get; set; }

        public PatientStatus Status { get; set; }
        public bool IsOpen { get; set; }
        public string? WardCode { get; set; }
        public string? WardName { get; set; }
        public string? BedNumber { get; set; }
        public DateTime AdmissionTime { get; set; }
    }
}
