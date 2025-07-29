# Purefire Authentication System - Implementation Guide

**Project**: Purefire Authentication System  
**Version**: 2.0  
**Date**: July 28, 2025  
**Status**: Refactored Implementation

## 📋 Overview

This document describes the implementation of a simplified authentication system built with ASP.NET Core and Keycloak. The refactored solution provides JWT-based authentication for both user scenarios and machine-to-machine (M2M) communication, with a clean microservices architecture.

## 🏗️ Architecture Overview

### Core Components

- **Keycloak**: External identity provider and token issuer
- **ASP.NET Identity**: Local user and role management (api3)
- **SQL Server**: User data persistence (api3)
- **JWT Bearer**: Token-based authentication (all APIs)
- **Client Credentials Flow**: Machine-to-machine authentication (api2)
- **JWT Events**: Custom role mapping from Keycloak tokens
- **Header-based Multi-tenancy**: Organization isolation via headers

### Service Architecture

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│    API2     │    │    API3     │    │  Keycloak   │
│ (M2M Focus) │◄──►│(User Focus) │◄──►│ (Identity)  │
│             │    │             │    │             │
└─────────────┘    └─────────────┘    └─────────────┘
      ▲                  ▲                    ▲
      │                  │                    │
      ▼                  ▼                    ▼
Client Credentials   ASP.NET Identity    JWT Tokens
  Flow (M2M)        + Role Mapping       + Roles
                    + Header Tenancy
```

### Data Flow

```
1. User Authentication: Client → Keycloak → JWT Token → API3 → ASP.NET Identity + Role Mapping
2. M2M Authentication: Service → Keycloak → JWT Token → API2 → Protected Resources
3. Multi-tenancy: All requests include OrganizationId header for tenant isolation
```

---

## 🔧 Technical Implementation

### 1. Project Structure

```
Purefire/
├── api2/                          # Machine-to-Machine API
│   ├── Controllers/               # M2M endpoints
│   │   └── DataController.cs      # Protected M2M operations
│   ├── Services/                  # M2M business logic
│   │   └── ExampleApiCallService.cs # Service-to-service calls
│   ├── Extensions/                # M2M configuration
│   │   └── ServiceCollectionExtensions.cs # Auth setup
│   └── Program.cs                 # M2M API configuration
├── api3/                          # User Management API
│   ├── Controllers/               # User endpoints
│   │   ├── SecuredController.cs   # Protected user endpoints
│   │   ├── UserController.cs      # User management
│   │   └── KeycloakOrganizationController.cs # Organization ops
│   ├── Data/                      # Database contexts
│   │   └── AuthDbContext.cs       # Identity context
│   ├── Models/                    # Entity models
│   │   ├── AppUser.cs             # User entity
│   │   └── UserDtos.cs            # Data transfer objects
│   ├── Services/                  # Business logic
│   │   ├── AppUserService.cs      # User operations
│   │   ├── KeycloakAdminService.cs # Keycloak integration
│   │   └── UserSyncService.cs     # User synchronization
│   ├── Extensions/                # Service configuration
│   │   ├── ServiceCollectionExtensions.cs # Auth setup
│   │   └── SwaggerExtensions.cs   # API documentation
│   └── Program.cs                 # Application configuration
└── Purefire.sln                  # Solution file
```

### 2. Key Dependencies

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
<PackageReference Include="Keycloak.AuthServices.Authentication" />
<PackageReference Include="Keycloak.AuthServices.Sdk" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
<PackageReference Include="Duende.AccessTokenManagement" />
<PackageReference Include="Newtonsoft.Json" />
```

### 3. Configuration Setup

#### appsettings.json

```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=localhost;Database=Purefire;Trusted_Connection=True;"
  },
  "Keycloak": {
    "realm": "dark-vader",
    "auth-server-url": "http://localhost:8080",
    "resource": "apple-private-client",
    "credentials": {
      "secret": "3J3xT5dQ7kIktZAhVdKFClPIAIyYk5wE"
    }
  },
  "KeycloakAdmin": {
    "realm": "master",
    "auth-server-url": "http://localhost:8080",
    "resource": "admin-api",
    "credentials": {
      "secret": "RWtUfVKWdFLhhKzihx1WopaJtpHk4c2x"
    }
  }
}
```

