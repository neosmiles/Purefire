using BCrypt.Net;
using Purefire.Auth.Models;

namespace Purefire.Auth.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(AuthDbContext context)
        {
            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    new User
                    {
                        Username = "admin",
                        Email = "admin@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        Role = "Admin",
                        CreatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Username = "user",
                        Email = "user@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                        Role = "User",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Users.AddRangeAsync(users);
            }

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
            }

            await context.SaveChangesAsync();
        }
    }
}