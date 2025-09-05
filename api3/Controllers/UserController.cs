using api3.Services.IServices;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api3.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class UserController(IAppUserService appUserService, ILogger<UserController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserRepresentation>>> GetUsers()
    {
        try
        {
            var users = await appUserService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving Keycloak users");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserRepresentation>> GetUser(string id)
    {
        try
        {
            var user = await appUserService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving keycloak user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserRepresentation>> CreateUser(string tenantId, UserRepresentation userDto)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return BadRequest("Tenant header is required.");
        }

        try
        {
            var createdUser = await appUserService.CreateUserAsync(userDto, organizationId: userDto.Attributes?.AdditionalData?.ContainsKey("organization") == true ? userDto.Attributes.AdditionalData["organization"]?.ToString() : null);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Organization") && ex.Message.Contains("not found"))
        {
            logger.LogWarning(ex, "Organization not found when creating user for tenant {TenantId}. User creation may have succeeded but organization assignment failed.", tenantId);
            return BadRequest($"Organization assignment failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid operation when creating keycloak user for tenant {TenantId}", tenantId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating keycloak user for tenant {TenantId}", tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UserRepresentation userDto)
    {
        try
        {
            var existingUser = await appUserService.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            await appUserService.UpdateUserAsync(id, userDto);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating keycloak user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var user = await appUserService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await appUserService.DeleteUserAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting keycloak user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }



    // --- Realm role (Keycloak) management endpoints ---

    [HttpPost("realm-roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRealmRole([FromBody] AspNetRoleRequestDto roleDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ok = await appUserService.CreateRealmRoleAsync(roleDto.RoleName);
        if (ok) return Ok($"Role '{roleDto.RoleName}' created in Keycloak.");
        return BadRequest("Failed to create role in Keycloak.");
    }

    [HttpGet("realm-roles")]
    public async Task<IActionResult> GetAllRealmRoles()
    {
        var roles = await appUserService.GetAllRealmRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{userId}/realm-roles")]
    public async Task<IActionResult> GetUserRealmRoles(string userId)
    {
        var user = await appUserService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();
        var roles = await appUserService.GetUserRealmRolesAsync(user.Id ?? string.Empty);
        return Ok(roles);
    }

    [HttpPost("{userId}/realm-roles")]
    public async Task<IActionResult> AssignRealmRoleToUser(string userId, [FromBody] AspNetRoleRequestDto roleDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var user = await appUserService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();
        var ok = await appUserService.AssignRealmRoleToUserAsync(user.Id ?? string.Empty, roleDto.RoleName);
        if (ok) return Ok($"Role '{roleDto.RoleName}' assigned to user.");
        return BadRequest("Failed to assign role in Keycloak.");
    }

    [HttpDelete("{userId}/realm-roles")]
    public async Task<IActionResult> RemoveRealmRoleFromUser(string userId, [FromBody] AspNetRoleRequestDto roleDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var user = await appUserService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();
        var ok = await appUserService.RemoveRealmRoleFromUserAsync(user.Id ?? string.Empty, roleDto.RoleName);
        if (ok) return Ok($"Role '{roleDto.RoleName}' removed from user.");
        return BadRequest("Failed to remove role in Keycloak.");
    }

    // DTO used by the role endpoints
    public record AspNetRoleRequestDto(string RoleName);
}
