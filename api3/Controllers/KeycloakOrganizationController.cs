using api3.Services.IServices;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Organizations.Item.Members.InviteUser;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace api3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KeycloakOrganizationController(IKeycloakAdminService keycloakAdmin, ILogger<KeycloakOrganizationController> logger) : ControllerBase
{
    [HttpPost("users/{userId}/organizations/{organizationId}")]
    public async Task<IActionResult> AddUserToOrganization(string userId, string organizationId)
    {
        try
        {
            var result = await keycloakAdmin.AddUserToOrganizationAsync(userId, organizationId);
            if (!result)
            {
                return BadRequest("Failed to add user to organization");
            }
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation when adding user {UserId} to organization {OrganizationId}", userId, organizationId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding user {UserId} to organization {OrganizationId}", userId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{organizationId}/invite")]
    public async Task<IActionResult> InviteUserToOrganization(string organizationId, [FromBody] InviteUserPostRequestBody request)
    {
        try
        {
            var result = await keycloakAdmin.InviteUserToOrganizationAsync(organizationId, request);
            if (!result)
            {
                return BadRequest("Failed to invite user to organization");
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inviting user to organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("users/{userId}/organizations/{organizationId}")]
    public async Task<IActionResult> RemoveUserFromOrganization(string userId, string organizationId)
    {
        try
        {
            var result = await keycloakAdmin.RemoveUserFromOrganizationAsync(userId, organizationId);
            if (!result)
            {
                return BadRequest("Failed to remove user from organization");
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing user {UserId} from organization {OrganizationId}", userId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationRepresentation>>> GetOrganizations()
    {
        try
        {
            var organizations = await keycloakAdmin.GetOrganizationsAsync();
            return Ok(organizations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organizations");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{organizationId}")]
    public async Task<ActionResult<OrganizationRepresentation>> GetOrganization(string organizationId)
    {
        try
        {
            var organization = await keycloakAdmin.GetOrganizationByIdAsync(organizationId);
            if (organization == null || string.IsNullOrEmpty(organization.Id))
            {
                return NotFound($"Organization with ID '{organizationId}' not found.");
            }
            return Ok(organization);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization(OrganizationRepresentation organizationRepresentation)
    {
        try
        {
            var result = await keycloakAdmin.CreateOrganizationAsync(organizationRepresentation);
            if (!result)
            {
                return BadRequest("Failed to create organization");
            }
            return CreatedAtAction(nameof(GetOrganization), new { organizationId = organizationRepresentation.Id }, organizationRepresentation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating organization");
            return StatusCode(500, "Internal server error");
        }
    }

    /* [HttpGet("debug/organizations")]
    public async Task<ActionResult> DebugOrganizations()
    {
        try
        {
            logger.LogInformation("Fetching all organizations for debugging...");
            var organizations = await keycloakAdmin.GetOrganizationsAsync();

            var debugInfo = new
            {
                TotalCount = organizations.Count(),
                Organizations = organizations.Select(org => new
                {
                    Id = org.Id,
                    Name = org.Name,
                    Description = org.Description
                }).ToList()
            };

            logger.LogInformation("Found {Count} organizations: {Organizations}",
                debugInfo.TotalCount,
                string.Join(", ", debugInfo.Organizations.Select(o => $"{o.Name} (ID: {o.Id})")));

            return Ok(debugInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error debugging organizations");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("debug/organization/{organizationId}/check")]
    public async Task<ActionResult> CheckOrganizationExists(string organizationId)
    {
        try
        {
            logger.LogInformation("Checking if organization '{OrganizationId}' exists...", organizationId);

            var organization = await keycloakAdmin.GetOrganizationByIdAsync(organizationId);
            var exists = organization != null && !string.IsNullOrEmpty(organization.Id);

            var result = new
            {
                OrganizationId = organizationId,
                Exists = exists,
                Organization = exists ? new
                {
                    Id = organization?.Id,
                    Name = organization?.Name,
                    Description = organization?.Description
                } : null
            };

            logger.LogInformation("Organization '{OrganizationId}' exists: {Exists}", organizationId, exists);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("debug/config")]
    public ActionResult DebugConfig(IConfiguration config)
    {
        try
        {
            var configInfo = new
            {
                Realm = config["Keycloak:realm"],
                AuthServerUrl = config["Keycloak:auth-server-url"],
                AdminRealm = config["KeycloakAdmin:realm"],
                AdminAuthServerUrl = config["KeycloakAdmin:auth-server-url"]
            };

            logger.LogInformation("Keycloak configuration: Realm={Realm}, AuthServer={AuthServer}, AdminRealm={AdminRealm}",
                configInfo.Realm, configInfo.AuthServerUrl, configInfo.AdminRealm);

            return Ok(configInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting configuration info");
            return StatusCode(500, "Internal server error");
        }
    } */
}
