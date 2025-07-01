using Microsoft.AspNetCore.Identity;

namespace Purefire.Auth.Models;

public class AppUser : IdentityUser, ITenantEntity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Keycloak integration properties
    public string? KeycloakUserId { get; set; }  // Link to Keycloak user ID
    public string? OrganizationId { get; set; }  // Keycloak Organization (same as TenantId)
    public bool Enabled { get; set; } = true;
}
