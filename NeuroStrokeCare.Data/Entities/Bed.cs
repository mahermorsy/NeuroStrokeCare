using NeuroStrokeCare.Data.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Data.Entities
{
    // NeuroFlow.Core/Entities/Bed.cs
    public class Bed : BaseEntity
    {
        public Guid WardId { get; set; }
        public string BedNumber { get; set; }  // FW-01, NICU-03
        public BedStatus Status { get; set; }

        // Navigation
        public Ward Ward { get; set; }
        public Admission? CurrentAdmission { get; set; }
    }
}
