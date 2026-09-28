using System;
using System.ComponentModel.DataAnnotations;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.Bed.Dtos
{
    public class UpdateBedRequest
    {
        [Required(ErrorMessage = "معرف السرير مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "الجناح مطلوب")]
        public Guid WardId { get; set; }

        [Required(ErrorMessage = "رقم السرير مطلوب")]
        [StringLength(20, ErrorMessage = "رقم السرير لازم يكون أقل من 20 حرف")]
        public string BedNumber { get; set; } = string.Empty;

        public BedStatus Status { get; set; }
    }
}
