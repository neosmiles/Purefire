using Keycloak.AuthServices.Sdk.Kiota.Admin.Admin.Realms.Item.Organizations.Item.Members.InviteUser;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Purefire.Auth.Services;

namespace api1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController(IKeycloakAdminService keycloakAdmin, ILogger<TenantController> logger) : ControllerBase
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding user {UserId} to organization {OrganizationId}", userId, organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("organizations/{organizationId}/invite")]
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

    [HttpGet("organizations")]
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

    [HttpGet("organizations/{organizationId}")]
    public async Task<ActionResult<OrganizationRepresentation>> GetOrganization(string organizationId)
    {
        try
        {
            var organization = await keycloakAdmin.GetOrganizationByIdAsync(organizationId);
            if (organization == null)
            {
                return NotFound();
            }
            return Ok(organization);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("organizations")]
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



}
