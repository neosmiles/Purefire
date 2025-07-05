using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Purefire.Auth.Services;

namespace Purefire.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class UserSyncController(IUserSyncService userSyncService, ILogger<UserSyncController> logger) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> SyncCurrentUser()
    {
        try
        {
            var syncedUser = await userSyncService.SyncUserFromClaimsAsync(User);

            if (syncedUser != null)
            {
                return Ok(new
                {
                    message = "User synchronized successfully"
                });
            }

            return BadRequest(new { message = "Failed to sync user" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing user");
            return StatusCode(500, new { message = "Internal server error during user sync" });
        }
    }

    
}