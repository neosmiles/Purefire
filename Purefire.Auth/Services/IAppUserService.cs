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
}
