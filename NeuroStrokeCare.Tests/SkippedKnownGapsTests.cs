using Xunit;

namespace NeuroStrokeCare.Tests
{
    // Spec areas 6 ("Decimal precision") and 11 ("Concurrency") explicitly instruct: if the
    // underlying feature isn't actually there, report it instead of inventing a passing test.
    // These two are left here, visibly skipped with the exact reasoning, so they surface in
    // every `dotnet test` run until someone either implements the feature (and deletes the
    // Skip) or formally decides not to.
    public class SkippedKnownGapsTests
    {
        [Fact(Skip =
            "DECIMAL PRECISION: grepped NeuroFlowDbContext.OnModelCreating (infrastructure/Context) " +
            "for HasPrecision/[Precision]/TypeName across the whole solution - none exist anywhere. " +
            "The migration named '20261001205915_FixDecimalPrecision' was also inspected directly: " +
            "its Up()/Down() only add AspNetUsers.EmployeeId/ProfilePhotoUrl and recreate the " +
            "Admissions.PatientId filtered unique index - it does not touch decimal precision at " +
            "all, despite its name. Conclusion: decimal precision was never actually configured; " +
            "all `decimal`/`decimal?` columns (LabResults' 13 fields, ThrombolysisDoseMg, " +
            "Patient.WeightKg) use whatever default precision/scale the provider picks. A prior " +
            "phase report's claim that precision rules exist for these fields does not match the " +
            "repository. Do not test rules that were never implemented.")]
        public void DecimalPrecision_NotConfigured_ReportedAsGap() { }

        [Fact(Skip =
            "CONCURRENCY: Admission.cs (Data/Entities/Admission.cs) has no RowVersion/Timestamp " +
            "property, and NeuroFlowDbContext.OnModelCreating has no IsRowVersion()/[ConcurrencyCheck] " +
            "anywhere for Admission or any other entity. EF Core's generated UPDATE statements have " +
            "no optimistic-concurrency WHERE clause, so a 'stale write' scenario cannot currently " +
            "produce a 409 - the second writer simply wins silently. A prior phase report's claim " +
            "that optimistic concurrency (stale-write -> 409) was implemented does not match the " +
            "repository. Do not fabricate a RowVersion merely to make this test pass.")]
        public void Concurrency_RowVersionNotImplemented_ReportedAsGap() { }
    }
}
