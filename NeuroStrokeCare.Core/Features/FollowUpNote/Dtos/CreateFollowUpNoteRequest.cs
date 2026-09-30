using System;
using System.ComponentModel.DataAnnotations;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.FollowUpNote.Dtos
{
    public class CreateFollowUpNoteRequest
    {
        [Required(ErrorMessage = "معرف الإدخال مطلوب")]
        public Guid AdmissionId { get; set; }

        [Required(ErrorMessage = "نص الملاحظة مطلوب")]
        [MaxLength(2000, ErrorMessage = "الملاحظة طويلة جدًا (٢٠٠٠ حرف كحد أقصى)")]
        public string Content { get; set; } = string.Empty;

        public FollowUpNoteType NoteType { get; set; } = FollowUpNoteType.General;

        public string? AuthorRole { get; set; }
    }
}
