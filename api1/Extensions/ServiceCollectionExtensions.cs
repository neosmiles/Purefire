using System.Security.Claims;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Sdk;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
namespace api1.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {

        #region Keycloak
        var protectionClient = "protection";

        // Configure Keycloak authentication as the primary scheme
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddKeycloakWebApi(configuration);

        // Let Keycloak handle authorization entirely
        services.AddAuthorization()
            .AddKeycloakAuthorization()
            .AddAuthorizationServer(configuration);

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