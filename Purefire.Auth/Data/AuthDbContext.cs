using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Purefire.Auth.Models;

namespace Purefire.Auth.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options, IMultiTenantContextAccessor tenantContextAccessor) : IdentityDbContext<AppUser>(options), IMultiTenantDbContext
{

    #region Finbuckle-required properties

    public ITenantInfo TenantInfo { get; private set; } = tenantContextAccessor?.MultiTenantContext.TenantInfo!;
    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    #endregion

    #region DbSets
    public DbSet<Client> Clients { get; set; }
    public DbSet<AppUser> AppUsers => Users; // Alias for cleaner access
    #endregion

    #region Model Configuration
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure multi-tenant entities
        builder.ConfigureMultiTenant();

        // Configure Client entity
        builder.Entity<Client>().IsMultiTenant();

        builder.Entity<Client>()
            .HasIndex(c => c.ClientId)
            .IsUnique();


        builder.Entity<AppUser>().IsMultiTenant();
    }
    #endregion

    #region Multi-Tenant Enforcement on SaveChanges Overrides
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Console.WriteLine($"TenantInfo is set to {TenantInfo.Id} in SaveChanges.");

        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"TenantInfo is set to {TenantInfo.Id} in SaveChangesAsync.");


        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    #endregion

}