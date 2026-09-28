using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Data.Entities
{
    // NeuroFlow.Core/Entities/Ward.cs
    public class Ward : BaseEntity
    {
        public string Code { get; set; }   // FW, MW, NICU, IMC, ER
        public string Name { get; set; }
        public int TotalBeds { get; set; }

        // Navigation
        public ICollection<Bed> Beds { get; set; }
    }
}
