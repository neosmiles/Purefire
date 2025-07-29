using System.Security.Claims;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Sdk;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
namespace api2.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {

        #region Keycloak
        var protectionClient = "protection";

        // Configure Keycloak authentication as the primary scheme
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // .AddKeycloakWebApi(configuration); // This line is commented out to avoid conflicts with the next line
            //  because it conflicts with the next line where I added the JwtBearerEvents to handle roles mapping for Keycloak clients
            .AddKeycloakWebApi(configuration, options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
                        {
                            var realmRoles = context.Principal.FindFirst("realm_access")?.Value;
                            if (!string.IsNullOrEmpty(realmRoles))
                            {
                                var parsed = JObject.Parse(realmRoles);
                                var roles = parsed["roles"]?.ToObject<List<string>>();
                                if (roles != null)
                                {
                                    foreach (var role in roles)
                                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                                }
                            }
                        }

                        return Task.CompletedTask;
                    }
                };

                // Ensure the role claim type is correctly mapped
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            });

        // Register a named HttpClient called "protection" for general use
        // This is a standard HttpClient that can be injected anywhere in your application
        // when you need to make authenticated calls to protected resources
        services.AddHttpClient("protection")
            // Attach the same token handler to automatically include bearer tokens in requests
            .AddClientCredentialsTokenHandler(protectionClient);


        services.AddDistributedMemoryCache();
        services
            .AddClientCredentialsTokenManagement()
            .AddClient(
                protectionClient,
                client =>
                {
                    var options = configuration.GetKeycloakOptions<KeycloakProtectionClientOptions>()!;

                    client.ClientId = options.Resource;
                    client.ClientSecret = options.Credentials.Secret;
                    client.TokenEndpoint = options.KeycloakTokenEndpoint;
                });

        #endregion



        return services;
    }
}