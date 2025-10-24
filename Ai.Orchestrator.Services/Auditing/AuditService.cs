using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Services.Auditing;

/// <summary>
/// Service for logging audit events and security activities
/// </summary>
public class AuditService : IAuditService
{
    private readonly OrchestratorDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(OrchestratorDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Log a successful action
    /// </summary>
    public async Task LogSuccessAsync(string action, string entityType, Guid? entityId, Guid? userId, string username, string details = null)
    {
        await LogAuditAsync(action, entityType, entityId, userId, username, "Success", details);
    }

    /// <summary>
    /// Log a failed action
    /// </summary>
    public async Task LogFailureAsync(string action, string entityType, Guid? entityId, Guid? userId, string username, string details = null)
    {
        await LogAuditAsync(action, entityType, entityId, userId, username, "Failure", details);
    }

    /// <summary>
    /// Log an unauthorized access attempt
    /// </summary>
    public async Task LogUnauthorizedAsync(string action, string entityType, Guid? entityId, Guid? userId, string username, string details = null)
    {
        await LogAuditAsync(action, entityType, entityId, userId, username, "Unauthorized", details);
    }

    /// <summary>
    /// Log a security event (login, password reset, etc.)
    /// </summary>
    public async Task LogSecurityEventAsync(string action, Guid? userId, string username, string outcome, string details = null)
    {
        await LogAuditAsync(action, "Security", null, userId, username, outcome, details);
    }

    /// <summary>
    /// Log an HTTP request
    /// </summary>
    public async Task LogHttpRequestAsync(
        string httpMethod,
        string requestPath,
        int statusCode,
        long durationMs,
        Guid? userId,
        string username,
        string ipAddress,
        string userAgent)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username ?? "Anonymous",
            Action = "HttpRequest",
            EntityType = "Http",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Outcome = statusCode >= 200 && statusCode < 300 ? "Success" : statusCode >= 400 && statusCode < 500 ? "ClientError" : "ServerError",
            Timestamp = DateTime.UtcNow,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            StatusCode = statusCode,
            DurationMs = durationMs
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    public async Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
    {
        var query = _context.AuditLogs
            .Where(a => a.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId, int limit = 100)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Get failed security events (for monitoring)
    /// </summary>
    public async Task<List<AuditLog>> GetFailedSecurityEventsAsync(DateTime? startDate = null, int limit = 100)
    {
        var query = _context.AuditLogs
            .Where(a => a.EntityType == "Security" && (a.Outcome == "Failure" || a.Outcome == "Unauthorized"));

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Search audit logs by action
    /// </summary>
    public async Task<List<AuditLog>> SearchByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
    {
        var query = _context.AuditLogs
            .Where(a => a.Action == action);

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Internal method to log audit events
    /// </summary>
    private async Task LogAuditAsync(string action, string entityType, Guid? entityId, Guid? userId, string username, string outcome, string details)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username ?? "Anonymous",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = GetClientIpAddress(httpContext),
            UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
            Details = details,
            Outcome = outcome,
            Timestamp = DateTime.UtcNow,
            HttpMethod = httpContext?.Request?.Method,
            RequestPath = httpContext?.Request?.Path.ToString()
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get client IP address from HTTP context
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        if (context == null)
            return null;

        // Check for X-Forwarded-For header (load balancers, proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        // Check for X-Real-IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fall back to RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString();
    }
}
