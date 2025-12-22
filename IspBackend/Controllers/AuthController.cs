using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IspBackend.DTOs;
using IspBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace IspBackend.Controllers;

/// <summary>
/// Controller for handling authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="userService">The user service.</param>
    /// <param name="configuration">The application configuration.</param>
    public AuthController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="loginDto">The login credentials.</param>
    /// <returns>A JWT token if authentication is successful.</returns>
    /// <response code="200">Returns the JWT token.</response>
    /// <response code="401">If the credentials are invalid.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate credentials using UserService (handles BCrypt verification)
        var user = await _userService.ValidateCredentialsAsync(loginDto.Username, loginDto.Password);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Generate JWT token
        var token = GenerateJwtToken(user.Username, user.Role, user.Id);

        var response = new LoginResponseDto
        {
            Token = token.Token,
            Expiration = token.Expiration,
            Username = user.Username,
            Role = user.Role
        };

        return Ok(response);
    }

    /// <summary>
    /// Ensures an admin user exists in the system. Creates one if not.
    /// </summary>
    /// <returns>Status of admin seeding.</returns>
    [HttpPost("seed-admin")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SeedAdmin()
    {
        var wasCreated = await _userService.EnsureAdminExistsAsync();

        if (wasCreated)
        {
            return Created("", new { message = "Admin user created successfully", username = "admin" });
        }

        return Ok(new { message = "Admin user already exists" });
    }

    /// <summary>
    /// Generates a JWT token for the authenticated user.
    /// </summary>
    /// <param name="username">The user's username.</param>
    /// <param name="role">The user's role.</param>
    /// <param name="userId">The user's ID.</param>
    /// <returns>The generated token and expiration.</returns>
    private (string Token, DateTime Expiration) GenerateJwtToken(string username, string role, int userId)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "SuperSecretKey123456789012345678901234567890";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var expiration = DateTime.UtcNow.AddDays(1);

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, expiration);
    }
}
