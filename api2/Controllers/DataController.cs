using api2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Duende.AccessTokenManagement;

namespace api2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;
        private readonly ExampleApiCallService _exampleApiCallService;
        private readonly IClientCredentialsTokenManagementService _tokenService;

        public DataController(ILogger<DataController> logger, ExampleApiCallService exampleApiCallService, IClientCredentialsTokenManagementService tokenService)
        {
            _exampleApiCallService = exampleApiCallService;
            _tokenService = tokenService;
            _logger = logger;
        }

        /* /// <summary>
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
            
        } */

        /// <summary>
        /// Example endpoint to demonstrate API call service
        /// </summary>
        [AllowAnonymous]
        [HttpGet("example-api-call")]
        public async Task<IActionResult> ExampleApiCall()
        {
            var result = await _exampleApiCallService.CallApiAsync();
            return Ok(result);
        }

        /// <summary>
        /// Example endpoint to demonstrate API call service to a protected role
        /// </summary>
        [AllowAnonymous]
        [HttpGet("example-api-call-to-protected-role")]
        public async Task<IActionResult> ExampleApiCallToProtectedRole()
        {
            var result = await _exampleApiCallService.CallApiRoleAsync();
            return Ok(result);
        }

        /// <summary>
        /// Example endpoint to demonstrate API call service to a protected scope
        /// </summary>
        [AllowAnonymous]
        [HttpGet("example-api-call-to-protected-scope")]
        public async Task<IActionResult> ExampleApiCallToProtectedScope()
        {
            var result = await _exampleApiCallService.CallApiScopedAsync();
            return Ok(result);
        }


        /// <summary>
        /// Example endpoint to demonstrate token retrieval
        /// </summary>
        [AllowAnonymous]
        [HttpGet("example-token-retrieval")]
        public async Task<IActionResult> ExampleTokenRetrieval()
        {
            var token = await _tokenService.GetAccessTokenAsync("protection");
            return Ok(new { Token = token });
        }

        /// <summary>
        /// Example endpoint to demonstrate API call with token
        /// </summary>
        [AllowAnonymous]
        [HttpGet("example-api-call-with-token")]
        public async Task<IActionResult> ExampleApiCallWithToken()
        {
            var token = await _tokenService.GetAccessTokenAsync("protection");
            //call this API http://localhost:5007/Secured with the token  direct httpclient
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            var result = await httpClient.GetAsync("http://localhost:5007/Secured");
            if (result.IsSuccessStatusCode)
            {
                var content = await result.Content.ReadAsStringAsync();
                return Ok(content);
            }
            return BadRequest($"Error: {result.StatusCode}");
        }



    }
}