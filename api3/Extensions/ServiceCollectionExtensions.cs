using api3.Data;
using api3.Models;
using api3.Services;
using api3.Services.IServices;
using Finbuckle.MultiTenant;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Kiota;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KeycloakAdminClientOptions = Keycloak.AuthServices.Sdk.KeycloakAdminClientOptions;

namespace api3.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServerConnection")));

        #region finbuckle-multitenant
        services.AddDbContext<TenantContext>(options =>
            options.UseInMemoryDatabase("TenantConnection"));


        //  Add Finbuckle.MultiTenant, pointing at DataContext as the EF store:
        services.AddMultiTenant<TenantInfo>()
            .WithEFCoreStore<TenantContext, TenantInfo>()
            // .WithHeaderStrategy("X-Tenant-ID")
            .WithClaimStrategy("organization")
            //.WithHostStrategy()          // subdomain
            // .WithRouteStrategy("{tenantId}")
            .WithStaticStrategy("lg");
        #endregion



        services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        #region Keycloak
        var adminSection = "KeycloakAdmin";

        var adminClient = "admin-api";
        var protectionClient = "protection";

        // Configure Keycloak authentication as the primary scheme
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddKeycloakWebApi(configuration);


        // Register our custom permission-based authorization
        //services.AddPermissionsAuthorization();

        // Still add Keycloak authorization for backward compatibility
        //services.AddKeycloakAuthorization();


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
        services.AddScoped<IUserSyncService, UserSyncService>();
        services.AddScoped<IKeycloakAdminService, KeycloakAdminService>();
        services.AddMemoryCache();

        return services;
    }
}