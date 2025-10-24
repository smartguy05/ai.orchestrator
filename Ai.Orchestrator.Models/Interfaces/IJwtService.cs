using Ai.Orchestrator.Models.Entities;
using System.Security.Claims;

namespace Ai.Orchestrator.Models.Interfaces;

/// <summary>
/// Service interface for JWT token management
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a JWT token for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="username">Username</param>
    /// <param name="roles">User roles</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(Guid userId, string username, List<string> roles);

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// Extracts claims from a JWT token
    /// </summary>
    /// <param name="token">Token to extract claims from</param>
    /// <returns>List of claims, or empty list if token is invalid</returns>
    List<Claim> GetClaims(string token);

    /// <summary>
    /// Extracts user ID from a JWT token
    /// </summary>
    /// <param name="token">Token to extract user ID from</param>
    /// <returns>User ID, or null if token is invalid</returns>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Extracts username from a JWT token
    /// </summary>
    /// <param name="token">Token to extract username from</param>
    /// <returns>Username, or null if token is invalid</returns>
    string GetUsernameFromToken(string token);

    /// <summary>
    /// Generates a cryptographically secure refresh token
    /// </summary>
    /// <returns>Base64-encoded refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Stores a refresh token in the database
    /// </summary>
    /// <param name="userId">User who owns the token</param>
    /// <param name="jwtId">JWT ID from the access token</param>
    /// <param name="token">The refresh token string</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>Created RefreshToken entity</returns>
    Task<RefreshToken> StoreRefreshTokenAsync(Guid userId, string jwtId, string token, string ipAddress, string userAgent);

    /// <summary>
    /// Validates a refresh token and returns it if valid
    /// </summary>
    /// <param name="token">The refresh token to validate</param>
    /// <returns>RefreshToken entity if valid, null otherwise</returns>
    Task<RefreshToken> ValidateRefreshTokenAsync(string token);

    /// <summary>
    /// Rotates a refresh token (marks old as used, creates new)
    /// </summary>
    /// <param name="oldToken">The token being replaced</param>
    /// <param name="newJwtId">JWT ID for the new access token</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>New RefreshToken entity</returns>
    Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, string newJwtId, string ipAddress, string userAgent);

    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    /// <param name="token">The token to revoke</param>
    /// <param name="reason">Reason for revocation</param>
    Task RevokeRefreshTokenAsync(string token, string reason);

    /// <summary>
    /// Extracts JWT ID from an access token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>JWT ID (jti claim)</returns>
    string GetJwtId(string token);

    /// <summary>
    /// Gets the expiration time from a JWT token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>Expiration timestamp</returns>
    DateTime GetTokenExpiration(string token);
}
