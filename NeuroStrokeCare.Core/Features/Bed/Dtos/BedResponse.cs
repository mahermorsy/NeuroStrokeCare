using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.Bed.Dtos
{
    public class BedResponse
    {
        public Guid Id { get; set; }
        public Guid WardId { get; set; }
        public string BedNumber { get; set; }
        public BedStatus Status { get; set; }
    }
}
