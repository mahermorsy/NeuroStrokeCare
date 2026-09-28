using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroStrokeCare.Data.UserApplication;
using System;
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
        }
    }
}
