using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Purefire.Auth.Data;
using Purefire.Auth.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Purefire.Auth.Services
{
    public interface IAuthService
    {
        Task<(bool success, string token)> LoginAsync(string email, string password);
        Task<(bool success, string token)> LoginServiceClientAsync(string clientId, string clientSecret);
        Task<(bool success, string message)> RegisterAsync(string email, string password, string firstName, string lastName);
        Task<(bool success, string message)> CreateClientAsync(string clientId, string clientSecret, string name, string[] allowedScopes);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AuthDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool success, string token)> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return (false, "Invalid email or password");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!result.Succeeded)
            {
                return (false, "Invalid email or password");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var token = GenerateJwtToken(user);
            return (true, token);
        }

        public async Task<(bool success, string token)> LoginServiceClientAsync(string clientId, string clientSecret)
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            if (client == null || !VerifyClienSecret(clientSecret, client.ClientSecret))
            {
                return (false, "Invalid client credentials");
            }

            var claims = new List<Claim>
            {
                new Claim("client_id", clientId),
                new Claim("scope", string.Join(" ", client.AllowedScopes))
            };

            var token = GenerateJwtToken(claims);
            return (true, token);
        }

        public async Task<(bool success, string message)> RegisterAsync(string email, string password, string firstName, string lastName)
        {
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Assign User role
            await _userManager.AddToRoleAsync(user, "User");

            return (true, "User registered successfully");
        }

        // TODO: Add a method to create a client
        public async Task<(bool success, string message)> CreateClientAsync(string clientId, string clientSecret, string name, string[] allowedScopes)
        {
            var client = new Client
            {
                ClientId = clientId,
                // Hash the client secret
                ClientSecret = BCrypt.Net.BCrypt.HashPassword(clientSecret),
                AllowedScopes = allowedScopes,
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Clients.AddAsync(client);
            await _context.SaveChangesAsync();

            return (true, "Client created successfully");
        }

        private string GenerateJwtToken(AppUser user)
        {
            var roles = _userManager.GetRolesAsync(user).Result;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim("scope", "api1 api2")
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return GenerateJwtToken(claims);
        }

        private string GenerateJwtToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerifyClienSecret(string clientSecret, string clientSecretHash)
        {
            // In a real application, you would use a proper password hashing library
            // This is just a simple example

            return BCrypt.Net.BCrypt.Verify(clientSecret, clientSecretHash);
        }
    }
}