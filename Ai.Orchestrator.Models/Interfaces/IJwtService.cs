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
}
