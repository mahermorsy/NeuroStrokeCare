using System.Net;
using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Service.Auth.Dtos;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class AuthorizationTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        [Fact]
        public async Task UnauthenticatedRequest_ToClinicalEndpoint_Returns401()
        {
            // No explicit [Authorize]/[AllowAnonymous] on AdmissionController.GetAll - covered by
            // Program.cs's global FallbackPolicy (RequireAuthenticatedUser).
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/Admission");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task WrongRole_CreateAdmission_Returns403()
        {
            // Admission.Create is [Authorize(Roles = Roles.AnyDoctor)] = Admin,Consultant,Registrar,Resident.
            // Nurse/NursingSupervisor are deliberately excluded from AnyDoctor.
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, nameof(WrongRole_CreateAdmission_Returns403));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedRequest_ToAdmissionPatientSearch_Returns401()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/Admission/patient-search?query=HN");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task WrongRole_AdmissionPatientSearch_Returns403()
        {
            var (client, _) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, nameof(WrongRole_AdmissionPatientSearch_Returns403));
            var response = await client.GetAsync("/api/Admission/patient-search?query=HN");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData(Roles.Admin)]
        [InlineData(Roles.Consultant)]
        [InlineData(Roles.Registrar)]
        [InlineData(Roles.Resident)]
        public async Task AnyDoctorRole_CreateAdmission_Succeeds(string role)
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, role, nameof(AnyDoctorRole_CreateAdmission_Succeeds) + role);
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task AnyClinical_LabResultsCreate_NurseIsAllowed()
        {
            // AnyClinical = AnyDoctor + Nurse + NursingSupervisor. Confirms Nurse (excluded from
            // AnyDoctor above) IS included in this broader group, as Roles.cs defines it.
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(AnyClinical_LabResultsCreate_NurseIsAllowed));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, nurseId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(AnyClinical_LabResultsCreate_NurseIsAllowed));
            var response = await nurseClient.PostAsJsonAsync($"/api/LabResults?actingUserId={nurseId}", new NeuroStrokeCare.Core.Features.LabResults.Dtos.CreateLabResultsRequest
            {
                AdmissionId = admissionId,
                RecordedAt = DateTime.UtcNow,
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task AdminOnlyEndpoint_NonAdminDenied()
        {
            // AuthController.Register is [Authorize(Roles = "Admin")].
            var (client, _) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Consultant, nameof(AdminOnlyEndpoint_NonAdminDenied));

            var response = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Timeline_IsAnyClinical_NotAnyDoctorOnly()
        {
            // AdmissionController.GetTimeline has NO [Authorize] attribute at all (only the
            // global FallbackPolicy applies) - so in practice it's open to ANY authenticated
            // role, not specifically "AnyClinical" as the spec's wording implies. Documents the
            // actual (broader) current behavior: a Nurse (not in AnyDoctor) can still read it.
            var (doctorClient, doctorId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, "doc_" + nameof(Timeline_IsAnyClinical_NotAnyDoctorOnly));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, doctorId);
            var create = await doctorClient.PostAsJsonAsync($"/api/Admission?actingUserId={doctorId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            var (nurseClient, _) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Nurse, "nurse_" + nameof(Timeline_IsAnyClinical_NotAnyDoctorOnly));
            var response = await nurseClient.GetAsync($"/api/Admission/{admissionId}/timeline");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
