using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using api3.Attributes;

namespace api3.Controllers;

[ApiController]
[Route("[controller]")]
public class SecuredController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        return Ok(new { message = "This is a secured endpoint powered by Neosmiles", user = User.Identity?.Name });
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

    /// <summary>
    /// Debug endpoint to see all user claims
    /// </summary>
    [HttpGet("debug")]
    [Authorize]
    public IActionResult GetDebugInfo()
    {
        var claims = User.Claims.Select(c => new
        {
            Type = c.Type,
            Value = c.Value,
            ValueType = c.ValueType,
            Issuer = c.Issuer
        }).ToList();

        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            Message = "Debug information",
            User = User.Identity?.Name,
            IsAuthenticated = User.Identity?.IsAuthenticated,
            AllClaims = claims,
            RoleClaims = roles,
            IsInAdminRole = User.IsInRole("Admin")
        });
    }

    /// <summary>
    /// Example endpoint using UMA permission check via RPT
    /// This requires the user to have 'read' permission on 'vehicle' resource
    /// </summary>
    [HttpGet("vehicles")]
    [RequirePermission("vehicle", "read")]
    public IActionResult GetVehicles()
    {
        return Ok(new
        {
            Message = "Vehicle data accessible via UMA permission",
            User = User.Identity?.Name,
            Data = new[] { "Vehicle 1", "Vehicle 2", "Vehicle 3" }
        });
    }

    /// <summary>
    /// Example endpoint requiring write permission on organization resource
    /// </summary>
    [HttpPost("organizations")]
    [RequirePermission("organization", "write")]
    public IActionResult CreateOrganization([FromBody] object data)
    {
        return Ok(new
        {
            Message = "Organization creation allowed via UMA permission",
            User = User.Identity?.Name
        });
    }
}