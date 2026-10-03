using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NeuroStrokeCare.infrastructure.Context;

namespace NeuroStrokeCare.Tests
{
    // Phase 9 test infrastructure.
    //
    // Swaps the real SQL Server connection (appsettings.json) for a SQLite in-memory
    // database that lives only as long as this factory. Uses "Testing" as the hosting
    // environment specifically so neither Program.cs's `IsDevelopment()` DataSeeder block
    // nor its `IsProduction()` admin-bootstrap block run - each test seeds exactly the
    // users/wards/beds/patients it needs via TestDataHelper, so tests don't depend on
    // (or fight with) those two environment-gated seeding paths.
    //
    // KNOWN LIMITATION (documented per the task instructions rather than worked around):
    // SQLite does not enforce the same engine-level behavior as SQL Server in a few places
    // this codebase relies on:
    //   - Admission.PatientId "one open admission per patient" is a *filtered unique index*
    //     (`HasFilter("[DischargeTime] IS NULL AND [CurrentState] = 1")`). SQLite does accept
    //     bracket-quoted identifiers and partial/filtered indexes, so this should carry over,
    //     but it was never exercised against a real SQLite engine by the author of these tests
    //     (no dotnet runtime was available - see PHASE9_REPORT.md) - treat the first run of
    //     BedIntegrityTests/AdmissionLifecycleTests as the real verification of that claim.
    //   - SQLite's FK enforcement is OFF by default per-connection; this provider does not
    //     turn it on. A few tests below (e.g. "missing patient on Create") document current
    //     behavior rather than assert a specific status code for that reason - see the test
    //     bodies for specifics.
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<NeuroFlowDbContext>>();
                services.RemoveAll<DbContextOptions>();

                // Newer EF Core AddDbContext registrations also add provider-specific
                // IDbContextOptionsConfiguration<TContext> services. If the original SQL
                // Server configuration is left here alongside SQLite, EF sees two providers
                // in the final test service provider and refuses to start.
                foreach (var descriptor in services
                    .Where(d => d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true)
                    .ToList())
                {
                    services.Remove(descriptor);
                }

                // One shared, open connection for the whole factory lifetime - an in-memory
                // SQLite database is destroyed the moment its only connection closes, so every
                // DbContext created from this factory must reuse this same open connection
                // rather than each opening/closing its own ":memory:" (which would each get an
                // *independent* empty database).
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<NeuroFlowDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NeuroFlowDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection?.Dispose();
        }
    }
}
