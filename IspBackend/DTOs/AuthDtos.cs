using System.ComponentModel.DataAnnotations;

namespace IspBackend.DTOs;

/// <summary>
/// Data transfer object for user login requests.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// The username for authentication.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password for authentication.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response object containing the JWT token after successful authentication.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration date and time.
    /// </summary>
    public DateTime Expiration { get; set; }

    /// <summary>
    /// The authenticated user's username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The authenticated user's role.
    /// </summary>
    public string Role { get; set; } = string.Empty;
}
