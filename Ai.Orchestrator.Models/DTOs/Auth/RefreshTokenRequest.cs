namespace Ai.Orchestrator.Models.DTOs.Auth;

/// <summary>
/// Request model for refreshing an access token
/// </summary>
/// <example>
/// {
///   "RefreshToken": "a1b2c3d4e5f6..."
/// }
/// </example>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token received during login
    /// </summary>
    public string RefreshToken { get; set; }
}
