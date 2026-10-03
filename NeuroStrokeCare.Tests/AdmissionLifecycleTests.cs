using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class AdmissionLifecycleTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;

        public Task InitializeAsync()
        {
            _factory = new CustomWebApplicationFactory();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _factory.Dispose();
            return Task.CompletedTask;
        }

        private async Task<(Guid patientId, Guid bedId)> SeedPatientAndBedAsync(Guid actingUserId)
        {
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, actingUserId);
            return (patientId, bedId);
        }

        [Fact]
        public async Task Create_ValidAdmission_Succeeds()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_ValidAdmission_Succeeds));
            var (patientId, bedId) = await SeedPatientAndBedAsync(userId);

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Create_DuplicateActiveAdmissionForSamePatient_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_DuplicateActiveAdmissionForSamePatient_Rejected));
            var (patientId, _) = await SeedPatientAndBedAsync(userId);

            var first = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            // Same patient, still has an open (non-discharged) admission from above.
            var second = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Create_OccupiedBed_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_OccupiedBed_Rejected));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var bedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId, BedStatus.Occupied);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task PatientSearch_ByHospitalNumber_ReturnsEligiblePatient()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(PatientSearch_ByHospitalNumber_ReturnsEligiblePatient));
            var hospitalNumber = "HOSP-" + Guid.NewGuid().ToString("N")[..8];
            var patientId = await TestDataHelper.SeedPatientAsync(
                _factory.Services,
                userId,
                hospitalNumber: hospitalNumber,
                firstName: "Eligible",
                lastName: "Hospital");

            var response = await client.GetAsync($"/api/Admission/patient-search?query={hospitalNumber}");
            var results = await response.Content.ReadFromJsonAsync<List<AdmissionPatientSearchResultResponse>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = Assert.Single(results!);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal(hospitalNumber, result.HospitalNumber);
        }

        [Fact]
        public async Task PatientSearch_ByNationalId_ReturnsMaskedEligiblePatient()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(PatientSearch_ByNationalId_ReturnsMaskedEligiblePatient));
            var nationalId = "29801011234567";
            var patientId = await TestDataHelper.SeedPatientAsync(
                _factory.Services,
                userId,
                nationalId: nationalId,
                hospitalNumber: "NAT-" + Guid.NewGuid().ToString("N")[..8],
                firstName: "Eligible",
                lastName: "National");

            var response = await client.GetAsync($"/api/Admission/patient-search?query={nationalId}");
            var results = await response.Content.ReadFromJsonAsync<List<AdmissionPatientSearchResultResponse>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = Assert.Single(results!);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal("••••••••••4567", result.NationalIdMasked);
        }

        [Fact]
        public async Task PatientSearch_ExcludesOpenAdmissionPatients_AndRejectsExactActiveMatch()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(PatientSearch_ExcludesOpenAdmissionPatients_AndRejectsExactActiveMatch));
            var prefix = "ELIG-" + Guid.NewGuid().ToString("N")[..6];
            var eligiblePatientId = await TestDataHelper.SeedPatientAsync(
                _factory.Services,
                userId,
                hospitalNumber: $"{prefix}-A",
                firstName: "Eligible",
                lastName: "OpenFilter");
            var activePatientId = await TestDataHelper.SeedPatientAsync(
                _factory.Services,
                userId,
                hospitalNumber: $"{prefix}-B",
                firstName: "Active",
                lastName: "OpenFilter");

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = activePatientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var partialSearch = await client.GetAsync($"/api/Admission/patient-search?query={prefix}");
            var partialResults = await partialSearch.Content.ReadFromJsonAsync<List<AdmissionPatientSearchResultResponse>>();

            Assert.Equal(HttpStatusCode.OK, partialSearch.StatusCode);
            Assert.Contains(partialResults!, p => p.PatientId == eligiblePatientId);
            Assert.DoesNotContain(partialResults!, p => p.PatientId == activePatientId);

            var exactActiveSearch = await client.GetAsync($"/api/Admission/patient-search?query={prefix}-B");
            Assert.Equal(HttpStatusCode.Conflict, exactActiveSearch.StatusCode);
        }

        [Fact]
        public async Task Transfer_ToDischarged_ReleasesBed()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Transfer_ToDischarged_ReleasesBed));
            var (patientId, bedId) = await SeedPatientAndBedAsync(userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var transfer = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var bed = await client.GetAsync($"/api/Bed/{bedId}");
            var bedBody = await bed.Content.ReadFromJsonAsync<NeuroStrokeCare.Core.Features.Bed.Dtos.BedResponse>();
            Assert.Equal(BedStatus.Vacant, bedBody!.Status);

            var admission = await client.GetAsync($"/api/Admission/{admissionId}");
            var admissionBody = await admission.Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.NotNull(admissionBody!.DischargeTime);
        }

        [Fact]
        public async Task Transfer_ToNewBed_ReleasesOldBed_And_OccupiesNewBed()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Transfer_ToNewBed_ReleasesOldBed_And_OccupiesNewBed));
            var wardId = await TestDataHelper.SeedWardAsync(_factory.Services);
            var oldBedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId);
            var newBedId = await TestDataHelper.SeedBedAsync(_factory.Services, wardId);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = oldBedId,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var transfer = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.AdmittedToWard}&changeBed=true&newBedId={newBedId}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var oldBed = await (await client.GetAsync($"/api/Bed/{oldBedId}")).Content.ReadFromJsonAsync<NeuroStrokeCare.Core.Features.Bed.Dtos.BedResponse>();
            var newBed = await (await client.GetAsync($"/api/Bed/{newBedId}")).Content.ReadFromJsonAsync<NeuroStrokeCare.Core.Features.Bed.Dtos.BedResponse>();
            Assert.Equal(BedStatus.Vacant, oldBed!.Status);
            Assert.Equal(BedStatus.Occupied, newBed!.Status);
        }

        [Fact]
        public async Task Transfer_OnClosedAdmission_Rejected()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Transfer_OnClosedAdmission_Rejected));
            var (patientId, bedId) = await SeedPatientAndBedAsync(userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
                BedId = bedId,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var discharge = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}",
                content: null);
            Assert.Equal(HttpStatusCode.NoContent, discharge.StatusCode);

            // Admission is now closed (Discharged). Per AdmissionController.Transfer's own
            // comment this should be permanently locked - trying to move it to any other
            // non-terminal status must be rejected.
            var secondTransfer = await client.PatchAsync(
                $"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Emergency}",
                content: null);

            Assert.Equal(HttpStatusCode.Conflict, secondTransfer.StatusCode);
        }

        [Fact]
        public async Task Create_WithNonExistentPatientId_ReturnsNotFound()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Create_WithNonExistentPatientId_ReturnsNotFound));

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = Guid.NewGuid(), // does not exist
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetById_AfterSoftDelete_ReturnsNotFound()
        {
            // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED AND
            // FIXED. AdmissionController.GetById's query now filters on
            // `a.Id == id && a.CurrentState == (int)CurrentStatusType.Active`, matching what
            // GetAll/GetPaged already did. A soft-deleted (ChangeStatus'd away from Active)
            // admission is now a clean 404 here too - this test used to document the opposite
            // (gap) behavior, 200.
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(GetById_AfterSoftDelete_ReturnsNotFound));
            var (patientId, _) = await SeedPatientAndBedAsync(userId);

            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var softDelete = await client.PatchAsync($"/api/Admission/{admissionId}/status?actingUserId={userId}&status={(int)CurrentStatusType.Inactive}", content: null);
            Assert.Equal(HttpStatusCode.NoContent, softDelete.StatusCode);

            var getAfterDelete = await client.GetAsync($"/api/Admission/{admissionId}");

            Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
        }
    }
}
