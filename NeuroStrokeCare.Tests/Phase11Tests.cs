using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Core.Features.Patient.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    // PHASE 11 - PATIENT CONTEXT, HOSPITAL NUMBER & CLINICAL WORKFLOW UX (area 20).
    // Regression coverage for: HospitalNumber uniqueness (Create + Update), the new
    // GET /api/Admission/search endpoint's priority ordering / authorization / soft-delete
    // filtering / active-admission filtering, and the take clamp. Written against the real
    // PatientController/AdmissionController.Search code read directly from the repository
    // (see PHASE11_REPORT.md), following the exact same CustomWebApplicationFactory +
    // TestDataHelper pattern as the Phase 9 test files alongside this one. Does not touch or
    // weaken any existing Phase 9 test.
    public class Phase11Tests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        private static CreatePatientRequest NewPatientRequest(string firstName, string? hospitalNumber = null, string? nationalId = null) => new()
        {
            FirstName = firstName,
            MiddleName = "Phase11",
            LastName = "Test",
            DateOfBirth = new DateTime(1985, 1, 1),
            Gender = "Male",
            WeightKg = 70,
            ChiefComplaint = "Sudden weakness",
            HospitalNumber = hospitalNumber,
            NationalId = nationalId,
        };

        // --- HospitalNumber uniqueness (area 1, 20) ---------------------------------------

        [Fact]
        public async Task CreatePatient_DuplicateHospitalNumber_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(CreatePatient_DuplicateHospitalNumber_Rejected));

            var first = await client.PostAsJsonAsync($"/api/Patient?actingUserId={userId}", NewPatientRequest("Alice", hospitalNumber: "HN-1001"));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync($"/api/Patient?actingUserId={userId}", NewPatientRequest("Bob", hospitalNumber: "HN-1001"));
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreatePatient_TwoPatientsWithNoHospitalNumber_BothSucceed()
        {
            // The unique index is filtered (`WHERE HospitalNumber IS NOT NULL`) specifically so
            // multiple patients can stay without one assigned yet - this is the regression that
            // proves the filter, not a plain unique index, is actually what's deployed.
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(CreatePatient_TwoPatientsWithNoHospitalNumber_BothSucceed));

            var first = await client.PostAsJsonAsync($"/api/Patient?actingUserId={userId}", NewPatientRequest("Carol"));
            var second = await client.PostAsJsonAsync($"/api/Patient?actingUserId={userId}", NewPatientRequest("Dave"));

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        }

        [Fact]
        public async Task UpdatePatient_ToAnotherPatientsHospitalNumber_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(UpdatePatient_ToAnotherPatientsHospitalNumber_Rejected));

            await client.PostAsJsonAsync($"/api/Patient?actingUserId={userId}", NewPatientRequest("Eve", hospitalNumber: "HN-2001"));
            var createB = await client.PostAsJsonAsync($"/api/Patient?actingUserId={userId}", NewPatientRequest("Frank"));
            var patientBId = await createB.Content.ReadFromJsonAsync<Guid>();

            var update = await client.PutAsJsonAsync($"/api/Patient?actingUserId={userId}", new UpdatePatientRequest
            {
                Id = patientBId,
                HospitalNumber = "HN-2001",
                FirstName = "Frank",
                MiddleName = "Phase11",
                LastName = "Test",
                DateOfBirth = new DateTime(1985, 1, 1),
                Gender = "Male",
                WeightKg = 70,
                ChiefComplaint = "Sudden weakness",
            });

            Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
        }

        [Fact]
        public async Task UpdatePatient_KeepingItsOwnHospitalNumber_Succeeds()
        {
            // Regression for the `excludingPatientId` parameter on HospitalNumberTakenAsync -
            // a patient being updated must never collide with its OWN current value.
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(UpdatePatient_KeepingItsOwnHospitalNumber_Succeeds));

            var create = await client.PostAsJsonAsync($"/api/Patient?actingUserId={userId}", NewPatientRequest("Grace", hospitalNumber: "HN-3001"));
            var patientId = await create.Content.ReadFromJsonAsync<Guid>();

            var update = await client.PutAsJsonAsync($"/api/Patient?actingUserId={userId}", new UpdatePatientRequest
            {
                Id = patientId,
                HospitalNumber = "HN-3001",
                FirstName = "Grace",
                MiddleName = "Phase11",
                LastName = "Updated",
                DateOfBirth = new DateTime(1985, 1, 1),
                Gender = "Female",
                WeightKg = 65,
                ChiefComplaint = "Sudden weakness",
            });

            Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        }

        // --- Admission search (area 2, 15, 16, 20) -----------------------------------------

        private async Task<Guid> CreateOpenAdmissionAsync(HttpClient client, Guid actingUserId, Guid patientId)
        {
            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={actingUserId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            return await create.Content.ReadFromJsonAsync<Guid>();
        }

        [Fact]
        public async Task Search_RequiresAuthentication_Returns401()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/Admission/search?q=anything");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Search_MatchesHospitalNumberBeforeNationalId()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Search_MatchesHospitalNumberBeforeNationalId));
            var term = "7788" + Guid.NewGuid().ToString("N")[..4];

            // Patient A matches on HospitalNumber; Patient B matches on NationalId only - same
            // search term, deliberately, to exercise the priority ordering itself.
            var patientAId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId, hospitalNumber: term, firstName: "HospitalMatch");
            var patientBId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId, nationalId: term, firstName: "NationalIdMatch");

            var admissionAId = await CreateOpenAdmissionAsync(client, userId, patientAId);
            var admissionBId = await CreateOpenAdmissionAsync(client, userId, patientBId);

            var response = await client.GetAsync($"/api/Admission/search?q={term}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<AdmissionSearchResultResponse>>();

            Assert.NotNull(results);
            var ids = results!.Select(r => r.AdmissionId).ToList();
            Assert.Contains(admissionAId, ids);
            Assert.Contains(admissionBId, ids);
            Assert.True(
                ids.IndexOf(admissionAId) < ids.IndexOf(admissionBId),
                "The HospitalNumber match should rank before the NationalId-only match.");
        }

        [Fact]
        public async Task Search_ExcludesSoftDeletedAdmission()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Search_ExcludesSoftDeletedAdmission));
            var term = "HN-SOFTDEL-" + Guid.NewGuid().ToString("N")[..6];
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId, hospitalNumber: term, firstName: "SoftDeleteAdmission");
            var admissionId = await CreateOpenAdmissionAsync(client, userId, patientId);

            var softDelete = await client.PatchAsync(
                $"/api/Admission/{admissionId}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}", content: null);
            Assert.Equal(HttpStatusCode.NoContent, softDelete.StatusCode);

            var response = await client.GetAsync($"/api/Admission/search?q={term}");
            var results = await response.Content.ReadFromJsonAsync<List<AdmissionSearchResultResponse>>();

            Assert.DoesNotContain(admissionId, results!.Select(r => r.AdmissionId));
        }

        [Fact]
        public async Task Search_ExcludesAdmissionOfSoftDeletedPatient()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Search_ExcludesAdmissionOfSoftDeletedPatient));
            var term = "HN-SOFTDELPT-" + Guid.NewGuid().ToString("N")[..6];
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId, hospitalNumber: term, firstName: "SoftDeletePatient");
            var admissionId = await CreateOpenAdmissionAsync(client, userId, patientId);

            // The ADMISSION itself stays Active - only the PATIENT is soft-deleted. Search must
            // still exclude it (AdmissionController.Search filters on a.Patient.CurrentState too,
            // not just a.CurrentState).
            var softDelete = await client.PatchAsync(
                $"/api/Patient/{patientId}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}", content: null);
            Assert.Equal(HttpStatusCode.NoContent, softDelete.StatusCode);

            var response = await client.GetAsync($"/api/Admission/search?q={term}");
            var results = await response.Content.ReadFromJsonAsync<List<AdmissionSearchResultResponse>>();

            Assert.DoesNotContain(admissionId, results!.Select(r => r.AdmissionId));
        }

        [Fact]
        public async Task Search_OpenOnlyDefault_ExcludesDischargedAdmission_ButIncludesWhenOpenOnlyFalse()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Search_OpenOnlyDefault_ExcludesDischargedAdmission_ButIncludesWhenOpenOnlyFalse));
            var term = "HN-DISCHARGED-" + Guid.NewGuid().ToString("N")[..6];
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId, hospitalNumber: term, firstName: "DischargedSearch");
            var admissionId = await CreateOpenAdmissionAsync(client, userId, patientId);

            var discharge = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}", content: null);
            Assert.Equal(HttpStatusCode.NoContent, discharge.StatusCode);

            var defaultSearch = await client.GetAsync($"/api/Admission/search?q={term}");
            var defaultResults = await defaultSearch.Content.ReadFromJsonAsync<List<AdmissionSearchResultResponse>>();
            Assert.DoesNotContain(admissionId, defaultResults!.Select(r => r.AdmissionId));

            var historicalSearch = await client.GetAsync($"/api/Admission/search?q={term}&openOnly=false");
            var historicalResults = await historicalSearch.Content.ReadFromJsonAsync<List<AdmissionSearchResultResponse>>();
            Assert.Contains(admissionId, historicalResults!.Select(r => r.AdmissionId));
            Assert.False(historicalResults!.Single(r => r.AdmissionId == admissionId).IsOpen);
        }

        [Fact]
        public async Task Search_TakeIsClampedToAtLeastOne()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Search_TakeIsClampedToAtLeastOne));
            var term = "HN-CLAMP-" + Guid.NewGuid().ToString("N")[..6];
            var patientAId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId, hospitalNumber: term, firstName: "ClampA");
            var patientBId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId, hospitalNumber: term + "B", firstName: "ClampB" + term);
            await CreateOpenAdmissionAsync(client, userId, patientAId);
            await CreateOpenAdmissionAsync(client, userId, patientBId);

            // take=0 is out of range (Math.Clamp(take, 1, 50)) - must not be treated as
            // "unlimited" or error out; it should clamp up to 1.
            var response = await client.GetAsync($"/api/Admission/search?q={term}&take=0");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<AdmissionSearchResultResponse>>();
            Assert.Single(results!);
        }
    }
}
