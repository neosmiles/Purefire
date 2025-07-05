using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Identity;
using Purefire.Auth.Models;

namespace Purefire.Auth.Data
{
    public static class TenantDbSeeder
    {
        public static async Task SeedDataAsync(
            TenantContext context)
        {
            var tenantInfo = new TenantInfo
            {
                Id = "lg",
                Identifier = "lg",
                Name = "lg Tenant",
                // ConnectionString = "Server=localhost;Database=DefaultTenantDb;Trusted_Connection=True;"
            };

            await context.TenantInfo.AddAsync(tenantInfo);
            await context.SaveChangesAsync();
        }
    }
}