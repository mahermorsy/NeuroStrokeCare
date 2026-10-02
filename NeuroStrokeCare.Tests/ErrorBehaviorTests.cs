using System.Net.Http.Json;
using System.Net;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    // Spec area 13: "Expected domain failures should not return avoidable 500 responses."
    // Reality, traced through Program.cs + TableRepository.cs: there is exactly one exception
    // handler in the whole pipeline (Program.cs's app.UseExceptionHandler), and it maps every
    // unhandled exception - including TableRepository's own DataAccessException, which it
    // deliberately throws for a plain "not found" (ChangeStatus/UpdateAsync's missing-row paths
    // partially; see below) - to a flat 500. There is no middleware or filter anywhere that
    // inspects exception type and maps "not found" to 404. These tests document the actual,
    // current status codes for the domain failures the spec calls out, rather than the ones it
    // expects.
    public class ErrorBehaviorTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        [Fact]
        public async Task Admission_ChangeStatus_OnNonExistentId_Returns500NotFriendly404()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Admission_ChangeStatus_OnNonExistentId_Returns500NotFriendly404));

            var response = await client.PatchAsync(
                $"/api/Admission/{Guid.NewGuid()}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}",
                content: null);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
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
