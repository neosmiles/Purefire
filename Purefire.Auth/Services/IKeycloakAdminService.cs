using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Organizations.Item.Members.InviteUser;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;

namespace Purefire.Auth.Services;

public interface IKeycloakAdminService
{
    Task<IEnumerable<UserRepresentation>> GetUsersAsync();
    Task<UserRepresentation> GetUserByIdAsync(string userId);
    Task<UserRepresentation> CreateUserAsync(UserRepresentation userRepresentation);
    Task UpdateUserAsync(string userId, UserRepresentation userRepresentation);
    Task DeleteUserAsync(string userId);
    Task CreateRoleAsync(RoleRepresentation roleRepresentation);
    Task AssignRoleToUserAsync(string userId, RoleRepresentation role);
    Task<RoleRepresentation> GetRoleByNameAsync(string roleName);
    Task<IEnumerable<RoleRepresentation>> GetRolesAsync();
    Task<bool> AddUserToOrganizationAsync(string userId, string organizationId);
    Task<bool> InviteUserToOrganizationAsync(string organizationId, InviteUserPostRequestBody request);
    Task<bool> RemoveUserFromOrganizationAsync(string userId, string organizationId);
    Task<IEnumerable<OrganizationRepresentation>> GetOrganizationsAsync();
    Task<OrganizationRepresentation> GetOrganizationByIdAsync(string organizationId);
    Task<bool> CreateOrganizationAsync(OrganizationRepresentation organizationRepresentation);
    Task UpdateUserAttributesAsync(string userId, Dictionary<string, List<string>> attributes);

}