using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AmazonScraper.Api.Models;
using AmazonScraper.Api.Services;

namespace AmazonScraper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UsersRepository _users;
    private readonly UserSettingsRepository _userSettings;
    private readonly IConfiguration _config;

    public AuthController(UsersRepository users, UserSettingsRepository userSettings, IConfiguration config)
    {
        _users = users;
        _userSettings = userSettings;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and password are required." });

        var user = await _users.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { error = "Invalid email or password." });

        // Normalise $2y$ (PHP) → $2b$ and trim any whitespace from copy-paste
        var hash = user.PasswordHash.Trim();
        if (hash.StartsWith("$2y$"))
            hash = "$2b$" + hash[4..];

        bool passwordValid;
        try { passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, hash); }
        catch { passwordValid = false; }

        if (!passwordValid)
            return Unauthorized(new { error = "Invalid email or password." });

        var token = GenerateJwt(user);

        return Ok(new
        {
            token,
            user = new { user.Id, user.Name, user.Email }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Name, email and password are required." });

        if (request.Password.Length < 8)
            return BadRequest(new { error = "Password must be at least 8 characters." });

        var existing = await _users.FindByEmailAsync(request.Email);
        if (existing != null)
            return Conflict(new { error = "An account with that email already exists." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, 10);
        var user = await _users.CreateAsync(request.Name, request.Email, hash);

        if (user == null)
            return StatusCode(500, new { error = "Failed to create account." });

        // Create default user settings
        try
        {
            await _userSettings.CreateAsync(user.Id);
        }
        catch (Exception ex)
        {
            // Log but don't fail - user was created successfully
            Console.WriteLine($"Failed to create default settings for user {user.Id}: {ex.Message}");
        }

        var token = GenerateJwt(user);
        return Ok(new
        {
            token,
            user = new { user.Id, user.Name, user.Email }
        });
    }

    private string GenerateJwt(User user)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? throw new InvalidOperationException("Missing env var: JWT_SECRET");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
