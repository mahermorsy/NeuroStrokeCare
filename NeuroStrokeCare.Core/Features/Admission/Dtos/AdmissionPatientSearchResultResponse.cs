using System;

namespace NeuroStrokeCare.Core.Features.Admission.Dtos
{
    public class AdmissionPatientSearchResultResponse
    {
        public Guid PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? HospitalNumber { get; set; }
        public string? NationalIdMasked { get; set; }
    }
}
