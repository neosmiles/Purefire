using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;

namespace api3.Services.IServices;

public interface IAppUserService
{
    // Keycloak users only (UserRepresentation is the Keycloak SDK model)
    Task<IEnumerable<UserRepresentation>> GetAllUsersAsync();
    Task<UserRepresentation?> GetUserByIdAsync(string keycloakUserId);
    Task<UserRepresentation> CreateUserAsync(UserRepresentation user, string? password = null, string? organizationId = null);
    Task UpdateUserAsync(string keycloakUserId, UserRepresentation user);
    Task DeleteUserAsync(string keycloakUserId);

    // Role management via Keycloak Admin API
    Task<bool> CreateRealmRoleAsync(string roleName);
    Task<IEnumerable<string>> GetUserRealmRolesAsync(string keycloakUserId);
    Task<IEnumerable<string>> GetAllRealmRolesAsync();
    Task<bool> AssignRealmRoleToUserAsync(string keycloakUserId, string roleName);
    Task<bool> RemoveRealmRoleFromUserAsync(string keycloakUserId, string roleName);
}