namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Request model for revoking a refresh token
/// </summary>
/// <example>
/// {
///   "RefreshToken": "a1b2c3d4e5f6..."
/// }
/// </example>
public class RevokeTokenRequest
{
    /// <summary>
    /// The refresh token to revoke
    /// </summary>
    public string RefreshToken { get; set; }
}
