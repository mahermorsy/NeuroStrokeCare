using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Core.Features.LabResults.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class LabResultsTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        private async Task<(System.Net.Http.HttpClient client, Guid userId, Guid admissionId)> CreateOpenAdmissionAsync(string testName)
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, testName);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);
            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            // Admission.Create is [Authorize(Roles = Roles.AnyDoctor)] - a Nurse can't create one.
            // LabResults tests need an admission to exist first, so this helper uses a Resident for
            // that one call and then switches to the Nurse client for the actual LabResults calls.
            return (client, userId, await create.Content.ReadFromJsonAsync<Guid>());
        }

        [Fact]
        public async Task Create_ValidLabResults_Succeeds()
        {
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(Create_ValidLabResults_Succeeds));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, nurseId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(Create_ValidLabResults_Succeeds));
            var response = await nurseClient.PostAsJsonAsync($"/api/LabResults?actingUserId={nurseId}", new CreateLabResultsRequest
            {
                AdmissionId = admissionId,
                RecordedAt = DateTime.UtcNow,
                GlucoseMmol = 5.5m,
                INR = 1.1m,
                Platelets = 250,
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task INRAlert_And_GlucoseAlert_AreComputedCorrectly()
        {
            // These are display-only alert flags, computed from whatever was actually stored -
            // separate from (and not a substitute for) the [Range] validation added in the
            // FINAL RELEASE-CANDIDATE PASS (see Create_WithNegativeValue_IsRejected below).
            // INR > 1.7 and Glucose outside [2.8, 15] here just mean "flag this as concerning",
            // not "reject this value" - a real, alarming INR of 9.5 must still be accepted.
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(INRAlert_And_GlucoseAlert_AreComputedCorrectly));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, nurseId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(INRAlert_And_GlucoseAlert_AreComputedCorrectly));
            var createLab = await nurseClient.PostAsJsonAsync($"/api/LabResults?actingUserId={nurseId}", new CreateLabResultsRequest
            {
                AdmissionId = admissionId,
                RecordedAt = DateTime.UtcNow,
                INR = 2.0m,      // > 1.7 -> alert
                GlucoseMmol = 1.5m, // < 2.8 -> alert
                Platelets = 250,  // not < 100 -> no alert
            });
            var labId = await createLab.Content.ReadFromJsonAsync<Guid>();

            var lab = await (await nurseClient.GetAsync($"/api/LabResults/{labId}")).Content.ReadFromJsonAsync<LabResultsResponse>();

            Assert.True(lab!.INRAlert);
            Assert.True(lab.GlucoseAlert);
            Assert.False(lab.PlateletsAlert);
        }

        [Fact]
        public async Task Create_WithNegativeValue_IsRejected()
        {
            // FINAL RELEASE-CANDIDATE PASS (section 7, "Lab validation"): CONFIRMED AND FIXED.
            // All 13 LabResults decimal fields now carry [Range(0, double.MaxValue)] on both
            // CreateLabResultsRequest and UpdateLabResultsRequest - no invented clinical ceiling,
            // just the one bound that is impossible for every one of these fields regardless of
            // clinical judgment (see the comment on CreateLabResultsRequest for why no upper
            // bound was added). INRAlert/GlucoseAlert/PlateletsAlert (tested above) remain
            // read-only *display* flags, unrelated to this validation. This test used to
            // document the opposite (gap) behavior - a negative platelet count being accepted.
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(Create_WithNegativeValue_IsRejected));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, nurseId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(Create_WithNegativeValue_IsRejected));
            var response = await nurseClient.PostAsJsonAsync($"/api/LabResults?actingUserId={nurseId}", new CreateLabResultsRequest
            {
                AdmissionId = admissionId,
                RecordedAt = DateTime.UtcNow,
                Platelets = -50, // clinically impossible - now rejected
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WithValidNonNegativeValues_StillSucceeds()
        {
            // Backward-compatible sanity check: ordinary, realistic (even if clinically alarming)
            // values must still be accepted - the new validation only rejects negative values,
            // it does not add an invented upper ceiling that could block a genuine extreme reading.
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(Create_WithValidNonNegativeValues_StillSucceeds));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, nurseId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(Create_WithValidNonNegativeValues_StillSucceeds));
            var response = await nurseClient.PostAsJsonAsync($"/api/LabResults?actingUserId={nurseId}", new CreateLabResultsRequest
            {
                AdmissionId = admissionId,
                RecordedAt = DateTime.UtcNow,
                INR = 9.5m, // dangerously high but real - must not be rejected
                Platelets = 0, // boundary value (zero), must be accepted
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
