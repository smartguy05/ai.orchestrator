namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Response model for successful login
/// </summary>
/// <example>
/// {
///   "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
///   "RefreshToken": "a1b2c3d4e5f6...",
///   "Username": "john.doe",
///   "Email": "john.doe@example.com",
///   "Roles": ["User"],
///   "ExpiresAt": "2024-01-15T12:00:00Z"
/// }
/// </example>
public class LoginResponse
{
    /// <summary>
    /// JWT token for authentication (use in Authorization header as "Bearer {token}")
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens without re-authenticating
    /// </summary>
    public string RefreshToken { get; set; }

    /// <summary>
    /// Username of the authenticated user
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Email address of the authenticated user
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// List of roles assigned to the user (Admin, AgentManager, User, ReadOnly)
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// UTC timestamp when the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
