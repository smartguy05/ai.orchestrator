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
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
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
    /// Login and receive JWT token
    /// </summary>
    [HttpPost("login")]
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
    /// Reset a user's password (admin only)
    /// </summary>
    [HttpPost("reset-password")]
    [Authorize(Policy = "Admin")]
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
