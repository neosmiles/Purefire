using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Purefire.Auth.Data;
using Purefire.Auth.Models;
using System.Security.Claims;

namespace Purefire.Auth.Services;

public class UserSyncService(
    AuthDbContext context,
    UserManager<AppUser> userManager,
    ILogger<UserSyncService> logger) : IUserSyncService
{
    public async Task<AppUser?> SyncUserFromClaimsAsync(ClaimsPrincipal principal)
    {
        try
        {
            // Extract user information from JWT claims
            // var keycloakUserId = principal.FindFirst("sub")?.Value;
            // var email = principal.FindFirst("email")?.Value;
            // var username = principal.FindFirst("preferred_username")?.Value;
            // var firstName = principal.FindFirst("given_name")?.Value;
            // var lastName = principal.FindFirst("family_name")?.Value;
            // var emailVerified = principal.FindFirst("email_verified")?.Value;
            // var organizationId = ExtractOrganizationId(principal);

            // if (string.IsNullOrEmpty(keycloakUserId))
            // {
            //     logger.LogWarning("No subject claim found in JWT token");
            //     return null;
            // }

            // Extract user information from JWT claims
            var keycloakUserId = principal.FindFirst("sub")?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var email = principal.FindFirst("email")?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            var username = principal.FindFirst("preferred_username")?.Value;

            var firstName = principal.FindFirst("given_name")?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value;

            var lastName = principal.FindFirst("family_name")?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value;

            var emailVerified = principal.FindFirst("email_verified")?.Value;
            var organizationId = ExtractOrganizationId(principal);

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                logger.LogWarning("No subject claim found in JWT token");
                return null;
            }

            // Try to find existing user by KeycloakUserId
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId);

            if (existingUser != null)
            {
                // Check if any properties have changed
                bool userUpdated = false;

                if (existingUser.OrganizationId != organizationId)
                {
                    existingUser.OrganizationId = organizationId;
                    userUpdated = true;
                }

                if (existingUser.Email != email)
                {
                    existingUser.Email = email;
                    userUpdated = true;
                }

                if (existingUser.UserName != username)
                {
                    existingUser.UserName = username;
                    userUpdated = true;
                }

                if (existingUser.FirstName != firstName)
                {
                    existingUser.FirstName = firstName;
                    userUpdated = true;
                }

                if (existingUser.LastName != lastName)
                {
                    existingUser.LastName = lastName;
                    userUpdated = true;
                }

                if (emailVerified != null && existingUser.EmailConfirmed != bool.Parse(emailVerified))
                {
                    existingUser.EmailConfirmed = bool.Parse(emailVerified);
                    userUpdated = true;
                }


                // Always update last login time
                existingUser.LastLoginAt = DateTime.UtcNow;

                await userManager.UpdateAsync(existingUser);

                if (userUpdated)
                {
                    logger.LogInformation("Updated properties for user {UserId}", existingUser.Id);
                }

                return existingUser;
            }

            // Create new user if not found
            var newUser = new AppUser
            {
                KeycloakUserId = keycloakUserId,
                UserName = username ?? email ?? $"user_{keycloakUserId}",
                Email = email,
                EmailConfirmed = bool.Parse(emailVerified!),
                FirstName = firstName,
                LastName = lastName,
                OrganizationId = organizationId,
                Enabled = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing user from JWT claims");
            return null;
        }
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
}
