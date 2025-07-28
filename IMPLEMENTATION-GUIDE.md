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
- **Finbuckle.MultiTenant**: Multi-tenancy support (api3)
- **SQL Server**: User data and tenant persistence (api3)
- **JWT Bearer**: Token-based authentication (all APIs)
- **Client Credentials Flow**: Machine-to-machine authentication (api2)

### Service Architecture

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│    API2     │    │    API3     │    │  Keycloak   │
│ (M2M Focus) │◄──►│(User/Tenant)│◄──►│ (Identity)  │
│             │    │             │    │             │
└─────────────┘    └─────────────┘    └─────────────┘
      ▲                  ▲                    ▲
      │                  │                    │
      ▼                  ▼                    ▼
Client Credentials   ASP.NET Identity    JWT Tokens
  Flow (M2M)        + Multi-Tenant       + Roles
```

### Data Flow

```
1. User Authentication: Client → Keycloak → JWT Token → API3 → ASP.NET Identity
2. M2M Authentication: Service → Keycloak → JWT Token → API2 → Protected Resources
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
├── api3/                          # User & Tenant Management API
│   ├── Controllers/               # User/tenant endpoints
│   │   ├── SecuredController.cs   # Protected user endpoints
│   │   ├── UserController.cs      # User management
│   │   └── KeycloakOrganizationController.cs # Organization ops
│   ├── Data/                      # Database contexts
│   │   ├── AuthDbContext.cs       # Identity context
│   │   └── TenantContext.cs       # Multi-tenant context
│   ├── Models/                    # Entity models
│   │   ├── AppUser.cs             # User entity
│   │   └── UserDtos.cs            # Data transfer objects
│   ├── Services/                  # Business logic
│   │   ├── AppUserService.cs      # User operations
│   │   ├── KeycloakAdminService.cs # Keycloak integration
│   │   └── UserSyncService.cs     # User synchronization
│   ├── Extensions/                # Service configuration
│   │   ├── ServiceCollectionExtensions.cs # Auth + MultiTenant setup
│   │   └── SwaggerExtensions.cs   # API documentation
│   └── Program.cs                 # Application configuration
└── Purefire.sln                  # Solution file
```

### 2. Key Dependencies

```xml
<PackageReference Include="Finbuckle.MultiTenant" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
<PackageReference Include="Keycloak.AuthServices.Authentication" />
<PackageReference Include="Keycloak.AuthServices.Sdk" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
<PackageReference Include="Duende.AccessTokenManagement" />
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

// Configure multi-tenancy
services.AddMultiTenant<TenantInfo>()
    .WithEFCoreStore<TenantContext, TenantInfo>()
    .WithClaimStrategy("organization");

// Configure Keycloak + ASP.NET Identity
services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakWebApi(configuration);
```

### 2. JWT Token Flow

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
app.UseAuthentication(); // Validates JWT tokens
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

## 🏢 Multi-Tenancy Implementation

### 1. Tenant Context Strategy

```csharp
services.AddMultiTenant<TenantInfo>()
    .WithClaimStrategy("organization"); // Uses JWT claim for tenant resolution
```

### 2. Tenant Data Isolation

#### Tenant-Aware Controller

```csharp
[ApiController]
public class TenantController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTenantData()
    {
        var tenantId = HttpContext.GetMultiTenantContext()?.TenantInfo?.Id;
        var data = await dataService.GetTenantDataAsync(tenantId);
        return Ok(data);
    }
}
```

### 3. Organization Management

```csharp
[HttpPost]
public async Task<IActionResult> CreateOrganization([FromBody] CreateOrgRequest request)
{
    // Create tenant in local database
    var tenant = await tenantService.CreateAsync(request.Name);
    return Ok(tenant);
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
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

#### TenantInfo (Multi-tenancy)

```csharp
public class TenantInfo : ITenantInfo
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string ConnectionString { get; set; }
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
        // Custom configurations
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

        return Ok(new { userId, roles });
    }
}
```

### 3. User Context Resolution

```csharp
public class UserContextService
{
    public UserContext GetCurrentUser(ClaimsPrincipal principal)
    {
        return new UserContext
        {
            UserId = principal.FindFirst("sub")?.Value,
            Email = principal.FindFirst("email")?.Value,
            Roles = principal.FindAll("role").Select(c => c.Value).ToList(),
            Organization = principal.FindFirst("organization")?.Value
        };
    }
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
        // Implementation
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
