using System;
using NeuroStrokeCare.Data.Enums;

namespace NeuroStrokeCare.Data.Entities
{
    // سجل متابعة نصي بسيط مربوط بإدخال معين - بيستخدمه الدكاترة والتمريض مع بعض.
    // متعمد إنه immutable (مفيش Update) زي أي سجل طبي: لو غلط بيتعمله Cancel عن طريق
    // ChangeStatus (Soft Delete) مش تعديل، عشان يفضل تاريخ حقيقي لأي حد كتب ايه وامتى.
    public class FollowUpNote : BaseEntity
    {
        public Guid AdmissionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public FollowUpNoteType NoteType { get; set; } = FollowUpNoteType.General;

        // نسخة من دور الكاتب وقت الكتابة (Consultant/Nurse/..) - بتتسجل من الفرونت بناءً على
        // اليوزر المسجل دخوله، عشان العرض يفضل صح حتى لو دوره اتغير بعدين
        public string? AuthorRole { get; set; }

        public Admission Admission { get; set; } = null!;
    }
}
