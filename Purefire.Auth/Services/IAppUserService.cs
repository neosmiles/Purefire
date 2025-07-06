using Purefire.Auth.Models;

namespace Purefire.Auth.Services;

public interface IAppUserService
{
    Task<IEnumerable<AppUser>> GetAllUsersAsync();
    Task<AppUser?> GetUserByIdAsync(string id);
    Task<AppUser> CreateUserAsync(AppUser user, string? password = null, string? organizationId = null);
    Task UpdateUserAsync(AppUser user);
    Task DeleteUserAsync(string id);

    // Role management methods
    Task<IEnumerable<RoleDto>> GetRolesAsync();
    Task<RoleDto?> GetRoleByNameAsync(string roleName);
    Task CreateRoleAsync(RoleDto role);
    Task AssignRoleToUserAsync(string userId, RoleDto role);

    // New ASP.NET Core Identity Role Management Methods
    Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAspNetRoleAsync(string roleName);
    Task<IEnumerable<string>> GetUserAspNetRolesAsync(string userId);
    Task<IEnumerable<Microsoft.AspNetCore.Identity.IdentityRole>> GetAllAspNetRolesAsync();
    Task<Microsoft.AspNetCore.Identity.IdentityResult> AssignAspNetRoleToUserAsync(string userId, string roleName);
    Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveAspNetRoleFromUserAsync(string userId, string roleName);
}
