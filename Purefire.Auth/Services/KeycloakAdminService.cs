using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Organizations.Item.Members.InviteUser;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Users.Item.UnmanagedAttributes;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.Kiota.Abstractions;

namespace Purefire.Auth.Services;

public class KeycloakAdminService : IKeycloakAdminService
{
    private readonly KeycloakAdminApiClient _adminApiClient;
    private const string DefaultRealm = "zeus";

    public KeycloakAdminService(KeycloakAdminApiClient adminApiClient)
    {
        _adminApiClient = adminApiClient;
    }

    public async Task<IEnumerable<UserRepresentation>> GetUsersAsync()
    {
        return await _adminApiClient.Admin.Realms[DefaultRealm].Users.GetAsync() ?? Enumerable.Empty<UserRepresentation>();
    }

    public async Task<UserRepresentation> GetUserByIdAsync(string userId)
    {
        return await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].GetAsync() ?? new();
    }

    public async Task<UserRepresentation> CreateUserAsync(UserRepresentation userRepresentation)
    {

        // Create the user in Keycloak
        await _adminApiClient.Admin.Realms[DefaultRealm].Users.PostAsync(userRepresentation);

        // If username is provided, find the created user to get the ID
        if (string.IsNullOrEmpty(userRepresentation.Username)) return userRepresentation;
        var users = await _adminApiClient.Admin.Realms[DefaultRealm].Users
            .GetAsync(q =>
            {
                q.QueryParameters.Username = userRepresentation.Username;
                q.QueryParameters.Exact = true;
            });

        var createdUser = users?.FirstOrDefault();
        return createdUser ?? new UserRepresentation();  // Fallback if we couldn't retrieve the user (this shouldn't normally happen)
    }

    //add login


    public async Task UpdateUserAsync(string userId, UserRepresentation userRepresentation)
    {
        await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].PutAsync(userRepresentation);
    }

    public async Task DeleteUserAsync(string userId)
    {
        await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].DeleteAsync();
    }

    public async Task CreateRoleAsync(RoleRepresentation roleRepresentation)
    {
        await _adminApiClient.Admin.Realms[DefaultRealm].Roles.PostAsync(roleRepresentation);
    }

    public async Task AssignRoleToUserAsync(string userId, RoleRepresentation role)
    {
        var roles = new List<RoleRepresentation> { role };
        await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].RoleMappings.Realm.PostAsync(roles);
    }

    public async Task<RoleRepresentation> GetRoleByNameAsync(string roleName)
    {
        return await _adminApiClient.Admin.Realms[DefaultRealm].Roles[roleName].GetAsync() ?? new();
    }

    public async Task<IEnumerable<RoleRepresentation>> GetRolesAsync()
    {
        return await _adminApiClient.Admin.Realms[DefaultRealm].Roles.GetAsync() ?? Enumerable.Empty<RoleRepresentation>();
    }

    public async Task<bool> AddUserToOrganizationAsync(string userId, string organizationId)
    {
        try
        {
            var response = await _adminApiClient.Admin.Realms[DefaultRealm].Organizations[organizationId]
                 .Members.PostAsync(userId);

            return response != null;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400 || ex.ResponseStatusCode == 403)
        {
            return false;
        }



    }

    public async Task<bool> InviteUserToOrganizationAsync(string organizationId, InviteUserPostRequestBody request)
    {
        try
        {

            var response = await _adminApiClient.Admin.Realms[DefaultRealm].Organizations[organizationId]
                .Members.InviteUser.PostAsync(request);

            return response != null;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400 || ex.ResponseStatusCode == 403)
        {
            return false;
        }
    }

    public async Task<bool> RemoveUserFromOrganizationAsync(string userId, string organizationId)
    {
        try
        {
            var response = await _adminApiClient.Admin.Realms[DefaultRealm].Organizations[organizationId]
                 .Members[userId].DeleteAsync();
            return response != null;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400 || ex.ResponseStatusCode == 403)
        {
            return false;
        }
    }

    public async Task<IEnumerable<OrganizationRepresentation>> GetOrganizationsAsync()
    {
        return await _adminApiClient.Admin.Realms[DefaultRealm].Organizations.GetAsync() ?? Enumerable.Empty<OrganizationRepresentation>();
    }

    public async Task<OrganizationRepresentation> GetOrganizationByIdAsync(string organizationId)
    {
        return await _adminApiClient.Admin.Realms[DefaultRealm].Organizations[organizationId].GetAsync() ?? new OrganizationRepresentation();
    }

    public async Task<bool> CreateOrganizationAsync(OrganizationRepresentation organizationRepresentation)
    {
        try
        {
            var response = await _adminApiClient.Admin.Realms[DefaultRealm].Organizations.PostAsync(organizationRepresentation);
            // Status code 201 indicates successful creation
            return response != null;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400 || ex.ResponseStatusCode == 403)
        {
            // Handle both Bad Request (400) and Forbidden (403) cases
            return false;
        }
    }

    /* public async Task UpdateUserAttributesAsync(string userId, UserRepresentation_attributes? attributes)
    {
        var userRepresentation = new UserRepresentation
        {
            Attributes = attributes
        };
        var df = await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].PutAsync(userRepresentation);
    } */

    public async Task UpdateUserAttributesAsync(string userId, Dictionary<string, object> attributes)
    {
        // First, get the existing user to avoid clearing other fields
        var existingUser = await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].GetAsync();

        if (existingUser == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // Initialize attributes if null, otherwise preserve existing ones
        if (existingUser.Attributes == null)
        {
            
            existingUser.Attributes = new UserRepresentation_attributes
            {
                AdditionalData = new Dictionary<string, object>()
            };
        }
        else if (existingUser.Attributes.AdditionalData == null)
        {
            existingUser.Attributes.AdditionalData = new Dictionary<string, object>();
        }

        // Update/add the new attributes (preserving existing ones)
        foreach (var kvp in attributes)
        {
            // Convert values to string list (Keycloak stores attributes as string arrays)
            List<string> stringValues;

            if (kvp.Value is IEnumerable<string> enumerable && !(kvp.Value is string))
            {
                stringValues = enumerable.ToList();
            }
            else if (kvp.Value is string str)
            {
                stringValues = new List<string> { str };
            }
            else
            {
                stringValues = new List<string> { kvp.Value?.ToString() ?? string.Empty };
            }

            existingUser.Attributes.AdditionalData[kvp.Key] = stringValues;
        }

        // Update the user with the modified attributes
        await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].PutAsync(existingUser);
    }
    public async Task<UnmanagedAttributesGetResponse> GetUnmanagedAttributesAsync(string userId)
    {

        // This method is used to update unmanaged attributes of a user
        // It retrieves the current unmanaged attributes and returns them
        // You can modify this method to update specific attributes as needed
        var unmanagedAttributes = await _adminApiClient.Admin.Realms[DefaultRealm].Users[userId].UnmanagedAttributes.GetAsync();
        return unmanagedAttributes;
    }
}
