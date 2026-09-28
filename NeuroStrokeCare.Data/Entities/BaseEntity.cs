using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Data.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public int CurrentState { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

    }
}
