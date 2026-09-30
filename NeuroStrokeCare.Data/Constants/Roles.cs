namespace NeuroStrokeCare.Data.Constants
{
    // ثوابت أسماء الأدوار - نفس القيم اللي بتتزرع في DataSeeder وبتترجع في AuthResponse.Role.
    // بنستخدمها في [Authorize(Roles = ...)] بدل ما نكرر الأسماء كنص حر في كل كنترولر.
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Consultant = "Consultant";           // استشاري
        public const string Registrar = "Registrar";             // نائب
        public const string Resident = "Resident";               // مقيم
        public const string Nurse = "Nurse";                     // تمريض
        public const string NursingSupervisor = "NursingSupervisor"; // مشرف تمريض

        // الدرجات العلمية الثلاث بتاعة الأطباء - نقل المريض/الحالة وتقارير الفحص (NIHSS, ASPECTS, ICH, Canadian TIA, GCS)
        public const string AnyDoctor = Admin + "," + Consultant + "," + Registrar + "," + Resident;

        // التمريض ومشرف التمريض - اختبارات الرعاية التمريضية (Braden, Morse, GUSS)
        public const string AnyNurse = Admin + "," + Nurse + "," + NursingSupervisor;
            public const string AnyClinical = AnyDoctor + "," + Nurse + "," + NursingSupervisor;
    }
}
