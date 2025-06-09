# Purefire Authentication System

A secure authentication system built with ASP.NET Core, featuring JWT-based authentication for both user and machine-to-machine (M2M) scenarios. The system is designed as a microservices architecture with two APIs and a shared authentication library.

## Project Structure

```
Purefire/
├── Purefire.Auth/                 # Shared authentication library
│   ├── Data/                     # Database context and seeding
│   ├── Extensions/               # Service configuration extensions
│   ├── Models/                   # Domain models
│   └── Services/                 # Authentication services
├── api1/                         # Authentication API
│   └── Controllers/             # API endpoints
└── api2/                         # Protected API
    └── Controllers/             # Protected endpoints
```

## Features

- **User Authentication**

  - JWT-based authentication
  - Role-based authorization (Admin, User roles)
  - User registration and login
  - Password hashing using ASP.NET Identity

- **Machine-to-Machine (M2M) Authentication**

  - Client credentials flow
  - Scope-based access control
  - Client registration and management

- **Security Features**
  - JWT token validation
  - Role-based access control
  - Scope-based authorization
  - Secure password storage
  - In-memory database for development

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Postman or similar API testing tool

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/Purefire.git
   cd Purefire
   ```

2. Build the solution:

   ```bash
   dotnet build
   ```

3. Run the APIs:

   ```bash
   # Terminal 1 - API 1 (Authentication)
   cd api1
   dotnet run

   # Terminal 2 - API 2 (Protected API)
   cd api2
   dotnet run
   ```

## API Endpoints

### Authentication API (api1)

#### User Authentication

- `POST /api/auth/register`

  - Register a new user
  - Body: `{ "email": "string", "password": "string", "firstName": "string", "lastName": "string" }`

- `POST /api/auth/login`
  - Login with user credentials
  - Body: `{ "email": "string", "password": "string" }`
  - Returns: JWT token

#### Client Authentication

- `POST /api/auth/login/client`
  - Login with client credentials
  - Body: `{ "clientId": "string", "clientSecret": "string" }`
  - Returns: JWT token

### Protected API (api2)

All endpoints require a valid JWT token in the Authorization header:

```
Authorization: Bearer <token>
```

## Default Users

The system comes with pre-seeded users:

1. Admin User:

   - Email: admin@example.com
   - Password: Admin123!
   - Role: Admin

2. Regular User:
   - Email: user@example.com
   - Password: User123!
   - Role: User

## Default Service Clients

1. Service 1:

   - Client ID: service1
   - Client Secret: service1-secret
   - Scopes: api1, api2

2. Service 2:
   - Client ID: service2
   - Client Secret: service2-secret
   - Scopes: api2

## Authorization Policies

The system implements several authorization policies:

1. **ServiceClient**

   - Requires a valid client_id claim
   - Used for M2M authentication

2. **AdminOrService**

   - Allows either Admin role or client_id claim
   - Used for endpoints accessible by both admins and services

3. **RequireApi2Scope**
   - Requires the api2 scope in the token
   - Used for API 2 specific endpoints

## Development

### Adding New Endpoints

1. Create a new controller in the appropriate API project
2. Add the `[Authorize]` attribute to require authentication
3. Use policy attributes for specific authorization requirements:
   ```csharp
   [Authorize(Policy = "AdminOrService")]
   [Authorize(Policy = "RequireApi2Scope")]
   ```

### Adding New Roles

1. Update the `DbSeeder.cs` to include the new role
2. Add the role to the `IdentityRole` seeding
3. Assign the role to users as needed

## Security Considerations

- The project uses an in-memory database for development
- For production:
  - Replace the in-memory database with a persistent database
  - Use secure configuration management
  - Implement proper logging and monitoring
  - Use HTTPS in production
  - Consider implementing rate limiting
  - Add additional security headers

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
