using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Purefire.Auth.Models;
using Purefire.Auth.Services;

namespace Purefire.Auth.Data;

public class AuthDbContext : IdentityDbContext<AppUser>
{
    #region Fields and Constructor
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantService _tenantService;

    public AuthDbContext(DbContextOptions<AuthDbContext> options, IHttpContextAccessor httpContextAccessor, ITenantService tenantService)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantService = tenantService;
    }
    #endregion

    #region DbSets
    public DbSet<Client> Clients { get; set; }
    public DbSet<AppUser> AppUsers => Users; // Alias for cleaner access
    #endregion

    #region Model Configuration
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Client entity
        builder.Entity<Client>()
            .HasIndex(c => c.ClientId)
            .IsUnique();

        // Apply global query filters to ALL entities implementing ITenantEntity
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }
    }
    #endregion

    #region Tenant Management
    private string GetCurrentTenantId()
    {
        // Try HTTP context first (web requests)
        var httpTenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(httpTenantId))
            return httpTenantId;

        // Try tenant service (background jobs)
        return _tenantService?.GetCurrentTenantId() ?? string.Empty;
    }

    private static readonly MethodInfo SetGlobalQueryMethod = typeof(AuthDbContext)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(t => t.IsGenericMethodDefinition && t.Name == "SetGlobalQuery");

    private void SetGlobalQuery<T>(ModelBuilder builder) where T : class, ITenantEntity
    {
        builder.Entity<T>().HasQueryFilter(e =>
            string.IsNullOrEmpty(e.OrganizationId) ||
            e.OrganizationId == GetCurrentTenantId());
    }

    private void SetTenantForNewEntities()
    {
        var currentTenantId = GetCurrentTenantId();

        var newTenantEntities = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added && string.IsNullOrEmpty(e.Entity.OrganizationId))
            .ToList();

        if (!newTenantEntities.Any()) return;

        if (string.IsNullOrEmpty(currentTenantId))
        {
            var entityTypes = string.Join(", ", newTenantEntities.Select(e => e.Entity.GetType().Name));
            throw new InvalidOperationException(
                $"Cannot save tenant-specific entities [{entityTypes}] without tenant context. " +
                "Ensure user is authenticated or tenant context is set for background operations.");
        }

        foreach (var entry in newTenantEntities)
        {
            entry.Entity.OrganizationId = currentTenantId;
        }
    }
    #endregion

    #region SaveChanges Overrides
    public override int SaveChanges()
    {
        SetTenantForNewEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantForNewEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }
    #endregion

    #region Query Helpers
    // Helper methods for when you need to bypass filters
    public IQueryable<T> GetAllTenantsData<T>() where T : class
    {
        return Set<T>().IgnoreQueryFilters();
    }

    public IQueryable<T> GetTenantData<T>(string tenantId) where T : class
    {
        return Set<T>().IgnoreQueryFilters()
            .Where(e => EF.Property<string>(e, "TenantId") == tenantId);
    }
    #endregion
}