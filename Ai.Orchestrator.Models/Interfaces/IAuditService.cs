namespace Ai.Orchestrator.Models.Interfaces;

/// <summary>
/// Service for logging audit events and security activities
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log a successful action
    /// </summary>
    Task LogSuccessAsync(string action, string entityType, Guid? entityId, Guid? userId, string username, string details = null);

    /// <summary>
    /// Log a failed action
    /// </summary>
    Task LogFailureAsync(string action, string entityType, Guid? entityId, Guid? userId, string username, string details = null);

    /// <summary>
    /// Log an unauthorized access attempt
    /// </summary>
    Task LogUnauthorizedAsync(string action, string entityType, Guid? entityId, Guid? userId, string username, string details = null);

    /// <summary>
    /// Log a security event (login, password reset, etc.)
    /// </summary>
    Task LogSecurityEventAsync(string action, Guid? userId, string username, string outcome, string details = null);

    /// <summary>
    /// Log an HTTP request
    /// </summary>
    Task LogHttpRequestAsync(
        string httpMethod,
        string requestPath,
        int statusCode,
        long durationMs,
        Guid? userId,
        string username,
        string ipAddress,
        string userAgent);

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    Task<List<Entities.AuditLog>> GetUserAuditLogsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<List<Entities.AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId, int limit = 100);

    /// <summary>
    /// Get failed security events (for monitoring)
    /// </summary>
    Task<List<Entities.AuditLog>> GetFailedSecurityEventsAsync(DateTime? startDate = null, int limit = 100);

    /// <summary>
    /// Search audit logs by action
    /// </summary>
    Task<List<Entities.AuditLog>> SearchByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
}
