using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.Data.Entities
{
    // NeuroFlow.Core/Entities/Patient.cs
    public class Patient : BaseEntity
    {

        public string? NationalId { get; set; }

        // PHASE 11 (area 1): internal hospital/medical-record identifier - the primary
        // day-to-day search key for clinical staff, deliberately independent from NationalId
        // (never derived from it). Nullable - existing patients stay null until an Admin/
        // clinical user explicitly assigns one; nothing backfills this automatically. Unique
        // when present (see NeuroFlowDbContext's filtered unique index + the explicit
        // pre-check in PatientController).
        public string? HospitalNumber { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public decimal WeightKg { get; set; }
        public string ChiefComplaint { get; set; }


        // Navigation
        public ApplicationUser CreatedByUser { get; set; }
        public ICollection<Admission> Admissions { get; set; }
    }
}
