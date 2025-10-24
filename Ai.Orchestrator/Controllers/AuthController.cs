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
    private readonly IAuditService _auditService;

    public AuthController(IUserService userService, IJwtService jwtService, IAuditService auditService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
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

            // Log successful registration
            await _auditService.LogSecurityEventAsync(
                "UserRegistration",
                user.Id,
                user.Username,
                "Success",
                $"User '{user.Username}' registered with email '{user.Email}'");

            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            // Log failed registration
            await _auditService.LogSecurityEventAsync(
                "UserRegistration",
                null,
                request.Username,
                "Failure",
                $"Registration failed: {ex.Message}");

            // Duplicate username or email
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Log validation failure
            await _auditService.LogSecurityEventAsync(
                "UserRegistration",
                null,
                request.Username,
                "Failure",
                $"Validation failed: {ex.Message}");

            // Validation errors (invalid email, password too short, etc.)
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log unexpected error
            await _auditService.LogSecurityEventAsync(
                "UserRegistration",
                null,
                request.Username,
                "Failure",
                $"Unexpected error: {ex.Message}");

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
                // Log failed login
                await _auditService.LogSecurityEventAsync(
                    "Login",
                    null,
                    request.Username,
                    "Failure",
                    "Invalid credentials");

                return Unauthorized(new { message = "Invalid credentials" });
            }

            var token = _jwtService.GenerateToken(user.Id, user.Username, user.Roles);
            var expiresAt = _jwtService.GetTokenExpiration(token);
            var jwtId = _jwtService.GetJwtId(token);

            // Generate and store refresh token
            var refreshTokenString = _jwtService.GenerateRefreshToken();
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            await _jwtService.StoreRefreshTokenAsync(
                user.Id,
                jwtId,
                refreshTokenString,
                ipAddress,
                userAgent);

            // Log successful login
            await _auditService.LogSecurityEventAsync(
                "Login",
                user.Id,
                user.Username,
                "Success",
                $"User '{user.Username}' logged in successfully");

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshTokenString,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Log unexpected error
            await _auditService.LogSecurityEventAsync(
                "Login",
                null,
                request.Username,
                "Failure",
                $"Unexpected error: {ex.Message}");

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
            // Get admin user info from claims
            var adminUsername = User.Identity?.Name ?? "Unknown";
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var adminId = adminIdClaim != null ? Guid.Parse(adminIdClaim) : (Guid?)null;

            await _userService.ResetPasswordAsync(request);

            // Log successful password reset
            await _auditService.LogSecurityEventAsync(
                "PasswordReset",
                request.UserId,
                adminUsername,
                "Success",
                $"Admin '{adminUsername}' reset password for user ID '{request.UserId}'");

            return Ok(new { message = "Password reset successfully" });
        }
        catch (InvalidOperationException ex)
        {
            // Log not found error
            await _auditService.LogSecurityEventAsync(
                "PasswordReset",
                request.UserId,
                User.Identity?.Name ?? "Unknown",
                "Failure",
                $"User not found: {ex.Message}");

            // User not found
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Log validation error
            await _auditService.LogSecurityEventAsync(
                "PasswordReset",
                request.UserId,
                User.Identity?.Name ?? "Unknown",
                "Failure",
                $"Validation failed: {ex.Message}");

            // Password validation errors
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log unexpected error
            await _auditService.LogSecurityEventAsync(
                "PasswordReset",
                request.UserId,
                User.Identity?.Name ?? "Unknown",
                "Failure",
                $"Unexpected error: {ex.Message}");

            // Unexpected errors
            return StatusCode(500, new { message = "An error occurred while resetting the password", error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New access token and refresh token</returns>
    /// <response code="200">Tokens refreshed successfully</response>
    /// <response code="401">Invalid or expired refresh token</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/auth/refresh
    ///     {
    ///        "RefreshToken": "a1b2c3d4e5f6..."
    ///     }
    ///
    /// Token rotation:
    /// - Old refresh token is marked as used
    /// - New access token and refresh token are generated
    /// - Old token cannot be reused (prevents replay attacks)
    /// </remarks>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Validate refresh token
            var refreshToken = await _jwtService.ValidateRefreshTokenAsync(request.RefreshToken);

            if (refreshToken == null)
            {
                // Log failed refresh
                await _auditService.LogSecurityEventAsync(
                    "TokenRefresh",
                    null,
                    "Unknown",
                    "Failure",
                    "Invalid or expired refresh token");

                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            // Get user from refresh token
            var user = await _userService.GetUserByIdAsync(refreshToken.UserId);

            if (user == null || !user.IsActive)
            {
                // Log failed refresh (inactive user)
                await _auditService.LogSecurityEventAsync(
                    "TokenRefresh",
                    refreshToken.UserId,
                    "Unknown",
                    "Failure",
                    "User not found or inactive");

                return Unauthorized(new { message = "User not found or inactive" });
            }

            // Generate new access token
            var newToken = _jwtService.GenerateToken(user.Id, user.Username, user.Roles);
            var expiresAt = _jwtService.GetTokenExpiration(newToken);
            var newJwtId = _jwtService.GetJwtId(newToken);

            // Rotate refresh token
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();
            var newRefreshToken = await _jwtService.RotateRefreshTokenAsync(
                refreshToken,
                newJwtId,
                ipAddress,
                userAgent);

            // Log successful refresh
            await _auditService.LogSecurityEventAsync(
                "TokenRefresh",
                user.Id,
                user.Username,
                "Success",
                $"User '{user.Username}' refreshed access token");

            var response = new LoginResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken.Token,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Log unexpected error
            await _auditService.LogSecurityEventAsync(
                "TokenRefresh",
                null,
                "Unknown",
                "Failure",
                $"Unexpected error: {ex.Message}");

            return StatusCode(500, new { message = "An error occurred while refreshing the token", error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <returns>Success message</returns>
    /// <response code="200">Token revoked successfully</response>
    /// <response code="401">Unauthorized (not authenticated)</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// **Requires authentication**
    ///
    /// Sample request:
    ///
    ///     POST /api/auth/revoke
    ///     Authorization: Bearer {token}
    ///     {
    ///        "RefreshToken": "a1b2c3d4e5f6..."
    ///     }
    ///
    /// Use cases:
    /// - User logs out from a device
    /// - User wants to invalidate a specific session
    /// - Security measure after password change
    /// </remarks>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        try
        {
            var username = User.Identity?.Name ?? "Unknown";
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userId = userIdClaim != null ? Guid.Parse(userIdClaim) : (Guid?)null;

            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken, "Revoked by user");

            // Log successful revocation
            await _auditService.LogSecurityEventAsync(
                "TokenRevocation",
                userId,
                username,
                "Success",
                $"User '{username}' revoked a refresh token");

            return Ok(new { message = "Token revoked successfully" });
        }
        catch (Exception ex)
        {
            var username = User.Identity?.Name ?? "Unknown";
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userId = userIdClaim != null ? Guid.Parse(userIdClaim) : (Guid?)null;

            // Log unexpected error
            await _auditService.LogSecurityEventAsync(
                "TokenRevocation",
                userId,
                username,
                "Failure",
                $"Unexpected error: {ex.Message}");

            return StatusCode(500, new { message = "An error occurred while revoking the token", error = ex.Message });
        }
    }

    /// <summary>
    /// Get client IP address from HTTP context
    /// </summary>
    private string GetClientIpAddress()
    {
        // Check for X-Forwarded-For header (load balancers, proxies)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        // Check for X-Real-IP header
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fall back to RemoteIpAddress
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
