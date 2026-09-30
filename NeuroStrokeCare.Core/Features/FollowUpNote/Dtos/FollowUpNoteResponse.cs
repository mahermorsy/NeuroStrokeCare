using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Core.Features.FollowUpNote.Dtos
{
    public class FollowUpNoteResponse
    {
        public Guid Id { get; set; }
        public Guid AdmissionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public FollowUpNoteType NoteType { get; set; }
        public string? AuthorRole { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
