using IspBackend.DTOs;
using IspBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspBackend.Controllers;

/// <summary>
/// Controller for managing users. Restricted to Admin role only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">The user service.</param>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Gets all users with basic info (no password hash).
    /// </summary>
    /// <returns>List of users.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Creates a new user. Role defaults to "Client" if not specified.
    /// </summary>
    /// <param name="createUserDto">The user creation data.</param>
    /// <returns>The created user.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if username already exists
        if (await _userService.UsernameExistsAsync(createUserDto.Username))
        {
            return Conflict(new { message = "Username already exists" });
        }

        // Check if email already exists
        if (await _userService.EmailExistsAsync(createUserDto.Email))
        {
            return Conflict(new { message = "Email already exists" });
        }

        var userDto = await _userService.CreateUserAsync(createUserDto);

        return CreatedAtAction(nameof(GetUser), new { id = userDto.Id }, userDto);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="updateUserDto">The update data.</param>
    /// <returns>The updated user.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        // Check if email is being changed and already exists
        if (!string.IsNullOrEmpty(updateUserDto.Email))
        {
            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser != null && 
                updateUserDto.Email != existingUser.Email &&
                await _userService.EmailExistsAsync(updateUserDto.Email))
            {
                return Conflict(new { message = "Email already exists" });
            }
        }

        var userDto = await _userService.UpdateUserAsync(id, updateUserDto);

        if (userDto == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(userDto);
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var deleted = await _userService.DeleteUserAsync(id);

        if (!deleted)
        {
            return NotFound(new { message = "User not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Seeds an initial Admin user if no Admin exists in the database.
    /// This endpoint is publicly accessible for initial setup only.
    /// </summary>
    /// <returns>The created admin user or message if already exists.</returns>
    [HttpPost("seed-admin")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedAdmin()
    {
        var wasCreated = await _userService.EnsureAdminExistsAsync();

        if (wasCreated)
        {
            return Created("", new { message = "Admin user created successfully", username = "admin" });
        }

        return Ok(new { message = "Admin user already exists" });
    }
}
