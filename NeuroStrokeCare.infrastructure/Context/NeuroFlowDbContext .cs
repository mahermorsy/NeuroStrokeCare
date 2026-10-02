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

            // PHASE 11 (area 1): HospitalNumber is the real uniqueness guarantee's last line
            // of defense (PatientController.Create/Update already check explicitly first, for
            // a clear error message) - filtered so multiple patients can still have
            // HospitalNumber = NULL (not yet assigned) without violating the index.
            //
            // HasMaxLength(30) matches the [StringLength(30)] on Create/UpdatePatientRequest -
            // this one is NOT optional the way NationalId's unbounded nvarchar(max) is: SQL
            // Server cannot build an index (unique or not) on an nvarchar(max) column at all,
            // so without an explicit bound here this index would fail outright. NationalId
            // only gets away with nvarchar(450) because EF Core picks that default itself for
            // an *indexed* unbounded string; being explicit here instead of relying on the
            // same implicit default.
            builder.Entity<Patient>().Property(p => p.HospitalNumber).HasMaxLength(30);
            builder.Entity<Patient>()
                .HasIndex(p => p.HospitalNumber)
                .IsUnique()
                .HasFilter("[HospitalNumber] IS NOT NULL");

            // مريض واحد مينفعش يكون عنده أكتر من إدخال "مفتوح" (نشط + لسه متخرّجش) في نفس
            // الوقت - ده الحارس الحقيقي على مستوى قاعدة البيانات نفسها (شبكة أمان أخيرة ضد أي
            // Race Condition)، فحص AnyAsync في AdmissionController.Create ده بس لرسالة أوضح
            // للمستخدم العادي. الفهرس ده بيستبدل الفهرس العادي (غير الفريد) اللي EF Core كان
            // عامله تلقائيًا على PatientId (كـ Foreign Key) - لسه بيصلح لنفس الغرض (فهرسة
            // البحث بـ PatientId) وبيضيف الفرادة فوقها كمان.
            builder.Entity<Admission>()
                .HasIndex(a => a.PatientId)
                .IsUnique()
                .HasFilter("[DischargeTime] IS NULL AND [CurrentState] = 1");

            // Enum -> string (أوضح في قاعدة البيانات)
            builder.Entity<Bed>().Property(b => b.Status).HasConversion<string>();
            builder.Entity<Admission>().Property(a => a.Status).HasConversion<string>();
            builder.Entity<Admission>().Property(a => a.StrokeType).HasConversion<string>();

            // FINAL RELEASE-CANDIDATE PASS (section 4): optimistic concurrency for Admission.
            // Deliberately IsConcurrencyToken(), NOT IsRowVersion(): IsRowVersion() asks the
            // relational provider for a store-generated, provider-native "always changes on
            // every write" column (SQL Server's `rowversion`/`timestamp` type) - SQLite (the
            // only provider actually exercised by NeuroStrokeCare.Tests/CustomWebApplicationFactory
            // in this sandbox, since no dotnet/SQL Server is reachable here - see
            // FINAL_RELEASE_CANDIDATE_REPORT.md) has no equivalent native column type, and
            // risking that mismatch breaking EVERY existing test's EnsureCreated() was not worth
            // it just to get SQL Server's auto-generation for free. IsConcurrencyToken() alone
            // is a plain "include this column in the WHERE clause, and in the optimistic-
            // concurrency check" marker with no provider-specific DDL requirement - it works
            // identically on both providers. The actual "always changes on every write" behavior
            // that IsRowVersion() would have given us for free is instead done by hand, once, in
            // SaveChanges/SaveChangesAsync below (ApplyAdmissionRowVersionStamps) - functionally
            // equivalent, just application-managed instead of database-managed, which keeps this
            // portable and testable without a real SQL Server connection.
            builder.Entity<Admission>().Property(a => a.RowVersion)
                .IsConcurrencyToken()
                .HasMaxLength(16)
                .IsRequired();

            // FINAL RELEASE-CANDIDATE PASS (section 3): explicit decimal precision/scale.
            // Before this, NONE of these columns had any precision configured, so EF Core's
            // own default convention silently applied decimal(18,2) to every one of them -
            // which happens to round-trip correctly for most values here but was never an
            // intentional, reviewed choice, and a provider/version change could alter that
            // default out from under the schema without anyone noticing. Values below are
            // chosen to comfortably cover each field's real clinical range with a safety
            // margin, not to encode new clinical limits - actual validation of in-range
            // values stays in LabResults/Admission validation logic, not here. Applying this
            // requires a migration (see migration checklist in the final report) - the model
            // is being made correct here first, without running `dotnet ef` in this pass.

            // Body weight used directly in weight-based dose calculators (thrombolysis,
            // seizure protocols) - two decimal places is already finer than any real scale
            // reads, 999.99 kg is a generous upper bound.
            builder.Entity<Patient>().Property(p => p.WeightKg).HasPrecision(5, 2);

            // Thrombolysis dose in mg - calculated from weight (e.g. 0.9 mg/kg), so two
            // decimal places preserves the calculator's output without rounding it away;
            // 9999.99 mg is far beyond any real single dose.
            builder.Entity<Admission>().Property(a => a.ThrombolysisDoseMg).HasPrecision(6, 2);

            // ICH volume in mL (ABC/2 estimate) - two decimal places matches how the value is
            // actually computed/entered; 9999.99 mL is a generous upper bound.
            builder.Entity<ICHAssessment>().Property(i => i.ICHVolumeMl).HasPrecision(6, 2);

            // LabResults: each pair chosen per the field's real unit/range, with headroom.
            builder.Entity<LabResults>().Property(l => l.GlucoseMmol).HasPrecision(5, 2);   // mmol/L
            builder.Entity<LabResults>().Property(l => l.INR).HasPrecision(5, 2);            // ratio, unitless
            builder.Entity<LabResults>().Property(l => l.PT).HasPrecision(6, 2);             // seconds
            builder.Entity<LabResults>().Property(l => l.Platelets).HasPrecision(9, 2);       // count (can be reported as raw count, e.g. ~250000)
            builder.Entity<LabResults>().Property(l => l.Sodium).HasPrecision(6, 2);          // mmol/L
            builder.Entity<LabResults>().Property(l => l.Potassium).HasPrecision(5, 2);       // mmol/L
            builder.Entity<LabResults>().Property(l => l.Creatinine).HasPrecision(7, 2);       // umol/L or mg/dL depending on lab
            builder.Entity<LabResults>().Property(l => l.Hemoglobin).HasPrecision(5, 2);      // g/dL
            builder.Entity<LabResults>().Property(l => l.LDL).HasPrecision(6, 2);             // mmol/L or mg/dL depending on lab
            builder.Entity<LabResults>().Property(l => l.HbA1c).HasPrecision(5, 2);           // percent
            builder.Entity<LabResults>().Property(l => l.aPTT).HasPrecision(6, 2);            // seconds
            builder.Entity<LabResults>().Property(l => l.ALT).HasPrecision(8, 2);             // U/L (can run very high in acute liver injury)
            builder.Entity<LabResults>().Property(l => l.AST).HasPrecision(8, 2);             // U/L (can run very high in acute liver injury)
        }

        // FINAL RELEASE-CANDIDATE PASS (section 4): the application-managed half of the
        // Admission.RowVersion concurrency token (see the IsConcurrencyToken() comment in
        // OnModelCreating above). Every Admission row that is about to be inserted or updated
        // gets a brand-new, random 16-byte token written as its CURRENT value here, immediately
        // before SaveChanges actually runs. Crucially this does NOT touch the property's
        // ORIGINAL value - for a tracked entity (Transfer/ChangeStatus/SetStrokeType/
        // RecordThrombolysis all fetch a tracked Admission first) the original value is whatever
        // EF already loaded from the database a moment ago in this same request; for the plain
        // PUT path (AdmissionController.Update), which rebuilds a brand-new, never-tracked
        // Admission object and attaches it via GenericRepository.UpdateAsync's _dbSet.Update(),
        // the original value is whatever the controller explicitly set on admission.RowVersion
        // (the client's echoed token if it sent one, otherwise the token the controller itself
        // just read at the start of this request) - see AdmissionController.Update. Either way,
        // EF's generated UPDATE ends up as
        // "... WHERE Id = @id AND RowVersion = @originalValue", and if another write already
        // changed that row since the original value was captured, zero rows match, EF throws
        // DbUpdateConcurrencyException, and Program.cs's exception handler maps that to 409.
        private void ApplyAdmissionRowVersionStamps()
        {
            foreach (var entry in ChangeTracker.Entries<NeuroStrokeCare.Data.Entities.Admission>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    var token = new byte[16];
                    Guid.NewGuid().TryWriteBytes(token);
                    entry.Property(a => a.RowVersion).CurrentValue = token;
                }
            }
        }

        // Only the two (bool, ...) overloads are overridden - DbContext's own parameterless
        // SaveChanges()/SaveChangesAsync(CancellationToken) both delegate to these virtually
        // (SaveChanges() -> SaveChanges(true), SaveChangesAsync(ct) -> SaveChangesAsync(true, ct)),
        // so every call site is covered without stamping twice.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyAdmissionRowVersionStamps();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyAdmissionRowVersionStamps();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}