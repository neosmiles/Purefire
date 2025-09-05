using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace api3.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private readonly string _realm = configuration["Keycloak:realm"]!;
    private readonly string _authServerUrl = configuration["Keycloak:auth-server-url"]!;
    private readonly string _clientId = configuration["Keycloak:resource"]!;

    /// <summary>
    /// Get RPT (Requesting Party Token) with user permissions
    /// </summary>
    [HttpGet("rpt")]
    public async Task<IActionResult> GetRptToken()
    {
        try
        {
            var accessToken = GetAccessTokenFromRequest();
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized("Access token is required");
            }

            var rptToken = await RequestRptTokenAsync(accessToken);
            var permissions = ExtractPermissionsFromToken(rptToken);

            return Ok(new
            {
                RptToken = rptToken,
                Permissions = permissions,
                Message = "RPT token retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Check if user has specific permission for a resource
    /// </summary>
    [HttpPost("check")]
    public async Task<IActionResult> CheckPermission([FromBody] PermissionCheckRequest request)
    {
        try
        {
            var accessToken = GetAccessTokenFromRequest();
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized("Access token is required");
            }

            var hasPermission = await CheckUserPermissionAsync(accessToken, request.Resource, request.Scope);

            return Ok(new
            {
                HasPermission = hasPermission,
                Resource = request.Resource,
                Scope = request.Scope,
                UserId = User.Identity?.Name
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get all permissions for the current user
    /// </summary>
    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        try
        {
            var accessToken = GetAccessTokenFromRequest();
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized("Access token is required");
            }

            var rptToken = await RequestRptTokenAsync(accessToken);
            var permissions = ExtractPermissionsFromToken(rptToken);

            return Ok(new
            {
                Name = User.Identity?.Name,
                Permissions = permissions,
                TotalPermissions = permissions.Count()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    private string GetAccessTokenFromRequest()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length);
        }
        return string.Empty;
    }

    private async Task<string> RequestRptTokenAsync(string accessToken)
    {
        var httpClient = httpClientFactory.CreateClient();

        var tokenEndpoint = $"{_authServerUrl}/realms/{_realm}/protocol/openid-connect/token";

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
            ["audience"] = _clientId
        };

        var content = new FormUrlEncodedContent(parameters);

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = content
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to get RPT token: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

        return tokenResponse?.AccessToken ?? string.Empty;
    }

    private async Task<bool> CheckUserPermissionAsync(string accessToken, string resource, string scope)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient();

            var tokenEndpoint = $"{_authServerUrl}/realms/{_realm}/protocol/openid_connect/token";

            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
                ["audience"] = _clientId,
                ["permission"] = $"{resource}#{scope}"
            };

            var content = new FormUrlEncodedContent(parameters);

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = content
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Previous implementation commented out as requested.
    /*
    private IEnumerable<PermissionInfo> ExtractPermissionsFromToken(string rptToken)
    {
        try
        {
            var parts = rptToken.Split('.');
            if (parts.Length != 3)
                return Enumerable.Empty<PermissionInfo>();

            var payload = parts[1];

            // Add padding if needed for Base64 decoding
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(jsonBytes);
            var tokenData = JsonConvert.DeserializeObject<RptTokenPayload>(json);

            return tokenData?.Authorization?.Permissions ?? Enumerable.Empty<PermissionInfo>();
        }
        catch
        {
            return Enumerable.Empty<PermissionInfo>();
        }
    }
    */

    // New implementation: only returns scopes from the RPT token.
    private IEnumerable<string> ExtractPermissionsFromToken(string rptToken)
    {
        try
        {
            var parts = rptToken.Split('.');
            if (parts.Length != 3)
                return Enumerable.Empty<string>();

            var payload = parts[1];

            // Add padding if needed for Base64 decoding
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(jsonBytes);
            var tokenData = JsonConvert.DeserializeObject<RptTokenPayload>(json);

            // Flatten all scopes from all permissions
            return tokenData?.Authorization?.Permissions?
                .SelectMany(p => p.Scopes)
                .Distinct()
                ?? Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public class PermissionCheckRequest
    {
        public string Resource { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
    }

    public class PermissionInfo
    {
        [JsonProperty("rsname")]
        public string ResourceName { get; set; } = string.Empty;

        [JsonProperty("rsid")]
        public string ResourceId { get; set; } = string.Empty;

        [JsonProperty("scopes")]
        public List<string> Scopes { get; set; } = new();
    }

    private class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private class RptTokenPayload
    {
        [JsonProperty("authorization")]
        public AuthorizationData? Authorization { get; set; }
    }

    private class AuthorizationData
    {
        [JsonProperty("permissions")]
        public List<PermissionInfo> Permissions { get; set; } = new();
    }
}
