using System;

namespace NeuroStrokeCare.Core.Features.Patient.Dtos
{
    public class PatientResponse
    {
        public Guid Id { get; set; }
        public string? NationalId { get; set; }
        public string? HospitalNumber { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public decimal WeightKg { get; set; }
        public string ChiefComplaint { get; set; }
    }
}
