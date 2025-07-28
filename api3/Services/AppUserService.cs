using api3.Data;
using api3.Models;
using api3.Services.IServices;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// Added for logging potential errors

namespace api3.Services;

public class AppUserService(
    AuthDbContext context,
    IKeycloakAdminService keycloakAdminService,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager, // Added RoleManager
    ILogger<AppUserService> logger) // Added Logger
    : IAppUserService
{
    private readonly AuthDbContext _context = context;
    private readonly IKeycloakAdminService _keycloakAdminService = keycloakAdminService;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager; // Store RoleManager
    private readonly ILogger<AppUserService> _logger = logger; // Store Logger

    public async Task<IEnumerable<AppUser>> GetAllUsersAsync()
    {
        // var keycloakUsers = await keycloakAdminService.GetUsersAsync();
        var localUsers = await context.Users.IgnoreQueryFilters().ToListAsync();

        // Ensure all Keycloak users exist in local database
        // foreach (var keycloakUser in keycloakUsers)
        // {
        //     if (keycloakUser.Id == null) continue;

        //     var localUser = localUsers.FirstOrDefault(u => u.KeycloakUserId == keycloakUser.Id);
        //     if (localUser == null)
        //     {
        //         // Create missing local user reference using UserManager
        //         var newUser = new AppUser
        //         {
        //             KeycloakUserId = keycloakUser.Id,
        //             UserName = keycloakUser.Username ?? keycloakUser.Email ?? $"user_{keycloakUser.Id}",
        //             Email = keycloakUser.Email ?? "",
        //             FirstName = keycloakUser.FirstName,
        //             LastName = keycloakUser.LastName,
        //             Enabled = keycloakUser.Enabled ?? true,
        //             EmailConfirmed = keycloakUser.EmailVerified ?? false
        //         };

        //         var result = await userManager.CreateAsync(newUser);
        //         if (result.Succeeded)
        //         {
        //             localUsers.Add(newUser);
        //         }
        //     }
        // }

        return localUsers;
    }

    public async Task<UserRepresentation> GetUserByKeycloakUserIdAsync(string keycloakUserId)
    {
        // Fetch user from Keycloak by Keycloak ID
        var userRepresentation = await keycloakAdminService.GetUserByIdAsync(keycloakUserId);
        if (userRepresentation == null)
        {
            throw new KeyNotFoundException($"User with Keycloak ID '{keycloakUserId}' not found.");
        }

        return userRepresentation;
    }
    public async Task<AppUser?> GetUserByIdAsync(string id)
    {
        // First try to find by Keycloak ID
        var user = await context.Users.FirstOrDefaultAsync(u => u.KeycloakUserId == id);

        // If not found, try by local ID
        if (user == null)
        {
            user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        return user;
    }

    public async Task<AppUser> CreateUserAsync(AppUser user, string? password = null, string? organizationId = null)
    {
        _logger.LogInformation("Creating user {UserName} with email {Email} and organization {OrganizationId}",
            user.UserName, user.Email, organizationId ?? "none");

        // Create user in Keycloak
        var userRepresentation = MapToUserRepresentation(user, password);
        var createdKeycloakUser = await keycloakAdminService.CreateUserAsync(userRepresentation);

        if (string.IsNullOrEmpty(createdKeycloakUser.Id))
        {
            throw new InvalidOperationException("Failed to create user in Keycloak");
        }

        _logger.LogInformation("Successfully created user in Keycloak with ID: {KeycloakUserId}", createdKeycloakUser.Id);

        // Set KeycloakUserId and OrganizationId
        user.KeycloakUserId = createdKeycloakUser.Id;
        user.OrganizationId = organizationId;

        // Create user locally using UserManager
        //var result = await userManager.CreateAsync(user, password ?? "TempPassword123!");
        //if (!result.Succeeded)
        //{
        //    throw new InvalidOperationException($"Failed to create local user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        //}

        // If an organization ID is provided, assign the user to the organization in Keycloak
        if (!string.IsNullOrEmpty(organizationId))
        {
            _logger.LogInformation("Attempting to assign user {KeycloakUserId} to organization {OrganizationId}",
                createdKeycloakUser.Id, organizationId);

            try
            {
                // First, verify the organization exists
                var organization = await keycloakAdminService.GetOrganizationByIdAsync(organizationId);
                if (organization == null || string.IsNullOrEmpty(organization.Id))
                {
                    _logger.LogWarning("Organization with ID {OrganizationId} not found in Keycloak. Skipping organization assignment for user {UserId}.", organizationId, createdKeycloakUser.Id);
                    // Don't throw an exception here, just log the warning and continue
                    // The user is still created successfully
                }
                else
                {
                    _logger.LogInformation("Organization {OrganizationId} found: {OrganizationName}. Proceeding with user assignment.",
                        organizationId, organization.Name ?? "unnamed");

                    bool assignedToOrganization = await keycloakAdminService.AddUserToOrganizationAsync(createdKeycloakUser.Id, organizationId);

                    if (!assignedToOrganization)
                    {
                        // Log error but don't fail the entire user creation
                        _logger.LogError("Failed to assign user {UserId} to organization {OrganizationId} in Keycloak. User created successfully but organization assignment failed.", createdKeycloakUser.Id, organizationId);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully assigned user {UserId} to organization {OrganizationId} in Keycloak.", createdKeycloakUser.Id, organizationId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the specific error but don't fail the entire user creation
                _logger.LogError(ex, "Exception occurred while trying to assign user {UserId} to organization {OrganizationId} in Keycloak. User created successfully but organization assignment failed.", createdKeycloakUser.Id, organizationId);
            }
        }

        _logger.LogInformation("User creation process completed for {UserName} with Keycloak ID {KeycloakUserId}",
            user.UserName, createdKeycloakUser.Id);

        return user;
    }
    public async Task UpdateUserAsync(AppUser user)
    {
        // Update in Keycloak if user has KeycloakUserId
        if (!string.IsNullOrEmpty(user.KeycloakUserId))
        {
            var userRepresentation = MapToUserRepresentation(user);
            await keycloakAdminService.UpdateUserAsync(user.KeycloakUserId, userRepresentation);
        }

        await userManager.UpdateAsync(user);
        // }
        // Update local user using UserManager
        // var existingUser = await userManager.FindByIdAsync(user.Id);
        // if (existingUser != null )
        // {
        //     // Update properties
        //     existingUser.UserName = user.UserName;
        //     existingUser.Email = user.Email;
        //     existingUser.FirstName = user.FirstName;
        //     existingUser.LastName = user.LastName;
        //     existingUser.Enabled = user.Enabled;
        //     existingUser.EmailConfirmed = user.EmailConfirmed;
        //     existingUser.OrganizationId = user.OrganizationId;

        //     await userManager.UpdateAsync(existingUser);
        // }
    }
    public async Task DeleteUserAsync(string id)
    {
        // Find user by Keycloak ID or local ID
        var user = await context.Users.FirstOrDefaultAsync(u => u.KeycloakUserId == id || u.Id == id);

        if (user != null)
        {
            // Delete from Keycloak if user has KeycloakUserId
            if (!string.IsNullOrEmpty(user.KeycloakUserId))
            {
                await keycloakAdminService.DeleteUserAsync(user.KeycloakUserId);
            }

            // Delete from local database using UserManager
            await userManager.DeleteAsync(user);
        }
    }



    // New ASP.NET Core Identity Role Management Methods
    public async Task<IdentityResult> CreateAspNetRoleAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be empty.", nameof(roleName));
        }
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (roleExists)
        {
            // Consider specific error handling or return type
            return IdentityResult.Failed(new IdentityError { Description = $"Role '{roleName}' already exists." });
        }
        return await _roleManager.CreateAsync(new IdentityRole(roleName));
    }

    public async Task<IEnumerable<string>> GetUserAspNetRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for GetUserAspNetRolesAsync.", userId);
            return Enumerable.Empty<string>();
        }
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IEnumerable<IdentityRole>> GetAllAspNetRolesAsync()
    {
        return await _roleManager.Roles.ToListAsync();
    }

    public async Task<IdentityResult> AssignAspNetRoleToUserAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for AssignAspNetRoleToUserAsync.", userId);
            return IdentityResult.Failed(new IdentityError { Description = $"User with ID '{userId}' not found." });
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Role name cannot be empty." });
        }

        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            _logger.LogWarning("Role {RoleName} not found for assignment to user {UserId}.", roleName, userId);
            // Optionally create the role here if desired: await _roleManager.CreateAsync(new IdentityRole(roleName));
            return IdentityResult.Failed(new IdentityError { Description = $"Role '{roleName}' not found." });
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        if (result.Succeeded)
        {
            await SyncRolesToKeycloakAttributeAsync(user);
        }
        return result;
    }

    public async Task<IdentityResult> RemoveAspNetRoleFromUserAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found for RemoveAspNetRoleFromUserAsync.", userId);
            return IdentityResult.Failed(new IdentityError { Description = $"User with ID '{userId}' not found." });
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Role name cannot be empty." });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (result.Succeeded)
        {
            await SyncRolesToKeycloakAttributeAsync(user);
        }
        return result;
    }

    private async Task SyncRolesToKeycloakAttributeAsync(AppUser user)
    {
        if (user == null || string.IsNullOrEmpty(user.KeycloakUserId))
        {
            _logger.LogWarning("Cannot sync roles to Keycloak for user {UserName} - KeycloakUserId is missing.", user?.UserName);
            return;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var attributes = new Dictionary<string, object>
        {
            { "aspnet_roles", roles.ToList() } // Application-specific roles
        };
        // {

        //     AdditionalData = new Dictionary<string, object>
        //     {
        //         { "identity_roles", roles.ToList() } // Store roles in a separate attribute for easier access
        //     }
        // };


        try
        {
            await _keycloakAdminService.UpdateUserAttributesAsync(user.KeycloakUserId, attributes);
            _logger.LogInformation("Successfully synced roles to Keycloak attributes for user {UserName} (Keycloak ID: {KeycloakUserId}). Roles: {Roles}", user.UserName, user.KeycloakUserId, string.Join(", ", roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync roles to Keycloak attributes for user {UserName} (Keycloak ID: {KeycloakUserId}).", user.UserName, user.KeycloakUserId);
            // Optionally, rethrow or handle more gracefully depending on requirements
        }
    }

    private static UserRepresentation MapToUserRepresentation(AppUser user, string? password = null)
    {
        var userRepresentation = new UserRepresentation
        {
            Username = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Enabled = user.Enabled,
            EmailVerified = user.EmailConfirmed
            // Attributes should be handled by SyncRolesToKeycloakAttributeAsync or another dedicated mechanism
            // if we want to manage them broadly through this mapping.
            // For now, this mapping is primarily for user creation/core profile update.
        };

        // Only set ID if user has KeycloakUserId
        if (!string.IsNullOrEmpty(user.KeycloakUserId))
        {
            userRepresentation.Id = user.KeycloakUserId;
        }

        // Add credentials if password is provided
        if (!string.IsNullOrEmpty(password))
        {
            userRepresentation.Credentials = new List<CredentialRepresentation>
            {
                new()
                {
                    Type = "password",
                    Value = password,
                    Temporary = false
                }
            };
        }

        return userRepresentation;
    }
}