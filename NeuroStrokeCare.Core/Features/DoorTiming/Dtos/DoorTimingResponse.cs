using System;

namespace NeuroStrokeCare.Core.Features.DoorTiming.Dtos
{
    public class DoorTimingResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }

        public DateTime Er_StrokeArrival { get; set; }
        public DateTime? DoorToCT { get; set; }
        public DateTime? DoorToNeedle { get; set; }
        public DateTime? DoorToGroin { get; set; }

        public string? CTRecordedById { get; set; }
        public string? NeedleRecordedById { get; set; }
        public string? GroinRecordedById { get; set; }

        // قيم محسوبة تلقائيًا من الـ Entity، بتتقرا بس
        public double? MinutesToCT { get; set; }
        public double? MinutesToNeedle { get; set; }
        public double? MinutesToGroin { get; set; }
        public bool CTDelayed { get; set; }
        public bool NeedleDelayed { get; set; }
    }
}
