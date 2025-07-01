using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Purefire.Auth.Models;
using Purefire.Auth.Services;

namespace api1.Controllers;

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

    // Role management endpoints
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
    {
        try
        {
            var roles = await appUserService.GetRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("roles/{roleName}")]
    public async Task<ActionResult<RoleDto>> GetRole(string roleName)
    {
        try
        {
            var role = await appUserService.GetRoleByNameAsync(roleName);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving role {RoleName}", roleName);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("roles")]
    public async Task<ActionResult> CreateRole(RoleDto role)
    {
        try
        {
            await appUserService.CreateRoleAsync(role);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating role");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{userId}/roles")]
    public async Task<ActionResult> AssignRoleToUser(string userId, RoleDto role)
    {
        try
        {
            await appUserService.AssignRoleToUserAsync(userId, role);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning role to user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }


}