---

## 🔐 Authentication & Authorization

### 1. Service Registration (Program.cs)

#### API2 (M2M Focus)

```csharp
// Add authentication services
builder.Services.AddAuthServices(builder.Configuration);

// M2M-specific service registration
builder.Services.AddScoped<ExampleApiCallService>();

// Configure Client Credentials Token Management
services.AddClientCredentialsTokenManagement()
    .AddClient("protection", client =>
    {
        var options = configuration.GetKeycloakOptions<KeycloakProtectionClientOptions>()!;
        client.ClientId = options.Resource;
        client.ClientSecret = options.Credentials.Secret;
        client.TokenEndpoint = options.KeycloakTokenEndpoint;
    });
```

#### API3 (User/Tenant Focus)

```csharp
// Add authentication services
builder.Services.AddAuthServices(builder.Configuration);

// Configure ASP.NET Identity
services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// Configure Keycloak with JWT Events for role mapping
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
    });
```

### 2. JWT Token Flow & Role Mapping

#### JWT Events for Role Mapping

```csharp
.AddKeycloakWebApi(configuration, options =>
{
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
            {
                // Extract realm roles from Keycloak token
                var realmRoles = context.Principal.FindFirst("realm_access")?.Value;
                if (!string.IsNullOrEmpty(realmRoles))
                {
                    var parsed = JObject.Parse(realmRoles);
                    var roles = parsed["roles"]?.ToObject<List<string>>();
                    if (roles != null)
                    {
                        // Add each role as a ClaimTypes.Role claim
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
```

#### Authentication Endpoint

```csharp
[HttpPost("Keycloaktest")]
public async Task<IActionResult> KeycloakTest([FromBody] LoginRequest request)
{
    // Authenticate with Keycloak
    var tokenResponse = await keycloakService.AuthenticateAsync(request);
    return Ok(new { token = tokenResponse.AccessToken });
}
```

#### Token Validation Middleware

```csharp
app.UseAuthentication(); // Validates JWT tokens and processes events
app.UseAuthorization();  // Applies role-based policies
```

### 3. Role Management

#### Creating Roles

```csharp
[HttpPost("roles")]
public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
{
    // Create role in ASP.NET Identity
    var result = await roleManager.CreateAsync(new IdentityRole(request.RoleName));
    return Ok(result);
}
```

#### Assigning Roles

```csharp
[HttpPost("{userId}/identity-roles")]
public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
{
    var user = await userManager.FindByIdAsync(userId);
    var result = await userManager.AddToRoleAsync(user, request.RoleName);
    return Ok(result);
}
```

---

## 🔄 Machine-to-Machine (M2M) Communication

### 1. API2 M2M Implementation

#### Service Registration

```csharp
// API2 Extensions/ServiceCollectionExtensions.cs
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddKeycloakWebApi(configuration);

// Register HttpClient with automatic token management
services.AddHttpClient("protection")
    .AddClientCredentialsTokenHandler(protectionClient);

services.AddClientCredentialsTokenManagement()
    .AddClient(protectionClient, client =>
    {
        var options = configuration.GetKeycloakOptions<KeycloakProtectionClientOptions>()!;
        client.ClientId = options.Resource;
        client.ClientSecret = options.Credentials.Secret;
        client.TokenEndpoint = options.KeycloakTokenEndpoint;
    });
```

#### M2M Service Implementation

```csharp
public class ExampleApiCallService
{
    private readonly HttpClient _httpClient;

    public ExampleApiCallService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("protection");
    }

    public async Task<string> CallApiAsync()
    {
        // Token automatically retrieved and added to Authorization header
        var response = await _httpClient.GetAsync("http://localhost:5007/Secured");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        return $"Error: {response.StatusCode}";
    }
}
```

