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
            var (success, token) = await _authService.AuthenticateUserAsync(request.Username, request.Password);

            if (!success)
                return Unauthorized();

            return Ok(new TokenResponse { Token = token });
        }

        /// <summary>
        /// Authenticates a client application and returns a JWT token
        /// </summary>
        /// <param name="request">The client credentials</param>
        /// <returns>A JWT token if authentication is successful</returns>
        /// <response code="200">Returns the JWT token</response>
        /// <response code="401">If the credentials are invalid</response>
        [HttpPost("client")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ClientAuth([FromBody] ClientAuthRequest request)
        {
            var (success, token) = await _authService.AuthenticateClientAsync(request.ClientId, request.ClientSecret);

            if (!success)
                return Unauthorized();

            return Ok(new TokenResponse { Token = token });
        }
    }

    /// <summary>
    /// Request model for user login
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// The username of the user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password of the user
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    /// Request model for client authentication
    /// </summary>
    public class ClientAuthRequest
    {
        /// <summary>
        /// The client ID
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The client secret
        /// </summary>
        public string ClientSecret { get; set; }
    }

    /// <summary>
    /// Response model containing the JWT token
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// The JWT token
        /// </summary>
        public string Token { get; set; }
    }
}