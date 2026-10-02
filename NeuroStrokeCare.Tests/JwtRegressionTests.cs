using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    public class JwtRegressionTests : IAsyncLifetime
    {
        private CustomWebApplicationFactory _factory = null!;
        public Task InitializeAsync() { _factory = new CustomWebApplicationFactory(); return Task.CompletedTask; }
        public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

        private string CraftToken(TimeSpan expiresIn, string role = "Consultant")
        {
            using var scope = _factory.Services.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var jwt = config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, "crafted-user"),
                    new Claim(ClaimTypes.Role, role),
                },
                expires: DateTime.UtcNow.Add(expiresIn),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task NoToken_ProtectedEndpoint_Returns401()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/Auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ValidToken_IsAccepted()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CraftToken(TimeSpan.FromMinutes(5)));
            var response = await client.GetAsync("/api/Auth/me");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ExpiredToken_IsRejected()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CraftToken(TimeSpan.FromMinutes(-10)));
            var response = await client.GetAsync("/api/Auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TokenWithWrongRole_IsRejectedOnRoleRestrictedEndpoint()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CraftToken(TimeSpan.FromMinutes(5), role: "Nurse"));

            var response = await client.PostAsJsonAsync($"/api/Admission?actingUserId={Guid.NewGuid()}",
                new NeuroStrokeCare.Core.Features.Admission.Dtos.CreateAdmissionRequest
                {
                    PatientId = Guid.NewGuid(),
                    AdmissionTime = DateTime.UtcNow,
                    Status = NeuroStrokeCare.Data.Enums.PatientStatus.Emergency,
                });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RoleClaim_FromRealLogin_MapsCorrectlyIntoAuthorization()
        {
            // End-to-end through the real AuthService.LoginAsync -> GenerateToken path (not a
            // hand-crafted token), confirming the Role claim it issues is what [Authorize(Roles=...)]
            // actually checks against.
            var (client, _) = await TestDataHelper.CreateAuthorizedClientAsync(_factory, "Consultant", nameof(RoleClaim_FromRealLogin_MapsCorrectlyIntoAuthorization));
            var me = await client.GetAsync("/api/Auth/me");
            var body = await me.Content.ReadAsStringAsync();
            Assert.Contains("Consultant", body);
        }
    }
}
