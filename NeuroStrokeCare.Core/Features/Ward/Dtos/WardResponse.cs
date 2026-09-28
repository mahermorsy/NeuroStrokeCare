using System;

namespace NeuroStrokeCare.Core.Features.Ward.Dtos
{
    public class WardResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int TotalBeds { get; set; }
    }
}
