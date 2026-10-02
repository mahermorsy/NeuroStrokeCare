using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class StrokeTypeTests : IAsyncLifetime
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
        public async Task SetStrokeType_ValidChange_Succeeds_AndAuditEntryCreated()
        {
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(SetStrokeType_ValidChange_Succeeds_AndAuditEntryCreated));

            var response = await client.PatchAsync(
                $"/api/Admission/{admissionId}/stroke-type?actingUserId={userId}&strokeType={(int)StrokeType.Ischemic}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var admission = await (await client.GetAsync($"/api/Admission/{admissionId}")).Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.Equal(StrokeType.Ischemic, admission!.StrokeType);
            Assert.Equal(userId, admission.StrokeTypeSetById);
            Assert.NotNull(admission.StrokeTypeSetAt);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NeuroStrokeCare.infrastructure.Context.NeuroFlowDbContext>();
            var auditEntry = db.AuditLogs.FirstOrDefault(a => a.EntityId == admissionId && a.Action == "StrokeTypeSet");
            Assert.NotNull(auditEntry);
        }

        // FINAL RELEASE-CANDIDATE PASS: same bug class as the Thrombolysis one the spec called
        // out explicitly (see ThrombolysisRegressionTests.NormalAdmissionPut_DoesNotEraseThrombolysisFields)
        // - found while fixing that one and closed the same way. StrokeType/StrokeTypeSetAt/
        // StrokeTypeSetById are still fields on UpdateAdmissionRequest (kept there per that
        // DTO's own comment, "until migrated to a dedicated command"), but nothing forced an
        // ordinary PUT's caller to actually echo the current values back - so a PUT that
        // merely corrected, say, CT findings could silently revert a previously-set stroke
        // type to null. AdmissionController.Update now restores all three from the pre-update
        // row unconditionally, the same way it already did for AdmittedById/Thrombolysis*.
        [Fact]
        public async Task NormalAdmissionPut_DoesNotEraseStrokeType()
        {
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(NormalAdmissionPut_DoesNotEraseStrokeType));

            var setStrokeType = await client.PatchAsync(
                $"/api/Admission/{admissionId}/stroke-type?actingUserId={userId}&strokeType={(int)StrokeType.Ischemic}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, setStrokeType.StatusCode);

            var beforePut = await (await client.GetAsync($"/api/Admission/{admissionId}")).Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.Equal(StrokeType.Ischemic, beforePut!.StrokeType); // sanity check it's really there

            // An unrelated, routine PUT that never mentions stroke type at all.
            var putResponse = await client.PutAsJsonAsync($"/api/Admission?actingUserId={userId}", new UpdateAdmissionRequest
            {
                Id = admissionId,
                PatientId = beforePut.PatientId,
                AdmissionTime = beforePut.AdmissionTime,
                Status = beforePut.Status,
                BedId = beforePut.BedId,
                CTFindings = "Updated CT findings - no new infarct",
            });
            Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

            var afterPut = await (await client.GetAsync($"/api/Admission/{admissionId}")).Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.Equal(StrokeType.Ischemic, afterPut!.StrokeType);
            Assert.Equal(userId, afterPut.StrokeTypeSetById);
            Assert.NotNull(afterPut.StrokeTypeSetAt);
        }

        [Fact]
        public async Task SetStrokeType_OnClosedAdmission_IsNotRejected()
        {
            // GAP: SetStrokeType has no terminal-status guard (unlike Transfer). A discharged
            // admission's stroke type can still be changed after the fact.
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(SetStrokeType_OnClosedAdmission_IsNotRejected));
            await client.PatchAsync($"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}", content: null);

            var response = await client.PatchAsync(
                $"/api/Admission/{admissionId}/stroke-type?actingUserId={userId}&strokeType={(int)StrokeType.Hemorrhagic}",
                content: null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
