using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Data.Enums
{
    public enum PatientStatus
    {
        // 1️⃣ الطوارئ
        Emergency = 1,

        // 2️⃣ الأشعة
        SentToImaging = 2,        // اتبعت للأشعة
        ImagingCompleted = 3,     // رجع من الأشعة + تحديد النوع

        // 3️⃣ العلاج (إقفاري فقط)
        SentToNeedle = 4,         // اتبعت للحقنة
        NeedleCompleted = 5,      // خلصت الحقنة

        // 4️⃣ القسطرة (إقفاري فقط)
        SentToGroin = 6,          // اتبعت للقسطرة
        GroinCompleted = 7,       // خلصت القسطرة

        // 5️⃣ التنقل بين الأجنحة
        AdmittedToER = 8,         // رجع للطوارئ
        AdmittedToWard = 9,       // جناح (ذكور/إناث)
        AdmittedToNICU = 10,      // نيورو ICU
        AdmittedToIMC = 11,       // رعاية متوسطة

        // 6️⃣ الخروج
        Discharged = 12,          // خروج طبيعي
        TransferredOut = 13       // تحويل لمستشفى تاني
    }
}
