
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Purefire.Auth.Data;

// This context is ONLY for managing tenant definitions via Finbuckle
public class TenantContext(DbContextOptions<TenantContext> options) : EFCoreStoreDbContext<TenantInfo>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Call base first to ensure Finbuckle's TenantInfo configuration is applied
        base.OnModelCreating(builder);

        // OPTIONAL: Add any *additional* specific configurations for KeycloakTenantInfo
        // if needed, beyond what EFCoreStoreDbContext already does.
        // Example: Maybe you want a specific Table name JUST for the store.
        /*
        builder.Entity<KeycloakTenantInfo>(b =>
        {
            b.ToTable("TenantDefinitions"); // Example custom table name
            b.Property(t => t.Realm).HasMaxLength(200);
            // ... other property configurations
        });
        */

        
    }
}