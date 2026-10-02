using Xunit;

namespace NeuroStrokeCare.Tests
{
    // Spec areas 6 ("Decimal precision") and 11 ("Concurrency") explicitly instruct: if the
    // underlying feature isn't actually there, report it instead of inventing a passing test.
    // Both gaps below were real as of Phase 11 and are now RESOLVED in the FINAL RELEASE-
    // CANDIDATE PASS - kept here (Skip left in place, reasoning updated) as a historical record
    // of what was actually checked and when, rather than deleted outright.
    public class SkippedKnownGapsTests
    {
        [Fact(Skip =
            "DECIMAL PRECISION: RESOLVED in the FINAL RELEASE-CANDIDATE PASS. " +
            "NeuroFlowDbContext.OnModelCreating now calls HasPrecision(...) explicitly for all 15 " +
            "fields the spec listed (Patient.WeightKg, Admission.ThrombolysisDoseMg, " +
            "ICHAssessment.ICHVolumeMl, and LabResults' 13 fields) - see DecimalPrecisionModelTests " +
            "for model-metadata assertions covering each one. This does NOT yet mean the real " +
            "database column types have changed - no `dotnet ef migrations add` was run (no dotnet " +
            "available in this sandbox; see FINAL_RELEASE_CANDIDATE_REPORT.md for the exact migration " +
            "the user must generate and apply on their own machine). The original finding (no " +
            "precision configured anywhere, despite an earlier phase report's claim that it existed) " +
            "is left here for the historical record.")]
        public void DecimalPrecision_NotConfigured_ReportedAsGap() { }

        [Fact(Skip =
            "CONCURRENCY: RESOLVED in the FINAL RELEASE-CANDIDATE PASS. Admission now has a " +
            "RowVersion byte[] concurrency token (IsConcurrencyToken() in OnModelCreating, stamped " +
            "with a fresh value on every insert/update by NeuroFlowDbContext.ApplyAdmissionRowVersionStamps), " +
            "and Program.cs's exception handler maps DbUpdateConcurrencyException (unwrapped from " +
            "DataAccessException where applicable) to 409. See AdmissionConcurrencyTests for a real " +
            "stale-write -> 409 regression test. The original finding (no RowVersion/concurrency " +
            "token anywhere, despite an earlier phase report's claim that it existed) is left here " +
            "for the historical record.")]
        public void Concurrency_RowVersionNotImplemented_ReportedAsGap() { }
    }
}
