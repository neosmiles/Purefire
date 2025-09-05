using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace api3.Attributes;

/// <summary>
/// Custom authorization attribute that checks Keycloak UMA permissions using RPT
/// </summary>
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string Resource { get; }
    public string Scope { get; }

    public RequirePermissionAttribute(string resource, string scope)
    {
        Resource = resource;
        Scope = scope;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated first
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();

            var accessToken = GetAccessTokenFromRequest(context.HttpContext.Request);
            if (string.IsNullOrEmpty(accessToken))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var hasPermission = await CheckPermissionAsync(httpClientFactory, configuration, accessToken, Resource, Scope);

            if (!hasPermission)
            {
                context.Result = new ForbidResult($"Insufficient permissions. Required: {Resource}#{Scope}");
                return;
            }
        }
        catch (Exception)
        {
            context.Result = new ForbidResult("Permission check failed");
        }
    }

    private string GetAccessTokenFromRequest(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length);
        }
        return string.Empty;
    }

    private async Task<bool> CheckPermissionAsync(IHttpClientFactory httpClientFactory, IConfiguration configuration, string accessToken, string resource, string scope)
    {
        try
        {
            var realm = configuration["Keycloak:realm"]!;
            var authServerUrl = configuration["Keycloak:auth-server-url"]!;
            var clientId = configuration["Keycloak:resource"]!;

            var httpClient = httpClientFactory.CreateClient();

            var tokenEndpoint = $"{authServerUrl}/realms/{realm}/protocol/openid_connect/token";

            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:uma-ticket",
                ["audience"] = clientId,
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
}
