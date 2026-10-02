using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.infrastructure.Context;
using NeuroStrokeCare.Service.Auth.Dtos;

namespace NeuroStrokeCare.Tests
{
    // Shared setup used across every test file below. Written against the REAL entities/DTOs
    // read from the repository (Admission, Bed, Ward, Patient, Roles, LoginRequest/AuthResponse) -
    // see PHASE9_REPORT.md for the exact files this was cross-checked against.
    public static class TestDataHelper
    {
        public const string TestPassword = "Test@12345";

        public static async Task<Guid> CreateUserAsync(IServiceProvider services, string role, string userNameSuffix)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));

            var user = new ApplicationUser
            {
                UserName = $"{role.ToLowerInvariant()}.{userNameSuffix}",
                Email = $"{role.ToLowerInvariant()}.{userNameSuffix}@test.local",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = role,
                IsApproved = true,
            };

            var createResult = await userManager.CreateAsync(user, TestPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    "Failed to create test user: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, role);
            return user.Id;
        }

        public static async Task<string> LoginAsync(HttpClient client, string userName)
        {
            var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest
            {
                UserNameOrEmail = userName,
                Password = TestPassword,
            });
            response.EnsureSuccessStatusCode();
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth?.Token is null)
                throw new InvalidOperationException("Login did not return a token.");
            return auth.Token;
        }

        // Creates a user with the given role, logs in through the real /api/Auth/login endpoint
        // (so these tests exercise the real JWT-issuing path, not a hand-crafted token), and
        // returns an HttpClient with the Bearer token already attached plus the user's id for
        // use as actingUserId on the query-string-based endpoints (Create/Transfer/etc. all take
        // actingUserId explicitly rather than reading it from the token).
        public static async Task<(HttpClient Client, Guid UserId)> CreateAuthorizedClientAsync(
            CustomWebApplicationFactory factory, string role, string userNameSuffix)
        {
            var userId = await CreateUserAsync(factory.Services, role, userNameSuffix);
            var client = factory.CreateClient();
            var token = await LoginAsync(client, $"{role.ToLowerInvariant()}.{userNameSuffix}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, userId);
        }

        public static async Task<Guid> SeedWardAsync(IServiceProvider services, string code = "FW")
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NeuroFlowDbContext>();
            var ward = new Ward
            {
                Id = Guid.NewGuid(),
                Code = code + Guid.NewGuid().ToString("N")[..6],
                Name = "Test Ward",
                TotalBeds = 10,
                CurrentState = (int)CurrentStatusType.Active,
                CreatedAt = DateTime.UtcNow,
            };
            db.Wards.Add(ward);
            await db.SaveChangesAsync();
            return ward.Id;
        }

        public static async Task<Guid> SeedBedAsync(IServiceProvider services, Guid wardId, BedStatus status = BedStatus.Vacant)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NeuroFlowDbContext>();
            var bed = new Bed
            {
                Id = Guid.NewGuid(),
                WardId = wardId,
                BedNumber = "B-" + Guid.NewGuid().ToString("N")[..8],
                Status = status,
                CurrentState = (int)CurrentStatusType.Active,
                CreatedAt = DateTime.UtcNow,
            };
            db.Beds.Add(bed);
            await db.SaveChangesAsync();
            return bed.Id;
        }

        // PHASE 11: optional trailing parameters (nationalId/hospitalNumber/firstName/lastName)
        // added so Phase 11 tests can seed exactly the identifiers they need to search/collide
        // on, without changing any existing call site above (all of which keep using the
        // original defaults: no National ID/Hospital Number, "Test ... Regression").
        public static async Task<Guid> SeedPatientAsync(
            IServiceProvider services,
            Guid createdBy,
            string? nationalId = null,
            string? hospitalNumber = null,
            string firstName = "Test",
            string lastName = "Regression")
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NeuroFlowDbContext>();
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                NationalId = nationalId,
                HospitalNumber = hospitalNumber,
                FirstName = firstName,
                MiddleName = "Patient",
                LastName = lastName,
                DateOfBirth = new DateTime(1980, 1, 1),
                Gender = "Male",
                WeightKg = 70,
                ChiefComplaint = "Sudden weakness",
                CurrentState = (int)CurrentStatusType.Active,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            return patient.Id;
        }
    }
}
