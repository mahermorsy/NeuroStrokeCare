using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.DoorTiming.Dtos
{
    // ملحوظة: دي نسخة CRUD بسيطة مؤقتًا. تسجيل كل Milestone (CT / Needle / Groin) بشكل منفصل
    // هيتحول بعدين لعمليات (Commands) خاصة بيه، حسب الاتفاق على تأجيل العمليات الاستثنائية
    public class CreateDoorTimingRequest
    {
        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "وقت وصول الطوارئ مطلوب")]
        public DateTime Er_StrokeArrival { get; set; }

        public DateTime? DoorToCT { get; set; }
        public DateTime? DoorToNeedle { get; set; }
        public DateTime? DoorToGroin { get; set; }

        public string? CTRecordedById { get; set; }
        public string? NeedleRecordedById { get; set; }
        public string? GroinRecordedById { get; set; }
    }
}
