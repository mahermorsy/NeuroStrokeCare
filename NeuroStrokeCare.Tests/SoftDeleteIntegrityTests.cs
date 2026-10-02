using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Core.Features.LabResults.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED BUG, FIXED.
    // Every single-record GetById endpoint built on the generic GetByIdWithFiltersQuery
    // (13 controllers: Patient, Admission, DoorTiming, the 9 Assessment controllers, LabResults,
    // FollowUpNote) filtered on Id alone, with no CurrentState check - GetAll/GetPaged on the
    // very same entities DID already filter on CurrentState, so a soft-deleted record was
    // invisible in every list but still fully readable by direct id, with a normal 200. Every
    // one of those 13 controllers now filters `Id == id && CurrentState == Active`, consistent
    // with GetAll/paged and with how every write endpoint already treats "not found".
    // AdmissionLifecycleTests.GetById_AfterSoftDelete_ReturnsNotFound covers the Admission case
    // (it already existed, documenting the gap, before this fix) - this file covers Patient and
    // one representative clinical child record (LabResults) to confirm the fix is not
    // Admission-specific.
    public class SoftDeleteIntegrityTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        [Fact]
        public async Task Patient_GetById_AfterSoftDelete_ReturnsNotFound()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Patient_GetById_AfterSoftDelete_ReturnsNotFound));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);

            var softDelete = await client.PatchAsync(
                $"/api/Patient/{patientId}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, softDelete.StatusCode);

            var getAfterDelete = await client.GetAsync($"/api/Patient/{patientId}");

            Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
        }

        [Fact]
        public async Task LabResults_GetById_AfterSoftDelete_ReturnsNotFound()
        {
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(LabResults_GetById_AfterSoftDelete_ReturnsNotFound));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, nurseId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(LabResults_GetById_AfterSoftDelete_ReturnsNotFound));
            var createLab = await nurseClient.PostAsJsonAsync($"/api/LabResults?actingUserId={nurseId}", new CreateLabResultsRequest
            {
                AdmissionId = admissionId,
                RecordedAt = DateTime.UtcNow,
            });
            Assert.Equal(HttpStatusCode.Created, createLab.StatusCode);
            var labResultsId = await createLab.Content.ReadFromJsonAsync<Guid>();

            var softDelete = await nurseClient.PatchAsync(
                $"/api/LabResults/{labResultsId}/status?actingUserId={nurseId}&status={(int)CurrentStatusType.Inactive}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, softDelete.StatusCode);

            var getAfterDelete = await nurseClient.GetAsync($"/api/LabResults/{labResultsId}");

            Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
        }
    }
}
