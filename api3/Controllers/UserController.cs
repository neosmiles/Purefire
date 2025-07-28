using api3.Models;
using api3.Services.IServices;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api3.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class UserController(IAppUserService appUserService, ILogger<UserController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
    {
        try
        {
            var users = await appUserService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving app users");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppUser>> GetUser(string id)
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
            logger.LogError(ex, "Error retrieving app user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("keycloakuser/{id}")]
    public async Task<ActionResult<UserRepresentation>> GetKeycloakUser(string id)
    {
        try
        {
            var user = await appUserService.GetUserByKeycloakUserIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving app user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpPost]
    public async Task<ActionResult<AppUser>> CreateUser(string tenantId, AppUserCreateDto userDto)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return BadRequest("Tenant header is required.");
        }

        try
        {
            var newUser = new AppUser
            {
                UserName = userDto.Username,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
            };

            var createdUser = await appUserService.CreateUserAsync(newUser, userDto.Password, userDto.OrganizationId);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Organization") && ex.Message.Contains("not found"))
        {
            // Handle organization not found errors specifically
            logger.LogWarning(ex, "Organization not found when creating user for tenant {TenantId}. User creation may have succeeded but organization assignment failed.", tenantId);
            return BadRequest($"Organization assignment failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            // Handle other known operational exceptions
            logger.LogError(ex, "Invalid operation when creating app user for tenant {TenantId}", tenantId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating app user for tenant {TenantId}", tenantId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, AppUserUpdateDto userDto)
    {
        try
        {
            var existingUser = await appUserService.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Update user properties
            existingUser.UserName = userDto.Username ?? existingUser.UserName;
            existingUser.Email = userDto.Email ?? existingUser.Email;
            existingUser.FirstName = userDto.FirstName ?? existingUser.FirstName;
            existingUser.LastName = userDto.LastName ?? existingUser.LastName;
            existingUser.Enabled = userDto.Enabled ?? existingUser.Enabled;
            existingUser.EmailConfirmed = userDto.EmailConfirmed ?? existingUser.EmailConfirmed;

            await appUserService.UpdateUserAsync(existingUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating app user {UserId}", id);
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
            logger.LogError(ex, "Error deleting app user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }



    // --- New ASP.NET Core Identity Role Management Endpoints ---

    [HttpPost("identity-roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAspNetRole([FromBody] AspNetRoleRequestDto roleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            var result = await appUserService.CreateAspNetRoleAsync(roleDto.RoleName);
            if (result.Succeeded)
            {
                logger.LogInformation("ASP.NET Identity Role {RoleName} created successfully.", roleDto.RoleName);
                return Ok($"Role '{roleDto.RoleName}' created successfully.");
            }
            logger.LogWarning("Failed to create ASP.NET Identity Role {RoleName}. Errors: {Errors}", roleDto.RoleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Error creating ASP.NET Identity role: {RoleName}", roleDto.RoleName);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating ASP.NET Identity role: {RoleName}", roleDto.RoleName);
            return StatusCode(500, "Internal server error while creating ASP.NET Identity role.");
        }
    }

    [HttpGet("identity-roles")]
    [ProducesResponseType(typeof(IEnumerable<Microsoft.AspNetCore.Identity.IdentityRole>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAspNetRoles()
    {
        try
        {
            var roles = await appUserService.GetAllAspNetRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all ASP.NET Identity roles.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{userId}/identity-roles")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAspNetRoles(string userId)
    {
        try
        {
            // First, check if user exists to return a proper 404 if not.
            var user = await appUserService.GetUserByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User with ID {UserId} not found when trying to get ASP.NET Identity roles.", userId);
                return NotFound($"User with ID '{userId}' not found.");
            }
            var roles = await appUserService.GetUserAspNetRolesAsync(userId);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving ASP.NET Identity roles for user {UserId}.", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{userId}/identity-roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignAspNetRoleToUser(string userId, [FromBody] AspNetRoleRequestDto roleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            var result = await appUserService.AssignAspNetRoleToUserAsync(userId, roleDto.RoleName);
            if (result.Succeeded)
            {
                logger.LogInformation("Successfully assigned ASP.NET Identity role {RoleName} to user {UserId}.", roleDto.RoleName, userId);
                return Ok($"Role '{roleDto.RoleName}' assigned to user '{userId}'.");
            }
            // Check if failure was due to user not found to return 404
            if (result.Errors.Any(e => e.Description.Contains($"User with ID '{userId}' not found")))
            {
                logger.LogWarning("Attempted to assign role {RoleName} to non-existent user {UserId}.", roleDto.RoleName, userId);
                return NotFound($"User with ID '{userId}' not found.");
            }
            logger.LogWarning("Failed to assign ASP.NET Identity role {RoleName} to user {UserId}. Errors: {Errors}", roleDto.RoleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning ASP.NET Identity role {RoleName} to user {UserId}.", roleDto.RoleName, userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{userId}/identity-roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAspNetRoleFromUser(string userId, [FromBody] AspNetRoleRequestDto roleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            var result = await appUserService.RemoveAspNetRoleFromUserAsync(userId, roleDto.RoleName);
            if (result.Succeeded)
            {
                logger.LogInformation("Successfully removed ASP.NET Identity role {RoleName} from user {UserId}.", roleDto.RoleName, userId);
                return Ok($"Role '{roleDto.RoleName}' removed from user '{userId}'.");
            }
            if (result.Errors.Any(e => e.Description.Contains($"User with ID '{userId}' not found")))
            {
                logger.LogWarning("Attempted to remove role {RoleName} from non-existent user {UserId}.", roleDto.RoleName, userId);
                return NotFound($"User with ID '{userId}' not found.");
            }
            logger.LogWarning("Failed to remove ASP.NET Identity role {RoleName} from user {UserId}. Errors: {Errors}", roleDto.RoleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing ASP.NET Identity role {RoleName} from user {UserId}.", roleDto.RoleName, userId);
            return StatusCode(500, "Internal server error");
        }
    }
}
