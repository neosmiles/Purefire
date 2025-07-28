using api3.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api3.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<AppUser>(options)
{



    #region DbSets

    public DbSet<AppUser> AppUsers => Users; // Alias for cleaner access
    #endregion

    #region Model Configuration
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>();
    }
    #endregion



}