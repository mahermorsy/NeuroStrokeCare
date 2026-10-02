using System.Net.Http.Json;
using System.Net;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    // Spec area 13: "Expected domain failures should not return avoidable 500 responses."
    // FINAL RELEASE-CANDIDATE PASS (sections 1(f), 4, 8): CONFIRMED AND FIXED. Program.cs's
    // single app.UseExceptionHandler now unwraps DataAccessException.InnerException and maps
    // a few known, safe domain exceptions explicitly: KeyNotFoundException (thrown directly by
    // GenericRepository.ChangeStatus on a missing id) -> 404, and DbUpdateConcurrencyException
    // (thrown by SaveChangesAsync when Admission.RowVersion no longer matches - see the new
    // concurrency token added this phase) -> 409. Anything not on that explicit list still
    // falls through to the original flat 500, unchanged.
    public class ErrorBehaviorTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        [Fact]
        public async Task Admission_ChangeStatus_OnNonExistentId_ReturnsCleanNotFound()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Admission_ChangeStatus_OnNonExistentId_ReturnsCleanNotFound));

            var response = await client.PatchAsync(
                $"/api/Admission/{Guid.NewGuid()}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}",
                content: null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Admission_Update_OnNonExistentId_ReturnsCleanNotFound()
        {
            // Counter-example included deliberately: NOT every "missing row" path is a 500.
            // UpdateCommandHandler -> TableRepository.UpdateAsync returns -1 (a sentinel, not an
            // exception) when the row doesn't exist, and AdmissionController.Update translates
            // that -1 into a clean NotFound(). ChangeStatus's repository method, by contrast,
            // throws instead of returning a sentinel - that inconsistency between the two
            // generic-repository code paths is the actual bug, not "500s everywhere".
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Admission_Update_OnNonExistentId_ReturnsCleanNotFound));

            var response = await client.PutAsJsonAsync($"/api/Admission?actingUserId={userId}", new NeuroStrokeCare.Core.Features.Admission.Dtos.UpdateAdmissionRequest
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
