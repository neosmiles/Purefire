using Finbuckle.MultiTenant;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Kiota;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Purefire.Auth.Data;
using Purefire.Auth.Models;
using Purefire.Auth.Services;
using System.Security.Claims;
using KeycloakAdminClientOptions = Keycloak.AuthServices.Sdk.KeycloakAdminClientOptions;

namespace Purefire.Auth.Extensions;

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

        // Add Identity
        /*  services.AddIdentity<ApplicationUser, IdentityRole>(options =>
         {
             // Password settings
             options.Password.RequireDigit = true;
             options.Password.RequireLowercase = true;
             options.Password.RequireUppercase = true;
             options.Password.RequireNonAlphanumeric = true;
             options.Password.RequiredLength = 8;

             // Lockout settings
             options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
             options.Lockout.MaxFailedAccessAttempts = 5;

             // User settings
             options.User.RequireUniqueEmail = true;
         })
         .AddEntityFrameworkStores<AuthDbContext>()
         .AddDefaultTokenProviders();

         // Add JWT Authentication
         services.AddAuthentication(options =>
         {
             options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
             options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
         })
         .AddJwtBearer(options =>
         {
             options.TokenValidationParameters = new TokenValidationParameters
             {
                 ValidateIssuer = true,
                 ValidateAudience = true,
                 ValidateLifetime = true,
                 ValidateIssuerSigningKey = true,
                 ValidIssuer = configuration["Jwt:Issuer"],
                 ValidAudience = configuration["Jwt:Audience"],
                 IssuerSigningKey = new SymmetricSecurityKey(
                     Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
             };
         }); */

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


        services.AddKiotaKeycloakAdminHttpClient(configuration, keycloakClientSectionName: adminSection)
           .AddClientCredentialsTokenHandler(adminClient);

        services
            .AddKeycloakProtectionHttpClient(
                configuration, keycloakClientSectionName: KeycloakProtectionClientOptions.Section
            )
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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IUserSyncService, UserSyncService>();
        services.AddScoped<IKeycloakAdminService, KeycloakAdminService>();
        services.AddMemoryCache();

        return services;
    }
}