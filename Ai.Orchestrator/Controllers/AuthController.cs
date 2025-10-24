using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ai.Orchestrator.Controllers;

/// <summary>
/// Controller for authentication operations
/// Handles user registration, login, and password reset
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;

    public AuthController(IUserService userService, IJwtService jwtService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">User registration information (username, email, password)</param>
    /// <returns>The newly created user information (without password)</returns>
    /// <response code="200">User registered successfully</response>
    /// <response code="400">Validation error (invalid email, weak password, duplicate username/email)</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/auth/register
    ///     {
    ///        "Username": "john.doe",
    ///        "Email": "john.doe@example.com",
    ///        "Password": "SecurePassword123!"
    ///     }
    ///
    /// Password requirements:
    /// - Minimum 8 characters
    /// - Will be hashed with BCrypt (work factor 12)
    ///
    /// Default role: User
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(Models.DTOs.Users.UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _userService.RegisterUserAsync(request);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            // Duplicate username or email
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Validation errors (invalid email, password too short, etc.)
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Unexpected errors
            return StatusCode(500, new { message = "An error occurred while registering the user", error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticate user and receive JWT token
    /// </summary>
    /// <param name="request">Login credentials (username and password)</param>
    /// <returns>JWT token with user information and roles</returns>
    /// <response code="200">Login successful, returns JWT token</response>
    /// <response code="401">Invalid credentials or inactive user</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/auth/login
    ///     {
    ///        "Username": "john.doe",
    ///        "Password": "SecurePassword123!"
    ///     }
    ///
    /// The returned JWT token should be included in the Authorization header for all protected endpoints:
    ///
    ///     Authorization: Bearer {token}
    ///
    /// Token includes:
    /// - User ID (NameIdentifier claim)
    /// - Username (Name claim)
    /// - Roles (Role claims)
    /// - Expiration timestamp
    ///
    /// Token characteristics:
    /// - Signed with HMAC-SHA256
    /// - Configurable expiration (default: 24 hours)
    /// - Zero clock skew tolerance
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userService.AuthenticateAsync(request);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var token = _jwtService.GenerateToken(user.Id, user.Username, user.Roles);
            var expiresAt = _jwtService.GetTokenExpiration(token);

            var response = new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Unexpected errors (JWT generation failure, etc.)
            return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
        }
    }

    /// <summary>
    /// Reset a user's password (Admin only)
    /// </summary>
    /// <param name="request">User ID and new password</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Validation error (weak password)</response>
    /// <response code="401">Unauthorized (not an admin)</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// **Requires Admin role**
    ///
    /// Sample request:
    ///
    ///     POST /api/auth/reset-password
    ///     Authorization: Bearer {admin-token}
    ///     {
    ///        "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "NewPassword": "NewSecurePassword123!"
    ///     }
    ///
    /// Password requirements:
    /// - Minimum 8 characters
    /// - Will be hashed with BCrypt (work factor 12)
    ///
    /// Use cases:
    /// - User forgot password and requested admin assistance
    /// - Admin-initiated password reset for security reasons
    /// </remarks>
    [HttpPost("reset-password")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _userService.ResetPasswordAsync(request);
            return Ok(new { message = "Password reset successfully" });
        }
        catch (InvalidOperationException ex)
        {
            // User not found
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Password validation errors
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Unexpected errors
            return StatusCode(500, new { message = "An error occurred while resetting the password", error = ex.Message });
        }
    }
}
