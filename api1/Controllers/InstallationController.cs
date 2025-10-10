using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api1.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ProtectedResource("res:installations")]
    //[Authorize]
    public class InstallationController : ControllerBase
    {
        // add methods for vehicle-related operations
        [HttpGet]
        // [ProtectedResource("res:installations", "scopes:readscopes:update")]
        [ProtectedResource("res:installations", "scopes:delete")]
        public IActionResult GetInstallation()
        {
            // Logic to get the vehicle information
            return Ok(new { make = "Jenoptik", model = "Anpr", year = 2025, Country = "Congo Dc" , Reason = "this a God Admin test for delete"});
        }


        //[HttpDelete("delete/{HealthManagerInstallationId}")]
        // [ProtectedResource("res:installations", "scopes:delete")]
        //public IActionResult DeleteInstallation(string HealthManagerInstallationId)
        //{
        //    // Logic to get the vehicle information
        //    return Ok(new { make = "Jenoptik", model = "Anpr", year = 2025, Country = "Congo Dc" });
        //}
    }
}