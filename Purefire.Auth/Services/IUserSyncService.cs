using Purefire.Auth.Models;
using System.Security.Claims;

namespace Purefire.Auth.Services;

public interface IUserSyncService
{
    /// <summary>
    /// Syncs a user from Keycloak JWT claims to local AppUser entity
    /// </summary>
    /// <param name="principal">ClaimsPrincipal from JWT token</param>
    /// <returns>The synced local AppUser</returns>
    Task<AppUser?> SyncUserFromClaimsAsync(ClaimsPrincipal principal);

    /// <summary>
    /// Extracts organization ID from JWT claims
    /// </summary>
    /// <param name="principal">ClaimsPrincipal from JWT token</param>
    /// <returns>Organization ID or null</returns>
    string? ExtractOrganizationId(ClaimsPrincipal principal);
}
