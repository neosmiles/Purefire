using api3.Services.IServices;
using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Organizations.Item.Members.InviteUser;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Users.Item.UnmanagedAttributes;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.Kiota.Abstractions;

namespace api3.Services;

public class KeycloakAdminService(KeycloakAdminApiClient adminApiClient, IConfiguration config) : IKeycloakAdminService
{
    private readonly string _defaultRealm = config["Keycloak:realm"]!;
    public async Task<IEnumerable<UserRepresentation>> GetUsersAsync()
    {
        return await adminApiClient.Admin.Realms[_defaultRealm].Users.GetAsync() ?? Enumerable.Empty<UserRepresentation>();
    }

    public async Task<UserRepresentation> GetUserByIdAsync(string userId)
    {
        return await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].GetAsync() ?? new();
    }

    public async Task<UserRepresentation> CreateUserAsync(UserRepresentation userRepresentation)
    {

        // Create the user in Keycloak
        await adminApiClient.Admin.Realms[_defaultRealm].Users.PostAsync(userRepresentation);

        // If username is provided, find the created user to get the ID
        if (string.IsNullOrEmpty(userRepresentation.Username)) return userRepresentation;
        var users = await adminApiClient.Admin.Realms[_defaultRealm].Users
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
        await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].PutAsync(userRepresentation);
    }

    public async Task DeleteUserAsync(string userId)
    {
        await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].DeleteAsync();
    }

    public async Task CreateRoleAsync(RoleRepresentation roleRepresentation)
    {
        await adminApiClient.Admin.Realms[_defaultRealm].Roles.PostAsync(roleRepresentation);
    }

    public async Task AssignRoleToUserAsync(string userId, RoleRepresentation role)
    {
        var roles = new List<RoleRepresentation> { role };
        await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].RoleMappings.Realm.PostAsync(roles);
    }

    public async Task<bool> RemoveRoleFromUserAsync(string userId, RoleRepresentation role)
    {
        try
        {
            var roles = new List<RoleRepresentation> { role };
            await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].RoleMappings.Realm.DeleteAsync(roles);
            return true;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400 || ex.ResponseStatusCode == 404)
        {
            // Nothing to remove or not found
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<RoleRepresentation> GetRoleByNameAsync(string roleName)
    {
        return await adminApiClient.Admin.Realms[_defaultRealm].Roles[roleName].GetAsync() ?? new();
    }

    public async Task<IEnumerable<RoleRepresentation>> GetRolesAsync()
    {
        return await adminApiClient.Admin.Realms[_defaultRealm].Roles.GetAsync() ?? Enumerable.Empty<RoleRepresentation>();
    }

    public async Task<bool> AddUserToOrganizationAsync(string userId, string organizationId)
    {
        try
        {
            var response = await adminApiClient.Admin.Realms[_defaultRealm].Organizations[organizationId]
                .Members.PostAsync(userId);

            return response != null;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            // More specific logging for 404 errors
            throw new InvalidOperationException($"Organization with ID '{organizationId}' not found or user with ID '{userId}' not found in Keycloak realm '{_defaultRealm}'.", ex);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400)
        {
            // Handle bad request (e.g., user already in organization)
            throw new InvalidOperationException($"Bad request when adding user '{userId}' to organization '{organizationId}'. This might indicate the user is already a member or invalid parameters.", ex);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 403)
        {
            // Handle forbidden (insufficient permissions)
            throw new InvalidOperationException($"Insufficient permissions to add user '{userId}' to organization '{organizationId}' in Keycloak.", ex);
        }
        catch (ApiException ex)
        {
            // Handle other API exceptions
            throw new InvalidOperationException($"Keycloak API error (Status: {ex.ResponseStatusCode}) when adding user '{userId}' to organization '{organizationId}': {ex.Message}", ex);
        }
    }

    public async Task<bool> InviteUserToOrganizationAsync(string organizationId, InviteUserPostRequestBody request)
    {
        try
        {

            var response = await adminApiClient.Admin.Realms[_defaultRealm].Organizations[organizationId]
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
            var response = await adminApiClient.Admin.Realms[_defaultRealm].Organizations[organizationId]
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
        return await adminApiClient.Admin.Realms[_defaultRealm].Organizations.GetAsync() ?? Enumerable.Empty<OrganizationRepresentation>();
    }

    public async Task<OrganizationRepresentation> GetOrganizationByIdAsync(string organizationId)
    {
        if (string.IsNullOrEmpty(organizationId))
        {
            return new OrganizationRepresentation();
        }

        try
        {
            var org = await adminApiClient.Admin.Realms[_defaultRealm].Organizations[organizationId].GetAsync();
            return org ?? new OrganizationRepresentation();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            // Return empty object to indicate organization not found
            return new OrganizationRepresentation();
        }
    }



    public async Task<bool> CreateOrganizationAsync(OrganizationRepresentation organizationRepresentation)
    {
        try
        {
            var response = await adminApiClient.Admin.Realms[_defaultRealm].Organizations.PostAsync(organizationRepresentation);
            // Status code 201 indicates successful creation
            return response != null;
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 400 || ex.ResponseStatusCode == 403)
        {
            // Handle both Bad Request (400) and Forbidden (403) cases
            return false;
        }
    }



    public async Task UpdateUserAttributesAsync(string userId, Dictionary<string, object> attributes)
    {
        // First, get the existing user to avoid clearing other fields
        var existingUser = await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].GetAsync();

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
        await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].PutAsync(existingUser);
    }
    public async Task<UnmanagedAttributesGetResponse> GetUnmanagedAttributesAsync(string userId)
    {

        // This method is used to update unmanaged attributes of a user
        // It retrieves the current unmanaged attributes and returns them
        // You can modify this method to update specific attributes as needed
        var unmanagedAttributes = await adminApiClient.Admin.Realms[_defaultRealm].Users[userId].UnmanagedAttributes.GetAsync();
        return unmanagedAttributes;
    }
}