#### M2M Controller Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("example-api-call")]
    public async Task<IActionResult> ExampleApiCall()
    {
        var result = await _exampleApiCallService.CallApiAsync();
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("example-token-retrieval")]
    public async Task<IActionResult> ExampleTokenRetrieval()
    {
        var token = await _tokenService.GetAccessTokenAsync("protection");
        return Ok(new { Token = token });
    }
}
```

### 2. Token Management

#### Automatic Token Handling

```csharp
// Automatic token retrieval and refresh
var token = await _tokenService.GetAccessTokenAsync("protection");

// Direct HTTP call with token
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token.AccessToken);
var result = await httpClient.GetAsync("http://localhost:5007/Secured");
```

---

## 🏢 Header-Based Multi-Tenancy

### 1. Organization Header Implementation

#### Request Header Strategy

```http
GET /api/users HTTP/1.1
Host: localhost:5007
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
OrganizationId: org-123
Content-Type: application/json
```

### 2. Controller Implementation

#### Extracting Organization from Headers

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        // Extract organization ID from request headers
        var organizationId = Request.Headers["OrganizationId"].FirstOrDefault();

        if (string.IsNullOrEmpty(organizationId))
        {
            return BadRequest("OrganizationId header is required");
        }

        // Use organization ID for data filtering
        var users = await _userService.GetUsersByOrganizationAsync(organizationId);
        return Ok(users);
    }
}
```

#### Base Controller for Multi-Tenancy

```csharp
[ApiController]
public abstract class TenantAwareController : ControllerBase
{
    protected string GetOrganizationId()
    {
        return Request.Headers["OrganizationId"].FirstOrDefault()
               ?? throw new ArgumentException("OrganizationId header is required");
    }

    protected async Task<IActionResult> ExecuteWithTenant<T>(
        Func<string, Task<T>> operation)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var result = await operation(organizationId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

### 3. Service Layer Implementation

#### Tenant-Aware Service

```csharp
public class AppUserService : IAppUserService
{
    private readonly AuthDbContext _context;

    public AppUserService(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppUser>> GetUsersByOrganizationAsync(string organizationId)
    {
        return await _context.Users
            .Where(u => u.OrganizationId == organizationId)
            .ToListAsync();
    }

    public async Task<AppUser> CreateUserAsync(AppUser user, string organizationId)
    {
        user.OrganizationId = organizationId;
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }
}
```

### 4. Data Model Updates

#### Updated AppUser Entity

```csharp
public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? KeycloakUserId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

#### ITenantEntity Interface

```csharp
public interface ITenantEntity
{
    string OrganizationId { get; set; }
}

// Apply to all entities that need tenant isolation
public class AppUser : IdentityUser, ITenantEntity
{
    // ... existing properties
    public string OrganizationId { get; set; } = string.Empty;
}
```

### 5. Middleware for Header Validation

#### Organization Header Middleware

```csharp
public class OrganizationHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OrganizationHeaderMiddleware> _logger;

    public OrganizationHeaderMiddleware(RequestDelegate next, ILogger<OrganizationHeaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for non-API endpoints
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Skip validation for authentication endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        var organizationId = context.Request.Headers["OrganizationId"].FirstOrDefault();

        if (string.IsNullOrEmpty(organizationId))
        {
            _logger.LogWarning("Request to {Path} missing OrganizationId header", context.Request.Path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("OrganizationId header is required");
            return;
        }

        // Add organization ID to context for easy access
        context.Items["OrganizationId"] = organizationId;

        await _next(context);
    }
}

// Register in Program.cs
app.UseMiddleware<OrganizationHeaderMiddleware>();
```

### 6. HTTP Client Configuration for M2M

#### M2M with Organization Headers

```csharp
public class ExampleApiCallService
{
    private readonly HttpClient _httpClient;

    public ExampleApiCallService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("protection");
    }

    public async Task<string> CallApiAsync(string organizationId)
    {
        // Add organization header to M2M requests
        _httpClient.DefaultRequestHeaders.Remove("OrganizationId");
        _httpClient.DefaultRequestHeaders.Add("OrganizationId", organizationId);

        var response = await _httpClient.GetAsync("http://localhost:5007/Secured");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        return $"Error: {response.StatusCode}";
    }
}
```

---

## 🗄️ Database Schema

### 1. Core Entities

#### AppUser (ASP.NET Identity)

```csharp
public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? KeycloakUserId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### 2. Database Context

```csharp
public class AuthDbContext : IdentityDbContext<AppUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Add indexes for organization-based queries
        builder.Entity<AppUser>()
            .HasIndex(u => u.OrganizationId)
            .HasDatabaseName("IX_Users_OrganizationId");
    }

    // Override SaveChanges to ensure organization isolation
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Additional validation can be added here
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 🔌 Integration Patterns for Other Services

