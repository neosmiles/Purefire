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
    public async Task<ActionResult<IEnumerable<OrganizationResponse>>> GetOrganizations()
    {
        try
        {
            var organizations = await keycloakAdmin.GetOrganizationsAsync();
            var response = organizations.Select(org => new OrganizationResponse
            {
                Id = org.Id,
                Name = org.Name,
                Description = org.Description,
                Enabled = org.Enabled,
                Domains = org.Domains?.Select(domain => domain.Name).ToList()
            });
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organizations");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{organizationId}")]
    public async Task<ActionResult<OrganizationResponse>> GetOrganization(string organizationId)
    {
        try
        {
            var organization = await keycloakAdmin.GetOrganizationByIdAsync(organizationId);
            if (organization == null || string.IsNullOrEmpty(organization.Id))
            {
                return NotFound($"Organization with ID '{organizationId}' not found.");
            }

            var response = new OrganizationResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description,
                Enabled = organization.Enabled,
                Domains = organization.Domains?.Select(domain => domain.Name).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("by-name/{organizationName}")]
    public async Task<ActionResult<OrganizationResponse>> GetOrganizationByName(string organizationName)
    {
        try
        {
            var organizations = await keycloakAdmin.GetOrganizationsAsync();
            var organization = organizations?.FirstOrDefault(org =>
                string.Equals(org.Name, organizationName, StringComparison.OrdinalIgnoreCase));

            if (organization == null || string.IsNullOrEmpty(organization.Id))
            {
                return NotFound($"Organization with name '{organizationName}' not found.");
            }

            var response = new OrganizationResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description,
                Enabled = organization.Enabled,
                Domains = organization.Domains?.Select(domain => domain.Name).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization {OrganizationName}", organizationName);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationResponse>> CreateOrganization([FromBody] CreateOrganizationDto createOrganizationDto)
    {
        try
        {
            var organizationRepresentation = new OrganizationRepresentation
            {
                Name = createOrganizationDto.Name,
                Description = createOrganizationDto.Description,
                Domains = createOrganizationDto.Domains?.Select(domain => new OrganizationDomainRepresentation { Name = domain }).ToList()
            };

            var result = await keycloakAdmin.CreateOrganizationAsync(organizationRepresentation);
            if (!result)
            {
                return BadRequest("Failed to create organization");
            }

            var organizationResult = await GetOrganizationByName(createOrganizationDto.Name!);
            return organizationResult;


        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating organization");
            return StatusCode(500, "Internal server error");
        }
    }


    public class CreateOrganizationDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? Domains { get; set; }
    }

    public class OrganizationResponse
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? Enabled { get; set; }
        public List<string>? Domains { get; set; }
    }
}