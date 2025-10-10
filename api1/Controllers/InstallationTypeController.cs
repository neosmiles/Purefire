using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api1.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ProtectedResource("res:installations")]
  //  [Authorize]
    public class InstallationTypeController : ControllerBase
    {
        // add methods for vehicle-related operations
        [HttpGet]
        [ProtectedResource("res:installationType", "scopes:read")]
        public IActionResult GetInstallationType()
        {
            // Logic to get the vehicle information
            return Ok(new { make = "Toyota", model = "Camry", year = 2020 });
        }

        

    }
}