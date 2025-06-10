using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api3.Controllers;

[ApiController]
[Route("[controller]")]
public class SecuredController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        return Ok(new { message = "This is a secured endpoint", user = User.Identity?.Name });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminOnly()
    {
        return Ok(new { message = "This is an admin-only endpoint", user = User.Identity?.Name });
    }

    /// <summary>
    /// Get data with specific scope requirement
    /// </summary>
    [HttpGet("scoped")]
    [Authorize(Policy = "RequireApi2Scope")]
    public IActionResult GetScopedData()
    {
        return Ok(new { Message = "This data requires the api2 scope" });
    }
}