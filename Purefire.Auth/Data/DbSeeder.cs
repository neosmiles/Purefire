using Microsoft.AspNetCore.Identity;
using Purefire.Auth.Models;

namespace Purefire.Auth.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(
            AuthDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Seed roles if they don't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Seed admin user if it doesn't exist
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed regular user if it doesn't exist
            var userEmail = "user@example.com";
            var regularUser = await userManager.FindByEmailAsync(userEmail);
            if (regularUser == null)
            {
                regularUser = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true,
                    FirstName = "Regular",
                    LastName = "User"
                };

                var result = await userManager.CreateAsync(regularUser, "User123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(regularUser, "User");
                }
            }

            // Seed clients if they don't exist
            if (!context.Clients.Any())
            {
                var clients = new List<Client>
                {
                    new Client
                    {
                        ClientId = "service1",
                        ClientSecret = "service1-secret",
                        Name = "Service 1",
                        AllowedScopes = new[] { "api1", "api2" },
                        CreatedAt = DateTime.UtcNow
                    },
                    new Client
                    {
                        ClientId = "service2",
                        ClientSecret = "service2-secret",
                        Name = "Service 2",
                        AllowedScopes = new[] { "api2" },
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Clients.AddRangeAsync(clients);
                await context.SaveChangesAsync();
            }
        }
    }
}