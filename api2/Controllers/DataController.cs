using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;

        public DataController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get data accessible by any authenticated user
        /// </summary>
        [HttpGet("public")]
        public IActionResult GetPublicData()
        {
            return Ok(new { Message = "This data is accessible by any authenticated user" });
        }

        /// <summary>
        /// Get data accessible only by users with Admin role
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAdminData()
        {
            return Ok(new { Message = "This data is accessible only by Admin users" });
        }

        /// <summary>
        /// Get data accessible only by service clients
        /// </summary>
        [HttpGet("service")]
        [Authorize(Policy = "ServiceClient")]
        public IActionResult GetServiceData()
        {
            return Ok(new { Message = "This data is accessible only by service clients" });
        }

        /// <summary>
        /// Get data accessible by both Admin users and service clients
        /// </summary>
        [HttpGet("admin-or-service")]
        [Authorize(Policy = "AdminOrService")]
        public IActionResult GetAdminOrServiceData()
        {
            return Ok(new { Message = "This data is accessible by Admin users and service clients" });
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
}