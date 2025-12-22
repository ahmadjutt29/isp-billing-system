using IspBackend.Data;
using IspBackend.DTOs;
using IspBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace IspBackend.Services;

/// <summary>
/// Service for managing user operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets all users without password information.
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Gets a user by ID without password information.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(int id);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    Task<User?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Creates a new user with hashed password.
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    Task<bool> DeleteUserAsync(int id);

    /// <summary>
    /// Validates user credentials.
    /// </summary>
    Task<User?> ValidateCredentialsAsync(string username, string password);

    /// <summary>
    /// Checks if an admin user exists, and seeds one if not.
    /// </summary>
    Task<bool> EnsureAdminExistsAsync();

    /// <summary>
    /// Checks if a username already exists.
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);

    /// <summary>
    /// Checks if an email already exists.
    /// </summary>
    Task<bool> EmailExistsAsync(string email);
}

/// <summary>
/// Implementation of IUserService for managing user operations.
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        return await _context.Users
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user != null ? MapToDto(user) : null;
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        var user = new User
        {
            Username = createUserDto.Username,
            PasswordHash = HashPassword(createUserDto.Password),
            Email = createUserDto.Email,
            Role = string.IsNullOrEmpty(createUserDto.Role) ? "Client" : createUserDto.Role,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PhoneNumber = createUserDto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    /// <inheritdoc />
    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(updateUserDto.Email))
            user.Email = updateUserDto.Email;
        if (updateUserDto.FirstName != null)
            user.FirstName = updateUserDto.FirstName;
        if (updateUserDto.LastName != null)
            user.LastName = updateUserDto.LastName;
        if (updateUserDto.PhoneNumber != null)
            user.PhoneNumber = updateUserDto.PhoneNumber;
        if (!string.IsNullOrEmpty(updateUserDto.Role))
            user.Role = updateUserDto.Role;
        if (updateUserDto.IsActive.HasValue)
            user.IsActive = updateUserDto.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
        {
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user;
    }

    /// <inheritdoc />
    public async Task<bool> EnsureAdminExistsAsync()
    {
        var adminExists = await _context.Users.AnyAsync(u => u.Role == "Admin");

        if (adminExists)
        {
            return false; // Admin already exists, no seeding needed
        }

        // Seed default admin user
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = HashPassword("admin123"),
            Email = "admin@isp.local",
            Role = "Admin",
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        return true; // Admin was created
    }

    /// <inheritdoc />
    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    #region Private Helper Methods

    /// <summary>
    /// Hashes a password using BCrypt.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <returns>The hashed password.</returns>
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a password against a hash using BCrypt.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <param name="passwordHash">The stored password hash.</param>
    /// <returns>True if the password is valid.</returns>
    private static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            // If BCrypt verification fails (e.g., hash is not BCrypt format),
            // fall back to plain text comparison for legacy/demo data
            return password == passwordHash;
        }
    }

    /// <summary>
    /// Maps a User entity to UserDto.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <returns>The user DTO without sensitive data.</returns>
    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    #endregion
}
