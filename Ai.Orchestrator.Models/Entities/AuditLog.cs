namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Entity for storing audit logs of security events and user activities
/// Used for compliance, security monitoring, and forensic analysis
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID who performed the action (null for system actions or anonymous)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Username who performed the action (denormalized for faster queries)
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Type of action performed (e.g., "Login", "CreateAgent", "UpdateConfig", "FailedLogin")
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// Entity type affected (e.g., "User", "Agent", "PluginConfig")
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// ID of the entity affected (if applicable)
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// IP address of the client making the request
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    /// User agent string from the request
    /// </summary>
    public string UserAgent { get; set; }

    /// <summary>
    /// Additional details about the action (JSON format)
    /// </summary>
    public string Details { get; set; }

    /// <summary>
    /// Outcome of the action ("Success", "Failure", "Unauthorized")
    /// </summary>
    public string Outcome { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// HTTP method used (GET, POST, PUT, DELETE)
    /// </summary>
    public string HttpMethod { get; set; }

    /// <summary>
    /// Request path
    /// </summary>
    public string RequestPath { get; set; }

    /// <summary>
    /// HTTP status code of the response
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Request duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    // Navigation properties
    public User User { get; set; }
}