### 1. Service Registration Template

```csharp
// Add to your service's Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakWebApi(builder.Configuration);

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireClaim("role", "Admin"));
});
```

### 2. Token Validation in Controllers

```csharp
[Authorize] // Requires valid JWT token
[ApiController]
public class SecureController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")] // Role-based authorization
    public IActionResult GetSecureData()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var organizationId = Request.Headers["OrganizationId"].FirstOrDefault();

        return Ok(new { userId, roles, organizationId });
    }
}
```

### 3. User Context Resolution

```csharp
public class UserContextService
{
    public UserContext GetCurrentUser(ClaimsPrincipal principal, HttpContext httpContext)
    {
        return new UserContext
        {
            UserId = principal.FindFirst("sub")?.Value,
            Email = principal.FindFirst("email")?.Value,
            Roles = principal.FindAll("role").Select(c => c.Value).ToList(),
            OrganizationId = httpContext.Request.Headers["OrganizationId"].FirstOrDefault()
        };
    }
}

public class UserContext
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? OrganizationId { get; set; }
}
```

---

## 🛠️ Development Guidelines

### 1. Adding New Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Always require authentication
public class NewController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "User,Admin")] // Role-based access
    public async Task<IActionResult> GetData()
    {
        var organizationId = Request.Headers["OrganizationId"].FirstOrDefault();
        if (string.IsNullOrEmpty(organizationId))
        {
            return BadRequest("OrganizationId header is required");
        }

        // Implementation with organization filtering
        return Ok();
    }
}
```

---

## 🔒 Security Considerations

### 1. JWT Token Security

- **Token Expiration**: Configure appropriate expiration times
- **Token Validation**: Always validate issuer, audience, and signature
- **Claim Verification**: Verify required claims are present
- **HTTPS Only**: Never transmit tokens over HTTP
- **Organization Isolation**: Always validate OrganizationId header

### 2. Multi-Tenant Security

- **Header Validation**: Always validate OrganizationId header presence
- **Data Isolation**: Ensure all queries include organization filtering
- **Access Control**: Verify user has access to the specified organization
- **Audit Logging**: Log all cross-organization access attempts

### 2. Role-Based Access Control

```csharp
// Granular permission checks
[Authorize(Policy = "CanManageUsers")]
public async Task<IActionResult> DeleteUser(string userId)
{
    // Additional authorization checks
    if (!await authService.CanUserManageAsync(User, userId))
    {
        return Forbid();
    }

    // Implementation
}
```

---

## 🚀 Next Steps for Other Services

### 1. Service Onboarding Checklist

- [ ] Register service in Keycloak
- [ ] Configure JWT authentication
- [ ] Implement role-based authorization
- [ ] Add health checks
- [ ] Configure logging
- [ ] Set up monitoring
- [ ] Write integration tests

### 2. Common Integration Points

1. **Authentication**: Copy JWT configuration
2. **Authorization**: Implement role checks
3. **User Context**: Extract user info from claims
4. **Error Handling**: Use standard response format
5. **Logging**: Follow structured logging patterns

### 3. Support & Resources

- **Documentation**: This implementation guide
- **Code Examples**: Reference API endpoints
- **Support Channel**: Development team Slack
- **Architecture Review**: Required before production

---

_This implementation serves as the foundation for all identity integration across the platform. Follow these patterns to ensure consistency and security across all services._
