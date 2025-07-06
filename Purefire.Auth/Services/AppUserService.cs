using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Purefire.Auth.Data;
using Purefire.Auth.Models;
using Microsoft.Extensions.Logging; // Added for logging potential errors

namespace Purefire.Auth.Services;

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
        var keycloakUsers = await keycloakAdminService.GetUsersAsync();
        var localUsers = await context.Users.ToListAsync();

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
    public async Task<AppUser?> GetUserByIdAsync(string id)
    {
        // First try to find by Keycloak ID
        var user = await context.Users.FirstOrDefaultAsync(u => u.KeycloakUserId == id);

        // If not found, try by local ID
        if (user == null)
        {
            user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        // if (user == null)
        // {
        //     // Try to get user from Keycloak
        //     var keycloakUser = await keycloakAdminService.GetUserByIdAsync(id);

        //     if (!string.IsNullOrEmpty(keycloakUser.Id))
        //     {
        //         // Create local user reference using UserManager
        //         user = new AppUser
        //         {
        //             KeycloakUserId = keycloakUser.Id,
        //             UserName = keycloakUser.Username ?? keycloakUser.Email ?? $"user_{keycloakUser.Id}",
        //             Email = keycloakUser.Email ?? "",
        //             FirstName = keycloakUser.FirstName,
        //             LastName = keycloakUser.LastName,
        //             Enabled = keycloakUser.Enabled ?? true,
        //             EmailConfirmed = keycloakUser.EmailVerified ?? false
        //         };

        //         var result = await userManager.CreateAsync(user);
        //         if (!result.Succeeded)
        //         {
        //             return null;
        //         }
        //     }
        // }

        return user;
    }

    public async Task<AppUser> CreateUserAsync(AppUser user, string? password = null, string? organizationId = null)
    {
        // Create user in Keycloak
        var userRepresentation = MapToUserRepresentation(user, password);
        var createdKeycloakUser = await keycloakAdminService.CreateUserAsync(userRepresentation);

        if (string.IsNullOrEmpty(createdKeycloakUser.Id))
        {
            throw new InvalidOperationException("Failed to create user in Keycloak");
        }

        // Set KeycloakUserId and OrganizationId
        user.KeycloakUserId = createdKeycloakUser.Id;
        user.OrganizationId = organizationId;

        // Create user locally using UserManager
        var result = await userManager.CreateAsync(user, password ?? "TempPassword123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create local user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // If an organization ID is provided, assign the user to the organization in Keycloak
        if (!string.IsNullOrEmpty(organizationId))
        {
            bool assignedToOrganization = await keycloakAdminService.AddUserToOrganizationAsync(createdKeycloakUser.Id, organizationId);

            if (!assignedToOrganization)
            {
                // Consider how to handle this failure. 
                // You might want to log an error or even rollback the user creation.
                // For this example, we'll just log.
                //_logger.LogError($"Failed to assign user {createdKeycloakUser.Id} to organization {organizationId} in Keycloak.");
            }
        }

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

    // Role management methods
    public async Task<IEnumerable<RoleDto>> GetRolesAsync()
    {
        var roles = await keycloakAdminService.GetRolesAsync();
        return roles.Adapt<IEnumerable<RoleDto>>();
    }

    public async Task<RoleDto?> GetRoleByNameAsync(string roleName)
    {
        var role = await keycloakAdminService.GetRoleByNameAsync(roleName);
        return role.Adapt<RoleDto>();
    }

    public async Task CreateRoleAsync(RoleDto roleDto)
    {
        var role = new RoleRepresentation
        {
            Name = roleDto.Name,
            Description = roleDto.Description
        };
        await keycloakAdminService.CreateRoleAsync(role);
    }

    public async Task AssignRoleToUserAsync(string userId, RoleDto roleDto)
    {
        var role = new RoleRepresentation
        {
            Name = roleDto.Name,
            Description = roleDto.Description
        };
        await _keycloakAdminService.AssignRoleToUserAsync(userId, role);
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
        var attributes = new Dictionary<string, List<string>>
        {
            { "tenant_roles", roles.ToList() }
        };

        if (!string.IsNullOrEmpty(user.OrganizationId))
        {
            attributes.Add("tenant_id", new List<string> { user.OrganizationId });
        }
        else
        {
            _logger.LogWarning("User {UserName} (Keycloak ID: {KeycloakUserId}) is missing OrganizationId. tenant_id attribute will not be synced.", user.UserName, user.KeycloakUserId);
        }

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
