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
    public class FinanceController : ControllerBase
    {
        // add methods for finance-related operations
        [HttpGet("balance")]
        [ProtectedResource("finance", "finance:read")]
        public IActionResult GetBalance()
        {
            // Logic to get the balance
            return Ok(new { balance = 1000 });
        }

        [HttpPost("transfer")]
        [ProtectedResource("finance", "write")]
        public IActionResult TransferFunds()
        {
            // Logic to transfer funds
            return Ok(new { success = true, message = "Transfer completed successfully." });
        }

    }
}