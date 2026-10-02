using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class ThrombolysisRegressionTests : IAsyncLifetime
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
        public async Task RecordThrombolysis_ValidCall_Succeeds_AndRecordedByIsStored()
        {
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(RecordThrombolysis_ValidCall_Succeeds_AndRecordedByIsStored));

            var response = await client.PatchAsync(
                $"/api/Admission/{admissionId}/thrombolysis?actingUserId={userId}&drug=Alteplase&doseMg=9.5",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await client.GetAsync($"/api/Admission/{admissionId}");
            var admission = await getResponse.Content.ReadFromJsonAsync<AdmissionResponse>();

            Assert.Equal("Alteplase", admission!.ThrombolysisDrug);
            Assert.Equal(9.5m, admission.ThrombolysisDoseMg);
            Assert.Equal(userId, admission.ThrombolysisRecordedById);
            Assert.NotNull(admission.ThrombolysisGivenAt);
        }

        [Fact]
        public async Task RecordThrombolysis_CreatesAuditLogEntry()
        {
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(RecordThrombolysis_CreatesAuditLogEntry));
            await client.PatchAsync($"/api/Admission/{admissionId}/thrombolysis?actingUserId={userId}&drug=Tenecteplase&doseMg=5", content: null);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NeuroStrokeCare.infrastructure.Context.NeuroFlowDbContext>();
            var auditEntry = db.AuditLogs.FirstOrDefault(a =>
                a.EntityId == admissionId && a.Action == "ThrombolysisRecorded");

            Assert.NotNull(auditEntry);
        }

        // ============================================================================
        // CRITICAL (spec explicitly called this out): does a normal Admission PUT erase
        // the thrombolysis fields it never mentions?
        //
        // Root cause, traced through the real source:
        //   - UpdateAdmissionRequest (Core/Features/Admission/Dtos/UpdateAdmissionRequest.cs)
        //     has NO ThrombolysisGivenAt/ThrombolysisDrug/ThrombolysisDoseMg/
        //     ThrombolysisRecordedById properties at all.
        //   - AdmissionProfile's `CreateMap<UpdateAdmissionRequest, Admission>()` has no
        //     .ForMember(...).Ignore() for those fields, so AutoMapper leaves them at the
        //     CLR default (null) on the freshly-`new Admission()`-mapped object.
        //   - TableRepository.UpdateAsync only copies CreatedAt/CreatedBy/CurrentState over
        //     from the existing DB row before calling `_dbSet.Update(entity)` (a full-entity
        //     update) - it does NOT copy the four Thrombolysis* fields forward.
        // Net effect: PUT /api/admission on an admission that already has thrombolysis
        // recorded WIPES that treatment record back to null, silently, with no error.
        //
        // This test asserts the CORRECT behavior (fields survive a PUT) and is expected to
        // FAIL on the current code - that failure *is* the deliverable here. Do not
        // "fix" this test to match the bug; fix AdmissionProfile/UpdateAdmissionRequest (or
        // AdmissionController.Update) instead, then this test should start passing.
        // ============================================================================
        [Fact]
        public async Task NormalAdmissionPut_DoesNotEraseThrombolysisFields()
        {
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(NormalAdmissionPut_DoesNotEraseThrombolysisFields));

            var record = await client.PatchAsync(
                $"/api/Admission/{admissionId}/thrombolysis?actingUserId={userId}&drug=Alteplase&doseMg=8",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, record.StatusCode);

            var beforePut = await (await client.GetAsync($"/api/Admission/{admissionId}")).Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.Equal("Alteplase", beforePut!.ThrombolysisDrug); // sanity check it's really there

            // An unrelated, routine PUT - e.g. a doctor correcting the CT findings text -
            // that never touches thrombolysis at all.
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

            Assert.Equal("Alteplase", afterPut!.ThrombolysisDrug);
            Assert.Equal(8m, afterPut.ThrombolysisDoseMg);
            Assert.Equal(userId, afterPut.ThrombolysisRecordedById);
            Assert.NotNull(afterPut.ThrombolysisGivenAt);
        }

        // --- Further discrepancy-documenting tests (no validation exists on this endpoint) ---

        [Fact]
        public async Task RecordThrombolysis_EmptyDrugString_IsNotRejected()
        {
            // GAP: `[FromQuery] string drug` has no [Required]/length validation of its own, and
            // an empty string "" still satisfies ASP.NET Core's implicit non-nullable-reference-type
            // requirement (which only rejects a *missing* value, not an empty one).
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(RecordThrombolysis_EmptyDrugString_IsNotRejected));

            var response = await client.PatchAsync(
                $"/api/Admission/{admissionId}/thrombolysis?actingUserId={userId}&drug=&doseMg=5",
                content: null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RecordThrombolysis_NegativeDose_IsNotRejected()
        {
            // GAP: doseMg has no [Range]/positivity check anywhere on this path.
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(RecordThrombolysis_NegativeDose_IsNotRejected));

            var response = await client.PatchAsync(
                $"/api/Admission/{admissionId}/thrombolysis?actingUserId={userId}&drug=Alteplase&doseMg=-10",
                content: null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RecordThrombolysis_OnClosedAdmission_IsNotRejected()
        {
            // GAP: unlike Transfer, RecordThrombolysis never checks admission.Status for a
            // terminal (Discharged/TransferredOut) state before writing.
            var (client, userId, admissionId) = await CreateOpenAdmissionAsync(nameof(RecordThrombolysis_OnClosedAdmission_IsNotRejected));
            await client.PatchAsync($"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}", content: null);

            var response = await client.PatchAsync(
                $"/api/Admission/{admissionId}/thrombolysis?actingUserId={userId}&drug=Alteplase&doseMg=9",
                content: null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
