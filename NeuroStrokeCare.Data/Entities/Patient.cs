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
