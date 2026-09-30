using System;

namespace NeuroStrokeCare.Data.Enums
{
    // General: ملاحظة متابعة يومية عادية (دكتور أو تمريض)
    // NewFinding: حالة/نتيجة فحص جديدة تستحق الانتباه - "أضاف حالة جديدة بناءً على الفحص"
    public enum FollowUpNoteType
    {
        General = 1,
        NewFinding = 2
    }
}
