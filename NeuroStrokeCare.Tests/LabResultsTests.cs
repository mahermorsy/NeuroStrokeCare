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
            // These are the one real, currently-implemented "validation-adjacent" rule in
            // LabResults: computed display-only alert flags (NOT server-side rejection of the
            // value - see the gap test below). INR > 1.7 and Glucose outside [2.8, 15].
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
        public async Task Create_WithClinicallyImpossibleNegativeValues_IsNotRejected()
        {
            // GAP (spec area 5, "Lab validation" - "test the ACTUAL currently implemented rules
            // only"): there is NO server-side range/sanity validation anywhere on LabResultsController
            // or the LabResults entity - CreateLabResultsRequest has no [Range] attributes, and
            // LabResultsService is an empty class. INRAlert/GlucoseAlert/PlateletsAlert (tested
            // above) are read-only *display* flags computed from whatever was stored, not gates
            // that ever reject a Create/Update call. A negative platelet count is accepted as-is.
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(Create_WithClinicallyImpossibleNegativeValues_IsNotRejected));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, nurseId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(Create_WithClinicallyImpossibleNegativeValues_IsNotRejected));
            var response = await nurseClient.PostAsJsonAsync($"/api/LabResults?actingUserId={nurseId}", new CreateLabResultsRequest
            {
                AdmissionId = admissionId,
                RecordedAt = DateTime.UtcNow,
                Platelets = -50, // clinically impossible, accepted anyway
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
