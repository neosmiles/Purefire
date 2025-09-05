using api3.Data;
using api3.Services.IServices;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;

namespace api3.Services;

public class AppUserService : IAppUserService
{
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<AppUserService> _logger;

    public AppUserService(IKeycloakAdminService keycloakAdminService, ILogger<AppUserService> logger)
    {
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    public async Task<IEnumerable<UserRepresentation>> GetAllUsersAsync()
    {
        var users = await _keycloakAdminService.GetUsersAsync();
        return users;
    }

    public async Task<UserRepresentation?> GetUserByIdAsync(string keycloakUserId)
    {
        if (string.IsNullOrEmpty(keycloakUserId)) return null;
        var user = await _keycloakAdminService.GetUserByIdAsync(keycloakUserId);
        return user;
    }

    public async Task<UserRepresentation> CreateUserAsync(UserRepresentation user, string? password = null, string? organizationId = null)
    {
        var created = await _keycloakAdminService.CreateUserAsync(user);
        if (string.IsNullOrEmpty(created.Id))
        {
            throw new InvalidOperationException("Failed to create user in Keycloak");
        }

        if (!string.IsNullOrEmpty(organizationId))
        {
            await _keycloakAdminService.AddUserToOrganizationAsync(created.Id, organizationId);
        }

        return created;
    }

    public async Task UpdateUserAsync(string keycloakUserId, UserRepresentation user)
    {
        if (string.IsNullOrEmpty(keycloakUserId)) throw new ArgumentException("keycloakUserId is required", nameof(keycloakUserId));
        await _keycloakAdminService.UpdateUserAsync(keycloakUserId, user);
    }

    public async Task DeleteUserAsync(string keycloakUserId)
    {
        if (string.IsNullOrEmpty(keycloakUserId)) return;
        await _keycloakAdminService.DeleteUserAsync(keycloakUserId);
    }

    // Role management via Keycloak Admin API
    public async Task<bool> CreateRealmRoleAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return false;
        try
        {
            var role = new RoleRepresentation { Name = roleName };
            await _keycloakAdminService.CreateRoleAsync(role);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create realm role {RoleName}", roleName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetUserRealmRolesAsync(string keycloakUserId)
    {
        if (string.IsNullOrEmpty(keycloakUserId)) return Enumerable.Empty<string>();
        var user = await _keycloakAdminService.GetUserByIdAsync(keycloakUserId);
        if (user == null) return Enumerable.Empty<string>();
        if (user.Attributes?.AdditionalData != null && user.Attributes.AdditionalData.TryGetValue("aspnet_roles", out var rolesObj))
        {
            if (rolesObj is IEnumerable<object> arr)
                return arr.Select(o => o?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s));
        }
        return Enumerable.Empty<string>();
    }

    public async Task<IEnumerable<string>> GetAllRealmRolesAsync()
    {
        var roles = await _keycloakAdminService.GetRolesAsync();
        return roles.Select(r => r.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n));
    }

    public async Task<bool> AssignRealmRoleToUserAsync(string keycloakUserId, string roleName)
    {
        if (string.IsNullOrEmpty(keycloakUserId) || string.IsNullOrWhiteSpace(roleName)) return false;
        try
        {
            var role = await _keycloakAdminService.GetRoleByNameAsync(roleName);
            if (role == null || string.IsNullOrEmpty(role.Id)) return false;
            await _keycloakAdminService.AssignRoleToUserAsync(keycloakUserId, role);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role {RoleName} to user {UserId}", roleName, keycloakUserId);
            return false;
        }
    }

    public async Task<bool> RemoveRealmRoleFromUserAsync(string keycloakUserId, string roleName)
    {
        if (string.IsNullOrEmpty(keycloakUserId) || string.IsNullOrWhiteSpace(roleName)) return false;
        try
        {
            var role = await _keycloakAdminService.GetRoleByNameAsync(roleName);
            if (role == null || string.IsNullOrEmpty(role.Id)) return false;
            return await _keycloakAdminService.RemoveRoleFromUserAsync(keycloakUserId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove role {RoleName} from user {UserId}", roleName, keycloakUserId);
            return false;
        }
    }
}