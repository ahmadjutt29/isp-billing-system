using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IspBackend.Models;

/// <summary>
/// Represents a user in the system.
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unique username for authentication.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password for secure authentication.
    /// Never store plain text passwords.
    /// </summary>
    [Required(ErrorMessage = "Password hash is required")]
    [StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User role for authorization (e.g., 'Admin', 'Client').
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    [StringLength(20)]
    public string Role { get; set; } = "Client";

    /// <summary>
    /// User's email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    [StringLength(50)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    [StringLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// User's phone number.
    /// </summary>
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Indicates whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date and time when the user was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the user was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Date and time of the user's last login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Refresh token for JWT authentication.
    /// </summary>
    [StringLength(256)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Expiry date for the refresh token.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
