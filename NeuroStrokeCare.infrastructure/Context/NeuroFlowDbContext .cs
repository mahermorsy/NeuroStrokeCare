// ===== NeuroStrokeCare.Data/NeuroFlowDbContext.cs =====
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NeuroStrokeCare.Data.AuditLogModel;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Entities.Assessments;
using NeuroStrokeCare.Data.UserApplication;

namespace NeuroStrokeCare.infrastructure.Context
{
    public class NeuroFlowDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public NeuroFlowDbContext(DbContextOptions<NeuroFlowDbContext> options) : base(options) { }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Admission> Admissions => Set<Admission>();
        public DbSet<Ward> Wards => Set<Ward>();
        public DbSet<Bed> Beds => Set<Bed>();
        public DbSet<DoorTiming> DoorTimings => Set<DoorTiming>();
        public DbSet<NIHSSAssessment> NIHSSAssessments => Set<NIHSSAssessment>();
        public DbSet<ASPECTSAssessment> ASPECTSAssessments => Set<ASPECTSAssessment>();
        public DbSet<ICHAssessment> ICHAssessments => Set<ICHAssessment>();
        public DbSet<CanadianTIAAssessment> CanadianTIAAssessments => Set<CanadianTIAAssessment>();
        public DbSet<LabResults> LabResultsSet => Set<LabResults>();
        public DbSet<BradenAssessment> BradenAssessments => Set<BradenAssessment>();
        public DbSet<GCSAssessment> GCSAssessments => Set<GCSAssessment>();
        public DbSet<GUSSAssessment> GUSSAssessments => Set<GUSSAssessment>();
        public DbSet<MorseAssessment> MorseAssessments => Set<MorseAssessment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<NeuroStrokeCare.Data.Entities.FollowUpNote> FollowUpNotes => Set<NeuroStrokeCare.Data.Entities.FollowUpNote>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // لازم تتنادى الأول عشان تظبط جداول Identity (AspNetUsers, AspNetRoles...)

            // Admission 1-to-1 مع كل تقييم
            builder.Entity<Admission>()
                .HasOne(a => a.DoorTiming)
                .WithOne(d => d.Admission)
                .HasForeignKey<DoorTiming>(d => d.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.NIHSSAssessment)
                .WithOne(n => n.Admission)
                .HasForeignKey<NIHSSAssessment>(n => n.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.ASPECTSAssessment)
                .WithOne(x => x.Admission)
                .HasForeignKey<ASPECTSAssessment>(x => x.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.ICHAssessment)
                .WithOne(x => x.Admission)
                .HasForeignKey<ICHAssessment>(x => x.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.CanadianTIAAssessment)
                .WithOne(x => x.Admission)
                .HasForeignKey<CanadianTIAAssessment>(x => x.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.LabResults)
                .WithOne(x => x.Admission)
                .HasForeignKey<LabResults>(x => x.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.BradenAssessment)
                .WithOne(x => x.Admission)
                .HasForeignKey<BradenAssessment>(x => x.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.GCSAssessment)
                .WithOne(x => x.Admission)
                .HasForeignKey<GCSAssessment>(x => x.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.GUSSAssessment)
                .WithOne(x => x.Admission)
                .HasForeignKey<GUSSAssessment>(x => x.AdmissionId);

            builder.Entity<Admission>()
                .HasOne(a => a.MorseAssessment)
                .WithOne(x => x.Admission)
                .HasForeignKey<MorseAssessment>(x => x.AdmissionId);

            builder.Entity<Bed>()
                .HasOne(b => b.CurrentAdmission)
                .WithOne(a => a.Bed)
                .HasForeignKey<Admission>(a => a.BedId);
            // Patient.CreatedByUser بترتبط بالـ CreatedBy الموروثة من BaseEntity (بعد ما اتشالت CreatedById المكررة)
            // (سياسة الحذف بتاعتها بتتحدد تحت مع كل علاقات ApplicationUser الطبية سوا)
            builder.Entity<Patient>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy);

            // منع الـ Cascade نهائيًا على أي بيانات طبية (تقارير/تاريخ مرضى) عشان محدش يقدر يمسحها
            // بحذف حساب اليوزر اللي سجلها. الاستثناء الوحيد هو جداول Identity الداخلية نفسها
            // (AspNetUserRoles/Claims/Logins/Tokens) اللي المفروض تتنضف تلقائيًا لما اليوزر يتحذف.
            foreach (var fk in builder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetForeignKeys())
                         .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade
                                   && fk.DeclaringEntityType.ClrType.Namespace != "Microsoft.AspNetCore.Identity"))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Unique indexes
            builder.Entity<Patient>().HasIndex(p => p.NationalId).IsUnique(false);
            builder.Entity<Ward>().HasIndex(w => w.Code).IsUnique();
            builder.Entity<Bed>().HasIndex(b => b.BedNumber).IsUnique();

            // Enum -> string (أوضح في قاعدة البيانات)
            builder.Entity<Bed>().Property(b => b.Status).HasConversion<string>();
            builder.Entity<Admission>().Property(a => a.Status).HasConversion<string>();
            builder.Entity<Admission>().Property(a => a.StrokeType).HasConversion<string>();
        }
    }
}