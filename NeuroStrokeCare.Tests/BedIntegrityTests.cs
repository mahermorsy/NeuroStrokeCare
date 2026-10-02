using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Core.Features.Bed.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class BedIntegrityTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        [Fact]
        public async Task ReleasedBed_CanBeModified()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(ReleasedBed_CanBeModified));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId, BedStatus.Vacant);

            var response = await client.PutAsJsonAsync($"/api/Bed?actingUserId={userId}", new UpdateBedRequest
            {
                Id = bedId,
                WardId = wardId,
                BedNumber = "RENAMED-01",
                Status = BedStatus.Cleaning,
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // --- FINAL RELEASE-CANDIDATE PASS (section 5, "Bed integrity") ---
        // CONFIRMED AND FIXED: BedController.Update/ChangeStatus/Delete now all check whether an
        // ACTIVE, still-open Admission actually points at the bed (the real source of truth,
        // not just Bed.Status) before allowing a status change away from Occupied, a soft-delete,
        // or a hard delete - see BedController.IsHeldByActiveAdmissionAsync. The three tests below
        // used to document the opposite (gap) behavior; they now assert the fix.

        private async Task<Guid> SeedOccupiedBedWithActiveAdmissionAsync(string testName, Guid wardId)
        {
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId, BedStatus.Occupied);

            var (doctorClient, doctorUserId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, testName + "_Doctor");
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorUserId);

            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorUserId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            return bedId;
        }

        [Fact]
        public async Task Update_OnOccupiedBed_IsBlocked()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(Update_OnOccupiedBed_IsBlocked));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await SeedOccupiedBedWithActiveAdmissionAsync(nameof(Update_OnOccupiedBed_IsBlocked), wardId);

            var response = await client.PutAsJsonAsync($"/api/Bed?actingUserId={userId}", new UpdateBedRequest
            {
                Id = bedId,
                WardId = wardId,
                BedNumber = "CHANGED-WHILE-OCCUPIED",
                Status = BedStatus.Vacant, // tries to flip an occupied bed to vacant
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Update_OnOccupiedBed_KeepingStatusOccupied_IsAllowed()
        {
            // The guard is specifically about changing status AWAY from Occupied while a real
            // admission holds the bed - renaming/re-wording while leaving Status == Occupied
            // must keep working (e.g. an admin fixing a typo in BedNumber).
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(Update_OnOccupiedBed_KeepingStatusOccupied_IsAllowed));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await SeedOccupiedBedWithActiveAdmissionAsync(nameof(Update_OnOccupiedBed_KeepingStatusOccupied_IsAllowed), wardId);

            var response = await client.PutAsJsonAsync($"/api/Bed?actingUserId={userId}", new UpdateBedRequest
            {
                Id = bedId,
                WardId = wardId,
                BedNumber = "FW-01-RENAMED",
                Status = BedStatus.Occupied,
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_OnOccupiedBed_IsBlocked()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(ChangeStatus_OnOccupiedBed_IsBlocked));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await SeedOccupiedBedWithActiveAdmissionAsync(nameof(ChangeStatus_OnOccupiedBed_IsBlocked), wardId);

            // ChangeStatus here means Bed.CurrentState (soft-delete), not Bed.Status.
            var response = await client.PatchAsync(
                $"/api/Bed/{bedId}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}",
                content: null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Delete_OnOccupiedBed_IsBlocked()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(Delete_OnOccupiedBed_IsBlocked));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await SeedOccupiedBedWithActiveAdmissionAsync(nameof(Delete_OnOccupiedBed_IsBlocked), wardId);

            var response = await client.DeleteAsync($"/api/Bed/{bedId}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_OnNonExistentBed_ReturnsCleanNotFound()
        {
            // FINAL RELEASE-CANDIDATE PASS (section 8, "Error mapping"): CONFIRMED AND FIXED, same
            // underlying fix as Admission's equivalent test (ErrorBehaviorTests) - Program.cs's
            // exception handler now unwraps DataAccessException and maps the KeyNotFoundException
            // GenericRepository.ChangeStatus throws for a missing id to 404, for every entity type
            // that goes through the generic repository, not just Admission.
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(ChangeStatus_OnNonExistentBed_ReturnsCleanNotFound));

            var response = await client.PatchAsync(
                $"/api/Bed/{Guid.NewGuid()}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}",
                content: null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
