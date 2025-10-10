    using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;

namespace api3.Extensions;

public static class SwaggerExtensions
{
    public static void AddApplicationSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(
            (document, sp) =>
            {
                var keycloakOptions = sp.GetRequiredService<
                        IOptionsMonitor<KeycloakAuthenticationOptions>
                    >()
                    ?.Get(JwtBearerDefaults.AuthenticationScheme)!;

                document.Title = "Purefire API Core";

                // Add OpenID Connect security scheme (existing)
                document.AddSecurity(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    [],
                    new OpenApiSecurityScheme
                    {
                        Type = OpenApiSecuritySchemeType.OpenIdConnect,
                        OpenIdConnectUrl = "https://keycloak.etraffika.com.ng/realms/etraffika/.well-known/openid-configuration" //keycloakOptions.OpenIdConnectUrl,
                    }
                );

                // Add JWT Bearer security scheme for manual token input
                document.AddSecurity(
                    JwtBearerDefaults.AuthenticationScheme,
                    [],
                    new OpenApiSecurityScheme
                    {
                        Type = OpenApiSecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Enter your JWT token in the text input below.\n\nExample: \"your-jwt-token-here\""
                    }
                );

                // Add operation processors for both schemes
                document.OperationProcessors.Add(
                    new OperationSecurityScopeProcessor(OpenIdConnectDefaults.AuthenticationScheme)
                );
                
                document.OperationProcessors.Add(
                    new OperationSecurityScopeProcessor(JwtBearerDefaults.AuthenticationScheme)
                );
            }
        );
    }

    public static void UseApplicationSwaggerSettings(
        this SwaggerUiSettings ui,
        IConfiguration configuration
    )
    {
        //var keycloakOptions = configuration.GetKeycloakOptions<KeycloakAuthenticationOptions>("Keycloak:Default")!;
        var keycloakOptions = configuration.GetKeycloakOptions<KeycloakAuthenticationOptions>()!;


        ui.OAuth2Client = new OAuth2ClientSettings
        {
            ClientId = keycloakOptions.Resource,
            ClientSecret = keycloakOptions?.Credentials?.Secret,
        };
    }
}