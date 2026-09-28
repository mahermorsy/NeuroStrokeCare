using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.DoorTiming.Dtos
{
    public class UpdateDoorTimingRequest
    {
        [Required(ErrorMessage = "معرف السجل مطلوب")]
        public Guid Id { get; set; }

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
