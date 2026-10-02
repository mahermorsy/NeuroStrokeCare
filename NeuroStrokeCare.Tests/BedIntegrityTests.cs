using System.Net;
using System.Net.Http.Json;
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

        // --- Discrepancy-documenting tests (Phase 9 spec area 2, "Bed integrity") ---
        // BedService/BedController have no bed-specific business logic at all - BedService.cs is
        // an empty class, and BedController's Update/ChangeStatus/Delete all go straight through
        // the generic AddAsyncGetIDCommand/UpdateCommand/ChangeStatusCommand/DeleteCommand pipeline
        // with no "is this bed currently occupied by an active admission" guard anywhere. The only
        // place bed-occupancy is actually protected is inside AdmissionController (Create/Transfer),
        // which check+flip Bed.Status themselves. These tests document that gap directly against
        // BedController rather than asserting the rejection the spec describes.

        [Fact]
        public async Task Update_OnOccupiedBed_IsNotBlocked()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(Update_OnOccupiedBed_IsNotBlocked));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId, BedStatus.Occupied);

            var response = await client.PutAsJsonAsync($"/api/Bed?actingUserId={userId}", new UpdateBedRequest
            {
                Id = bedId,
                WardId = wardId,
                BedNumber = "CHANGED-WHILE-OCCUPIED",
                Status = BedStatus.Vacant, // flips an occupied bed to vacant with no check at all
            });

            // GAP: the spec expects this to be rejected. It is not - BedController.Update has no
            // occupancy guard, so this currently succeeds (204).
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_OnOccupiedBed_IsNotBlocked()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(ChangeStatus_OnOccupiedBed_IsNotBlocked));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId, BedStatus.Occupied);

            // ChangeStatus here means Bed.CurrentState (soft-delete), not Bed.Status - but the
            // spec area explicitly calls out "disable/delete occupied bed" being blocked, and this
            // is the only "disable" verb BedController actually exposes alongside hard Delete.
            var response = await client.PatchAsync(
                $"/api/Bed/{bedId}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}",
                content: null);

            // GAP: expected a rejection (bed is occupied); actual is success (204) - no guard exists.
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_OnNonExistentBed_Returns500NotFriendly404()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Admin, nameof(ChangeStatus_OnNonExistentBed_Returns500NotFriendly404));

            // GAP (spec area 13, "Error behavior"): TableRepository.ChangeStatus throws a
            // DataAccessException (wrapping KeyNotFoundException) when the id doesn't exist, and
            // Program.cs's global exception handler maps every unhandled exception to a bare 500.
            // There is no translation to 404 anywhere on this path.
            var response = await client.PatchAsync(
                $"/api/Bed/{Guid.NewGuid()}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}",
                content: null);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
