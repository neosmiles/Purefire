using api3.Data;
using api3.Models;
using api3.Services;
using api3.Services.IServices;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Kiota;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using KeycloakAdminClientOptions = Keycloak.AuthServices.Sdk.KeycloakAdminClientOptions;

namespace api3.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServerConnection")));


        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddKeycloakWebApi(configuration);

        // Let Keycloak handle authorization entirely
        services.AddAuthorization()
            .AddKeycloakAuthorization()
            .AddAuthorizationServer(configuration);




        #region Keycloak
        var adminSection = "KeycloakAdmin";

        var adminClient = "admin-api";
        var protectionClient = "protection";




        // Register Kiota-generated HttpClient for Keycloak Admin API operations
        // This client handles admin tasks like user management, realm configuration, etc.
        services.AddKiotaKeycloakAdminHttpClient(configuration, keycloakClientSectionName: adminSection)
            // Automatically handle OAuth2 client credentials flow for admin API authentication
            .AddClientCredentialsTokenHandler(adminClient);


        // Register HttpClient for Keycloak Protection API (UMA/resource protection)
        // Used for managing protected resources and permissions
        services.AddKeycloakProtectionHttpClient(
            configuration, keycloakClientSectionName: KeycloakProtectionClientOptions.Section
        )
        // Automatically handle OAuth2 client credentials flow for protection API authentication
        .AddClientCredentialsTokenHandler(protectionClient);


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
                adminClient,
                client =>
                {
                    var options = configuration.GetKeycloakOptions<KeycloakAdminClientOptions>(adminSection)!;

                    client.ClientId = options.Resource;
                    client.ClientSecret = options.Credentials.Secret;
                    client.TokenEndpoint = options.KeycloakTokenEndpoint;
                }
            )
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

        // Add Auth Service
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IKeycloakAdminService, KeycloakAdminService>();

        return services;
    }
}