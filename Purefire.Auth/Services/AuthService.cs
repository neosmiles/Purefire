using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Purefire.Auth.Data;
using Purefire.Auth.Models;

namespace Purefire.Auth.Services
{
    public interface IAuthService
    {
        Task<(bool success, string token)> AuthenticateUserAsync(string username, string password);
        Task<(bool success, string token)> AuthenticateClientAsync(string clientId, string clientSecret);
    }

    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AuthDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool success, string token)> AuthenticateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return (false, null);

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, GenerateJwtToken(user));
        }

        public async Task<(bool success, string token)> AuthenticateClientAsync(string clientId, string clientSecret)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (client == null || client.ClientSecret != clientSecret)
                return (false, null);

            client.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, GenerateJwtToken(client));
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateJwtToken(Client client)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("client_id", client.ClientId),
                    new Claim("client_name", client.Name),
                    new Claim("scope", string.Join(" ", client.AllowedScopes))
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            // In a real application, you would use a proper password hashing library
            // This is just a simple example
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}