using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.Data.Entities
{
    // NeuroFlow.Core/Entities/DoorTiming.cs
    // NeuroStrokeCare.Core/Entities/DoorTiming.cs
    public class DoorTiming : BaseEntity
    {
        public Guid AdmissionId { get; set; }

        // التوقيتات
        public DateTime Er_StrokeArrival { get; set; }  // وصول الطوارئ
        public DateTime? DoorToCT { get; set; }          // اتبعت للأشعة
        public DateTime? DoorToNeedle { get; set; }      // اتعطت الحقنة
        public DateTime? DoorToGroin { get; set; }       // بدأت القسطرة

        // مين سجل كل توقيت
        public string? CTRecordedById { get; set; }
        public string? NeedleRecordedById { get; set; }
        public string? GroinRecordedById { get; set; }

        // حسابات تلقائية بالدقايق
        public double? MinutesToCT =>
            DoorToCT.HasValue
                ? (DoorToCT.Value - Er_StrokeArrival).TotalMinutes
                : null;

        public double? MinutesToNeedle =>
            DoorToNeedle.HasValue
                ? (DoorToNeedle.Value - Er_StrokeArrival).TotalMinutes
                : null;

        public double? MinutesToGroin =>
            DoorToGroin.HasValue
                ? (DoorToGroin.Value - Er_StrokeArrival).TotalMinutes
                : null;

        // تنبيهات AHA
        public bool CTDelayed =>
            MinutesToCT.HasValue && MinutesToCT > 25;

        public bool NeedleDelayed =>
            MinutesToNeedle.HasValue && MinutesToNeedle > 60;

        // Navigation
        public Admission Admission { get; set; }
    }
}
