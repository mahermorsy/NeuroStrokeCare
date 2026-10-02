using Xunit;

namespace NeuroStrokeCare.Tests
{
    // Spec area 12: "Test SetStrokeType / RecordThrombolysis using current implementation."
    //
    // Reading AdmissionController.SetStrokeType and .RecordThrombolysis directly: both call
    // `await _context.SaveChangesAsync()` to persist the field change, THEN add the AuditLog
    // entry and call `await _context.SaveChangesAsync()` a SECOND time. That is NOT atomic -
    // it is two separate round trips/transactions. Contrast with Transfer and Create on the
    // same controller, which add the AuditLog entry to the SAME _context BEFORE their one and
    // only SaveChangesAsync call (genuinely atomic - a single transaction covers both the
    // state change and its audit row).
    //
    // Why this is left as documentation rather than an executable test: proving "not atomic"
    // requires either (a) killing the process between the two SaveChanges calls, or (b) a
    // fault-injecting DbContext/interceptor that makes the SECOND SaveChangesAsync throw and
    // then asserting the FIRST one's effect persisted anyway (i.e. the Admission row changed
    // with no matching AuditLog row). (b) is buildable, but doing it correctly needs an actual
    // dotnet run to get right (DbCommandInterceptor registration order, SQLite transaction
    // behavior under EnsureCreated, etc.) which this environment could not do - see
    // PHASE9_REPORT.md. Recorded here as a precise, actionable gap instead of a test that
    // might silently test nothing.
    public class AuditAtomicityTests
    {
        [Fact(Skip =
            "AdmissionController.SetStrokeType and .RecordThrombolysis each call SaveChangesAsync " +
            "TWICE (once for the field change, once for the AuditLog insert) instead of once - " +
            "unlike Transfer/Create on the same controller, which add their AuditLog entry before " +
            "a single SaveChangesAsync call. A failure between the two calls (e.g. a transient DB " +
            "error) leaves the clinical field changed with NO audit trail for it. Needs a fault- " +
            "injecting interceptor to prove under dotnet test - not buildable/verifiable from this " +
            "environment (no dotnet runtime available here). Recommended fix: mirror Transfer's " +
            "pattern - add the AuditLog to the same _context before the single SaveChangesAsync.")]
        public void SetStrokeTypeAndRecordThrombolysis_UseTwoSaveChangesCalls_NotOneTransaction() { }
    }
}
