namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Entity for storing refresh tokens
/// Used for long-lived authentication sessions with token rotation
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID this token belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The actual refresh token value (hashed)
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// JWT ID (jti) of the access token this refresh token was issued with
    /// </summary>
    public string JwtId { get; set; }

    /// <summary>
    /// Whether this token has been used (for token rotation)
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Whether this token has been revoked
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// When this token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// IP address from which the token was created
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// User agent from which the token was created
    /// </summary>
    public string UserAgent { get; set; }

    /// <summary>
    /// When this token was used (for rotation tracking)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// When this token was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revocation (if applicable)
    /// </summary>
    public string RevokedReason { get; set; }

    /// <summary>
    /// Token that replaced this one (for rotation chain tracking)
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    // Navigation properties
    public User User { get; set; }
    public RefreshToken ReplacedByToken { get; set; }

    /// <summary>
    /// Check if the token is currently valid
    /// </summary>
    public bool IsValid()
    {
        return !IsUsed && !IsRevoked && ExpiresAt > DateTime.UtcNow;
    }
}
