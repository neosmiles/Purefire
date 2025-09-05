using api1.Models;
using Microsoft.EntityFrameworkCore;

namespace api1.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    // Local persistence is minimized. Keycloak is the source of truth for users.

    public DbSet<Organization> Organizations { get; set; } = null!;

    #region Model Configuration
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Add local entity configurations here if needed in the future.
    }
    #endregion
}