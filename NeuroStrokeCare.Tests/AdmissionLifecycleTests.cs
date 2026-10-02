using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class AdmissionLifecycleTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;

        public Task InitializeAsync()
        {
            _factory = new CustomWebApplicationFactory();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _factory.Dispose();
            return Task.CompletedTask;
        }

        private async Task<(Guid patientId, Guid bedId)> SeedPatientAndBedAsync(Guid actingUserId)
        {
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, actingUserId);
            return (patientId, bedId);
        }

        [Fact]
        public async Task Create_ValidAdmission_Succeeds()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_ValidAdmission_Succeeds));
            var (patientId, bedId) = await SeedPatientAndBedAsync(userId);

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Create_DuplicateActiveAdmissionForSamePatient_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_DuplicateActiveAdmissionForSamePatient_Rejected));
            var (patientId, _) = await SeedPatientAndBedAsync(userId);

            var first = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            // Same patient, still has an open (non-discharged) admission from above.
            var second = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Create_OccupiedBed_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_OccupiedBed_Rejected));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId, BedStatus.Occupied);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Transfer_ToDischarged_ReleasesBed()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Transfer_ToDischarged_ReleasesBed));
            var (patientId, bedId) = await SeedPatientAndBedAsync(userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var transfer = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var bed = await client.GetAsync($"/api/Bed/{bedId}");
            var bedBody = await bed.Content.ReadFromJsonAsync<NeuroStrokeCare.Core.Features.Bed.Dtos.BedResponse>();
            Assert.Equal(BedStatus.Vacant, bedBody!.Status);

            var admission = await client.GetAsync($"/api/Admission/{admissionId}");
            var admissionBody = await admission.Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.NotNull(admissionBody!.DischargeTime);
        }

        [Fact]
        public async Task Transfer_ToNewBed_ReleasesOldBed_And_OccupiesNewBed()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Transfer_ToNewBed_ReleasesOldBed_And_OccupiesNewBed));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var oldBedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId);
            var newBedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = oldBedId,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var transfer = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.AdmittedToWard}&changeBed=true&newBedId={newBedId}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var oldBed = await (await client.GetAsync($"/api/Bed/{oldBedId}")).Content.ReadFromJsonAsync<NeuroStrokeCare.Core.Features.Bed.Dtos.BedResponse>();
            var newBed = await (await client.GetAsync($"/api/Bed/{newBedId}")).Content.ReadFromJsonAsync<NeuroStrokeCare.Core.Features.Bed.Dtos.BedResponse>();
            Assert.Equal(BedStatus.Vacant, oldBed!.Status);
            Assert.Equal(BedStatus.Occupied, newBed!.Status);
        }

        [Fact]
        public async Task Transfer_OnClosedAdmission_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Transfer_OnClosedAdmission_Rejected));
            var (patientId, bedId) = await SeedPatientAndBedAsync(userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var discharge = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, discharge.StatusCode);

            // Admission is now closed (Discharged). Per AdmissionController.Transfer's own
            // comment this should be permanently locked - trying to move it to any other
            // non-terminal status must be rejected.
            var secondTransfer = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Emergency}",
                content: null);

            Assert.Equal(HttpStatusCode.Conflict, secondTransfer.StatusCode);
        }

        // --- Discrepancy-documenting tests below: these describe GAPS against the Phase 9
        // spec's expectations, found by reading AdmissionController.cs directly. They assert
        // the CURRENT (gap) behavior so a future fix shows up as a changed/failing test here,
        // rather than silently going unnoticed. See PHASE9_REPORT.md section "Discrepancies".

        [Fact]
        public async Task Create_WithNonExistentPatientId_DoesNotReturnAFriendlyError()
        {
            // GAP: AdmissionController.Create never checks that request.PatientId refers to an
            // existing (or active) Patient - it only checks (a) no open admission already exists
            // for that id, and (b) bed vacancy. A missing patient therefore falls straight through
            // to _context.SaveChangesAsync, where only two *named unique-index* violations are
            // translated to a friendly Conflict; anything else (including a PatientId FK violation
            // on SQL Server) is rethrown as-is and hits Program.cs's catch-all handler -> 500.
            //
            // On SQLite (this test's provider) FK enforcement is OFF by default and this
            // project's test DbContext does not turn it on, so the same call may simply SUCCEED
            // here instead of failing at all - a second, independent gap on top of the first.
            // Both behaviors are wrong for a clinical system; neither is a clean 404/400.
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_WithNonExistentPatientId_DoesNotReturnAFriendlyError));

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = Guid.NewGuid(), // does not exist
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });

            // Document whichever it actually is, instead of asserting the spec's intended 404/400.
            Assert.True(
                response.StatusCode is HttpStatusCode.Created or HttpStatusCode.InternalServerError,
                $"Expected either the SQLite gap (silently succeeds: 201) or the general-exception-handler gap (500), got {response.StatusCode}. " +
                "If this now returns 404/400, the missing-patient guard was added - update this test to assert that instead.");
        }

        [Fact]
        public async Task GetById_AfterSoftDelete_ReturnsNotFound()
        {
            // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED AND
            // FIXED. AdmissionController.GetById's query now filters on
            // `a.Id == id && a.CurrentState == (int)CurrentStatusType.Active`, matching what
            // GetAll/GetPaged already did. A soft-deleted (ChangeStatus'd away from Active)
            // admission is now a clean 404 here too - this test used to document the opposite
            // (gap) behavior, 200.
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(GetById_AfterSoftDelete_ReturnsNotFound));
            var (patientId, _) = await SeedPatientAndBedAsync(userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var softDelete = await client.PatchAsync($"/api/Admission/{admissionId}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}", content: null);
            Assert.Equal(HttpStatusCode.NoContent, softDelete.StatusCode);

            var getAfterDelete = await client.GetAsync($"/api/Admission/{admissionId}");

            Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
        }
    }
}
