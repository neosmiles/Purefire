using api3.Data;
using api3.Models;
using api3.Services.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace api3.Services;

public class UserSyncService(
    AuthDbContext context,
    UserManager<AppUser> userManager,
    IMemoryCache cache,
    ILogger<UserSyncService> logger) : IUserSyncService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(4);

    public async Task<AppUser?> SyncUserFromClaimsAsync(ClaimsPrincipal principal)
    {
        try
        {
            // Extract user information from JWT claims
            var keycloakUserId = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                logger.LogWarning("No subject claim found in JWT token");
                return null;
            }

            // Check cache first
            var cacheKey = GetUserCacheKey(keycloakUserId);
            if (cache.TryGetValue(cacheKey, out AppUser? cachedUser))
            {
                return cachedUser;
            }

            // Extract all claims once
            var userClaims = ExtractUserClaims(principal);
            var organizationId = ExtractOrganizationId(principal);

            // Try to find existing user by KeycloakUserId with minimal data
            var existingUser = await context.Users
                .IgnoreQueryFilters()
                .AsNoTracking() // Read-only query for better performance
                .Select(u => new
                {
                    u.Id,
                    u.KeycloakUserId,
                    u.Email,
                    u.UserName,
                    u.FirstName,
                    u.LastName,
                    u.EmailConfirmed,
                    u.OrganizationId,
                    u.Enabled,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId);

            AppUser result;

            if (existingUser != null)
            {
                // Check if update is needed by comparing values
                var needsUpdate = DoesUserNeedUpdate(existingUser, userClaims, organizationId);

                if (needsUpdate)
                {
                    // Only fetch and update if changes detected
                    result = await UpdateExistingUserAsync(existingUser.Id, userClaims, organizationId);
                }
                else
                {
                    // Convert projection back to AppUser for caching
                    result = new AppUser
                    {
                        Id = existingUser.Id,
                        KeycloakUserId = existingUser.KeycloakUserId,
                        Email = existingUser.Email,
                        UserName = existingUser.UserName,
                        FirstName = existingUser.FirstName,
                        LastName = existingUser.LastName,
                        EmailConfirmed = existingUser.EmailConfirmed,
                        OrganizationId = existingUser.OrganizationId,
                        Enabled = existingUser.Enabled,
                        CreatedAt = existingUser.CreatedAt
                    };
                }


            }
            else
            {
                // Create new user
                result = await CreateNewUserAsync(keycloakUserId, userClaims, organizationId);
                if (result == null) return null;
            }

            // Cache the result with longer duration
            cache.Set(cacheKey, result, CacheExpiration);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing user from JWT claims");
            return null;
        }
    }

    private UserClaimsData ExtractUserClaims(ClaimsPrincipal principal)
    {
        return new UserClaimsData
        {
            Email = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value,
            Username = principal.FindFirst("preferred_username")?.Value,
            FirstName = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value,
            LastName = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value,
            EmailVerified = principal.FindFirst("email_verified")?.Value
        };
    }

    private bool DoesUserNeedUpdate(dynamic existingUser, UserClaimsData claims, string? organizationId)
    {
        return existingUser.Email != claims.Email ||
               existingUser.UserName != claims.Username ||
               existingUser.FirstName != claims.FirstName ||
               existingUser.LastName != claims.LastName ||
               claims.EmailVerified != null && existingUser.EmailConfirmed != bool.Parse(claims.EmailVerified);
        // Uncomment if you want to sync organization changes:
        // || existingUser.OrganizationId != organizationId;
    }

    private async Task<AppUser> UpdateExistingUserAsync(string userId, UserClaimsData claims, string? organizationId)
    {
        // Fetch only the user that needs updating
        var userToUpdate = await context.Users.FindAsync(userId);
        if (userToUpdate == null) return null;

        // Apply updates
        userToUpdate.Email = claims.Email;
        userToUpdate.UserName = claims.Username;
        userToUpdate.FirstName = claims.FirstName;
        userToUpdate.LastName = claims.LastName;
        userToUpdate.LastLoginAt = DateTime.UtcNow; // Update last login time

        if (claims.EmailVerified != null)
        {
            userToUpdate.EmailConfirmed = bool.Parse(claims.EmailVerified);
        }

        // Uncomment if syncing organization:
        // userToUpdate.OrganizationId = organizationId;

        await userManager.UpdateAsync(userToUpdate);
        logger.LogInformation("Updated properties for user {UserId}", userId);

        return userToUpdate;
    }

    private async Task<AppUser?> CreateNewUserAsync(string keycloakUserId, UserClaimsData claims, string? organizationId)
    {
        var newUser = new AppUser
        {
            KeycloakUserId = keycloakUserId,
            UserName = claims.Username ?? claims.Email ?? $"user_{keycloakUserId}",
            Email = claims.Email,
            EmailConfirmed = bool.Parse(claims.EmailVerified ?? "false"),
            FirstName = claims.FirstName,
            LastName = claims.LastName,
            OrganizationId = organizationId,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,

        };

        var result = await userManager.CreateAsync(newUser);
        if (result.Succeeded)
        {
            logger.LogInformation("Created new user {UserId} from Keycloak user {KeycloakUserId}",
                newUser.Id, keycloakUserId);
            return newUser;
        }

        logger.LogError("Failed to create user from Keycloak claims: {Errors}",
            string.Join(", ", result.Errors.Select(e => e.Description)));
        return null;
    }


    public string? ExtractOrganizationId(ClaimsPrincipal principal)
    {
        // Get all organization claims (Keycloak creates one claim per array element)
        var orgClaims = principal.FindAll("organization").Select(c => c.Value).ToList();

        if (orgClaims.Any())
        {
            return orgClaims.First(); // Return the first organization
        }

        // Fallback to other possible claim names
        return principal.FindFirst("org")?.Value
               ?? principal.FindFirst("organization_id")?.Value;
    }

    public void InvalidateUserCache(string keycloakUserId)
    {
        var cacheKey = GetUserCacheKey(keycloakUserId);
        cache.Remove(cacheKey);
        logger.LogDebug("Invalidated cache for user {KeycloakUserId}", keycloakUserId);
    }

    //public void InvalidateUserCacheByLocalId(string localUserId)
    //{
    //    // For cases where you only have the local user ID
    //    var cacheKey = $"user_sync_local_{localUserId}";
    //    cache.Remove(cacheKey);
    //    logger.LogDebug("Invalidated cache for local user {LocalUserId}", localUserId);
    //}

    private static string GetUserCacheKey(string keycloakUserId) => $"user_sync_{keycloakUserId}";
    private record UserClaimsData
    {
        public string? Email { get; init; }
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? EmailVerified { get; init; }
    }
}