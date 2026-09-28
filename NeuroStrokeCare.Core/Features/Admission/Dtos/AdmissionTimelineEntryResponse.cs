namespace NeuroStrokeCare.Core.Features.Admission.Dtos
{
    // صف واحد في رحلة المريض من لحظة الدخول (AdmissionTime) لحد كل تغيير حالة (StatusChanged)
    // مسجل في الـ AuditLog - مبني فوق الجدول الموجود، مفيش تعديل في قاعدة البيانات.
    public class AdmissionTimelineEntryResponse
    {
        public DateTime Timestamp { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public int? Status { get; set; }
        public Guid? ChangedById { get; set; }
        public int? MinutesSincePrevious { get; set; }
        public int MinutesSinceArrival { get; set; }
    }
}
