using AiMiddleTier.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AiMiddleTier.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static List<User> users = new();
        private readonly IConfiguration _config;
        private static bool _seedDataAdded = false;

        public AuthController(IConfiguration config)
        {
            _config = config;

            // Add seeded test user (only once)
            if (!_seedDataAdded)
            {
                var seedUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "Test",
                    LastName = "User",
                    Email = "a@a.a",
                    PasswordHash = HashPassword("P@$$w0rd"),
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow
                };
                users.Add(seedUser);
                _seedDataAdded = true;
            }
        }

        // Register endpoint
        [HttpPost("register")]
        public IActionResult Register([FromBody] User newUser)
        {
            // Check model validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();
                return BadRequest(new { Message = "Validation failed", Errors = errors });
            }

            // Check if email already exists
            if (users.Any(u => u.Email == newUser.Email))
                return BadRequest(new { Message = "Email already registered." });

            // Hash the password
            newUser.PasswordHash = HashPassword(newUser.PasswordHash);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.LastLogin = DateTime.UtcNow;

            users.Add(newUser);
            return Ok(new { Message = "User registered successfully." });
        }

        // Login endpoint
        [HttpPost("login")]
        public IActionResult Login([FromBody] JsonElement loginData)
        {
            // Extract email and password from JSON
            if (!loginData.TryGetProperty("email", out var emailElement) ||
                !loginData.TryGetProperty("passwordHash", out var passwordElement))
            {
                return BadRequest(new { Message = "Email and password are required" });
            }

            var email = emailElement.GetString();
            var password = passwordElement.GetString();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new { Message = "Email and password cannot be empty" });
            }

            var user = users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return Unauthorized(new { Message = "Invalid credentials." });

            if (!VerifyPassword(password, user.PasswordHash))
                return Unauthorized(new { Message = "Invalid credentials." });

            user.LastLogin = DateTime.UtcNow;

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.CreatedAt,
                    user.LastLogin
                }
            });
        }

        // helper: password hashing
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string entered, string storedHash)
        {
            return HashPassword(entered) == storedHash;
        }

        // helper: JWT token generation
        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", $"{user.FirstName} {user.LastName}")
            };

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(2),
                claims: claims,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
