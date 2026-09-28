using System;
using System.ComponentModel.DataAnnotations;

namespace NeuroStrokeCare.Core.Features.Ward.Dtos
{
    public class UpdateWardRequest
    {
        [Required(ErrorMessage = "معرف الجناح مطلوب")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "كود الجناح مطلوب")]
        [StringLength(20, ErrorMessage = "كود الجناح لازم يكون أقل من 20 حرف")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم الجناح مطلوب")]
        [StringLength(100, ErrorMessage = "اسم الجناح لازم يكون أقل من 100 حرف")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 500, ErrorMessage = "عدد الأسرّة لازم يكون بين 1 و 500")]
        public int TotalBeds { get; set; }
    }
}
