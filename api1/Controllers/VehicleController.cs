using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleController : ControllerBase
    {
        // add methods for vehicle-related operations
        [HttpGet("info")]
        [ProtectedResource("vehicle", "vehicle:read")]
        public IActionResult GetVehicleInfo()
        {
            // Logic to get the vehicle information
            return Ok(new { make = "Toyota", model = "Camry", year = 2020 });
        }

        [HttpPost("transfer")]
        [ProtectedResource("vehicle", "vehicle:write")]
        public IActionResult TransferVehicle()
        {
            // Logic to transfer vehicle
            return Ok(new { success = true, message = "Vehicle transfer completed successfully." });
        }

    }
}