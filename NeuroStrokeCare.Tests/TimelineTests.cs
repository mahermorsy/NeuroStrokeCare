using System.Net.Http.Json;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class TimelineTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        [Fact]
        public async Task Timeline_IncludesAdmittedEntry_ThenEachStatusChange()
        {
            var (client, userId) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, Roles.Resident, nameof(Timeline_IncludesAdmittedEntry_ThenEachStatusChange));
            var patientId = await TestDataHelper.SeedPatientAsync(_factory.Services, userId);
            var create = await client.PostAsJsonAsync($"/api/Admission?actingUserId={userId}", new CreateAdmissionRequest
            {
                PatientId = patientId,
                AdmissionTime = DateTime.UtcNow,
                Status = PatientStatus.Emergency,
            });
            var admissionId = await create.Content.ReadFromJsonAsync<Guid>();

            await client.PatchAsync($"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.AdmittedToWard}", content: null);
            await client.PatchAsync($"/api/Admission/{admissionId}/transfer?actingUserId={userId}&newStatus={(int)PatientStatus.Discharged}", content: null);

            var timeline = await (await client.GetAsync($"/api/Admission/{admissionId}/timeline"))
                .Content.ReadFromJsonAsync<List<AdmissionTimelineEntryResponse>>();

            Assert.NotNull(timeline);
            Assert.Equal(3, timeline!.Count); // Admitted (synthetic) + 2 StatusChanged entries
            Assert.Equal("Admitted", timeline[0].StatusLabel);
            Assert.Equal("AdmittedToWard", timeline[1].StatusLabel);
            Assert.Equal("Discharged", timeline[2].StatusLabel);
            Assert.Equal(0, timeline[0].MinutesSinceArrival);
        }
    }
}
