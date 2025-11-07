using AiMiddleTier.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AiMiddleTier.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static List<User> users = new();
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        // 🔹 Register endpoint
        [HttpPost("register")]
        public IActionResult Register([FromBody] User newUser)
        {
            if (users.Any(u => u.Email == newUser.Email))
                return BadRequest("Email already registered.");

            // hash the password
            newUser.PasswordHash = HashPassword(newUser.PasswordHash);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.LastLogin = DateTime.UtcNow;

            users.Add(newUser);
            return Ok("User registered successfully.");
        }

        // 🔹 Login endpoint
        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginUser)
        {
            var user = users.FirstOrDefault(u => u.Email == loginUser.Email);
            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (!VerifyPassword(loginUser.PasswordHash, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            user.LastLogin = DateTime.UtcNow;

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        // ✅ helper: password hashing
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

        // ✅ helper: JWT token generation
        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
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
