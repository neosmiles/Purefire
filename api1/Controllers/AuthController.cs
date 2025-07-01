using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Purefire.Auth.Services;

namespace api1.Controllers
{
    /// <summary>
    /// Controller for handling authentication operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token
        /// </summary>
        /// <param name="request">The login credentials</param>
        /// <returns>A JWT token if authentication is successful</returns>
        /// <response code="200">Returns the JWT token</response>
        /// <response code="401">If the credentials are invalid</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var (success, token) = await _authService.LoginAsync(request.Email, request.Password);
            if (!success)
            {
                return Unauthorized(new { message = token });
            }

            return Ok(new { token });
        }

        /// <summary>
        /// Authenticates a client application and returns a JWT token
        /// </summary>
        /// <param name="request">The client credentials</param>
        /// <returns>A JWT token if authentication is successful</returns>
        /// <response code="200">Returns the JWT token</response>
        /// <response code="401">If the credentials are invalid</response>
        [HttpPost("login/client")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginClient([FromBody] ClientLoginRequest request)
        {
            var (success, token) = await _authService.LoginServiceClientAsync(request.ClientId, request.ClientSecret);
            if (!success)
            {
                return Unauthorized(new { message = token });
            }

            return Ok(new { token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var (success, message) = await _authService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }

        /// <summary>
        /// Creates a new client
        /// </summary>
        /// <param name="request">The client details</param>
        /// <returns>A message indicating the result of the operation</returns>
        /// <response code="200">Returns a success message</response>
        /// <response code="400">Returns an error message</response>
        [HttpPost("create-client")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
        {
            var (success, message) = await _authService.CreateClientAsync(request.ClientId, request.ClientSecret, request.Name, request.AllowedScopes);

            if (!success)
            {
                return BadRequest(message);
            }

            return Ok(message);
        }

        [HttpGet("Keycloaktest")]
        [Authorize]
        public IActionResult Getkeycloak()
        {
            // Add debugging information
            var user = HttpContext.User;
            var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var authScheme = HttpContext.User.Identity?.AuthenticationType;
            var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated ?? false;

            return Ok(new
            {
                message = "Authenticated successfully, hurry!!!",
                isAuthenticated = isAuthenticated,
                authenticationScheme = authScheme,
                userName = user.Identity?.Name,
                claims = claims
            });
        }

        [HttpGet("test-no-auth")]
        public IActionResult TestNoAuth()
        {
            return Ok(new { message = "This endpoint works without authentication", timestamp = DateTime.UtcNow });
        }

        [HttpGet("test-auth-info")]
        public IActionResult TestAuthInfo()
        {
            var user = HttpContext.User;
            var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();

            return Ok(new
            {
                message = "Auth info endpoint",
                isAuthenticated = user.Identity?.IsAuthenticated ?? false,
                authScheme = user.Identity?.AuthenticationType,
                userName = user.Identity?.Name,
                hasAuthHeader = !string.IsNullOrEmpty(authHeader),
                authHeaderPrefix = authHeader?.Split(' ').FirstOrDefault(),
                claimCount = user.Claims.Count(),
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Request model for creating a new client
    /// </summary>
    public class CreateClientRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string[] AllowedScopes { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Request model for user login
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// The email of the user
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The password of the user
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for client authentication
    /// </summary>
    public class ClientLoginRequest
    {
        /// <summary>
        /// The client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The client secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for user registration
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// The email of the user
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The password of the user
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// The first name of the user
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The last name of the user
        /// </summary>
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model containing the JWT token
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// The JWT token
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}