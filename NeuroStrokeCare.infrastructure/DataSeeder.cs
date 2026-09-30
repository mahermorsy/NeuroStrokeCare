using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.infrastructure.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroStrokeCare.infrastructure
{
    // Development-only seed users. Passwords are intentionally not hardcoded.
    // Set SeedUsers:DefaultPassword or SeedUsers:<Role>:Password through user-secrets or environment variables.
    public static class DataSeeder
    {
        private static readonly (string UserName, string Email, string FirstName, string LastName, string Role)[] SeedUsers =
        {
            ("admin", "admin@neurostrokecare.local", "System", "Admin", "Admin"),
            ("consultant", "consultant@neurostrokecare.local", "Laila", "Hassan", "Consultant"),
            ("registrar", "registrar@neurostrokecare.local", "Omar", "Adel", "Registrar"),
            ("resident", "resident@neurostrokecare.local", "Nourhan", "Samir", "Resident"),
            ("nurse", "nurse@neurostrokecare.local", "Mona", "Farid", "Nurse"),
            ("supervisor", "supervisor@neurostrokecare.local", "Heba", "Younis", "NursingSupervisor"),
        };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            foreach (var roleName in new[] { "Admin", "Consultant", "Registrar", "Resident", "Nurse", "NursingSupervisor" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }

            foreach (var seed in SeedUsers)
            {
                if (await userManager.FindByNameAsync(seed.UserName) != null)
                    continue;

                var password = configuration[$"SeedUsers:{seed.Role}:Password"] ?? configuration["SeedUsers:DefaultPassword"];
                if (string.IsNullOrWhiteSpace(password))
                    continue;

                var user = new ApplicationUser
                {
                    UserName = seed.UserName,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    FirstName = seed.FirstName,
                    LastName = seed.LastName,
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, seed.Role);
                }
            }

            var dbContext = serviceProvider.GetRequiredService<NeuroFlowDbContext>();
            await SeedWardsAndBedsAsync(dbContext);
        }

        // Real Stroke Unit layout at Mansoura University Hospital — Female Stroke Ward (23 beds),
        // Male Stroke Ward (17 beds), Neuro-ICU (10 beds), Intermediate Care (7 beds), plus an
        // ER-Stroke bay (4 rapid-assessment bays) for patients pending admission. Idempotent: only
        // runs when the Wards table is empty, so it never overwrites real bed data or statuses.
        private static readonly (WardCode Code, string Name, int BedCount)[] WardLayout =
        {
            (WardCode.FW, "Female Stroke Ward", 23),
            (WardCode.MW, "Male Stroke Ward", 17),
            (WardCode.NICU, "Neuro-ICU", 10),
            (WardCode.IMC, "Intermediate Care", 7),
            (WardCode.ER, "ER-Stroke Bay", 4),
        };

        private static async Task SeedWardsAndBedsAsync(NeuroFlowDbContext context)
        {
            if (await Task.FromResult(context.Wards.Any()))
                return;

            foreach (var w in WardLayout)
            {
                var wardCodeStr = w.Code.ToString();
                var ward = new Ward
                {
                    Id = Guid.NewGuid(),
                    Code = wardCodeStr,
                    Name = w.Name,
                    TotalBeds = w.BedCount,
                    CurrentState = (int)CurrentStatusType.Active,
                };
                context.Wards.Add(ward);

                for (var i = 1; i <= w.BedCount; i++)
                {
                    context.Beds.Add(new Bed
                    {
                        Id = Guid.NewGuid(),
                        WardId = ward.Id,
                        BedNumber = $"{wardCodeStr}-{i:D2}",
                        Status = BedStatus.Vacant,
                        CurrentState = (int)CurrentStatusType.Active,
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
