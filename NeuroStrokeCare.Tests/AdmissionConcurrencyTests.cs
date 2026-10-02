using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    // FINAL RELEASE-CANDIDATE PASS (section 4): real stale-write regression test for
    // Admission's new RowVersion concurrency token (see NeuroFlowDbContext's
    // IsConcurrencyToken()/ApplyAdmissionRowVersionStamps and AdmissionController.Update's
    // "request.RowVersion ?? current.RowVersion" fallback). Also closes the matching entry in
    // SkippedKnownGapsTests.
    //
    // Scope note: this exercises the one Admission write path (the plain PUT) that actually
    // accepts an echoed RowVersion from the caller, because it is the only one where a true,
    // deterministic, two-sequential-HTTP-calls test of cross-request staleness is possible.
    // Transfer/ChangeStatus/SetStrokeType/RecordThrombolysis all fetch a TRACKED entity and so
    // get the same RowVersion column and the same 409 mapping "for free" from
    // NeuroFlowDbContext/Program.cs, but the race window they protect (another write landing
    // between that fetch and that same request's own SaveChanges) is not something two
    // sequential HTTP calls in a test can reliably force without reaching into the DbContext
    // mid-request - not attempted here per the spec's instruction not to fabricate a passing
    // test for something that cannot actually be verified this way.
    public class AdmissionConcurrencyTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        private async Task<(System.Net.Http.HttpClient client, Guid userId, Guid admissionId)> CreateOpenAdmissionAsync(string testName)
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, testName);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);
            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();
            return (client, userId, admissionId);
        }

        [Fact]
        public async Task Update_WithStaleRowVersion_Returns409AndDoesNotOverwrite()
        {
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(Update_WithStaleRowVersion_Returns409AndDoesNotOverwrite));

            // Two "page loads" of the same admission, both holding the same original RowVersion.
            var firstRead = await (await client.GetAsync($"/api/Admission/{admissionId}")).Content.ReadFromJsonAsync<AdmissionResponse>();
            var staleRowVersion = firstRead!.RowVersion;

            // The "other user" saves first - a routine PUT that echoes the same (still-current)
            // RowVersion it just read, and succeeds, bumping the row's RowVersion in the process.
            var firstPut = await client.PutAsJsonAsync($"/api/Admission?actingUserId={userId}", new UpdateAdmissionRequest
            {
                Id = admissionId,
                PatientId = firstRead.PatientId,
                AdmissionTime = firstRead.AdmissionTime,
                Status = firstRead.Status,
                BedId = firstRead.BedId,
                CTFindings = "First writer's note",
                RowVersion = staleRowVersion,
            });
            Assert.Equal(HttpStatusCode.NoContent, firstPut.StatusCode);

            // The "first user" now submits its own edit, still holding the ORIGINAL RowVersion
            // from before the other user's save above - a genuine stale write.
            var secondPut = await client.PutAsJsonAsync($"/api/Admission?actingUserId={userId}", new UpdateAdmissionRequest
            {
                Id = admissionId,
                PatientId = firstRead.PatientId,
                AdmissionTime = firstRead.AdmissionTime,
                Status = firstRead.Status,
                BedId = firstRead.BedId,
                CTFindings = "Second writer's (stale) note",
                RowVersion = staleRowVersion,
            });

            Assert.Equal(HttpStatusCode.Conflict, secondPut.StatusCode);

            // The first writer's note must survive - the stale second write must not have landed.
            var afterConflict = await (await client.GetAsync($"/api/Admission/{admissionId}")).Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.Equal("First writer's note", afterConflict!.CTFindings);
        }

        [Fact]
        public async Task Update_WithoutRowVersion_StillSucceeds_BackwardCompatible()
        {
            // No current frontend caller sends RowVersion at all (admissionsApi.update() has no
            // call sites anywhere in NeuroStrokeCare.client/src - confirmed by grep). Omitting it
            // must keep working exactly as before: the controller falls back to the RowVersion
            // it reads itself at the start of this request.
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(Update_WithoutRowVersion_StillSucceeds_BackwardCompatible));
            var current = await (await client.GetAsync($"/api/Admission/{admissionId}")).Content.ReadFromJsonAsync<AdmissionResponse>();

            var putResponse = await client.PutAsJsonAsync($"/api/Admission?actingUserId={userId}", new UpdateAdmissionRequest
            {
                Id = admissionId,
                PatientId = current!.PatientId,
                AdmissionTime = current.AdmissionTime,
                Status = current.Status,
                BedId = current.BedId,
                CTFindings = "No RowVersion sent at all",
            });

            Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);
        }
    }
}
