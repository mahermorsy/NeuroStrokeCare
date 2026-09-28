using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Data.AuditLogModel
{
    public class AuditLog
    {
        public long Id { get; set; }
        public string EntityName { get; set; } = string.Empty;   // اسم الكيان (Product, Order, ...)
        public Guid EntityId { get; set; }                        // مفتاح السطر المتأثر
        public string Action { get; set; } = string.Empty;       // Created / Updated / StatusChanged / Deleted
        public Guid UserId { get; set; }                          // مين عمل العملية (0 = نظام/مجهول)
        public string? Details { get; set; }                     // تفاصيل اختيارية (الحالة الجديدة مثلًا)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
