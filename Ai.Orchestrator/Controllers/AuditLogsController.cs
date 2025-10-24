using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ai.Orchestrator.Controllers;

/// <summary>
/// Controller for viewing audit logs (Admin only)
/// Provides endpoints for security monitoring and compliance reporting
/// </summary>
[ApiController]
[Route("api/auditlogs")]
[Authorize(Policy = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    /// <summary>
    /// Get audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID to query</param>
    /// <param name="startDate">Start date filter (optional)</param>
    /// <param name="endDate">End date filter (optional)</param>
    /// <param name="limit">Maximum number of logs to return (default: 100, max: 1000)</param>
    /// <returns>List of audit logs for the user</returns>
    /// <response code="200">Returns the audit logs</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="401">Unauthorized (not an admin)</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// **Requires Admin role**
    ///
    /// Sample request:
    ///
    ///     GET /api/auditlogs/user/3fa85f64-5717-4562-b3fc-2c963f66afa6?startDate=2024-01-01&amp;limit=50
    ///     Authorization: Bearer {admin-token}
    ///
    /// Use cases:
    /// - View all actions performed by a user
    /// - Audit trail for compliance
    /// - Security investigation
    /// </remarks>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<AuditLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserAuditLogs(
        Guid userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            if (limit <= 0 || limit > 1000)
            {
                return BadRequest(new { message = "Limit must be between 1 and 1000" });
            }

            var logs = await _auditService.GetUserAuditLogsAsync(userId, startDate, endDate, limit);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving audit logs", error = ex.Message });
        }
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Entity type (e.g., "Agent", "PluginConfig", "User")</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="limit">Maximum number of logs to return (default: 100, max: 1000)</param>
    /// <returns>List of audit logs for the entity</returns>
    /// <response code="200">Returns the audit logs</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="401">Unauthorized (not an admin)</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// **Requires Admin role**
    ///
    /// Sample request:
    ///
    ///     GET /api/auditlogs/entity/Agent/3fa85f64-5717-4562-b3fc-2c963f66afa6?limit=50
    ///     Authorization: Bearer {admin-token}
    ///
    /// Use cases:
    /// - View all actions performed on a specific agent
    /// - Track configuration changes
    /// - Compliance audit trail
    /// </remarks>
    [HttpGet("entity/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(List<AuditLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEntityAuditLogs(
        string entityType,
        Guid entityId,
        [FromQuery] int limit = 100)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                return BadRequest(new { message = "Entity type is required" });
            }

            if (limit <= 0 || limit > 1000)
            {
                return BadRequest(new { message = "Limit must be between 1 and 1000" });
            }

            var logs = await _auditService.GetEntityAuditLogsAsync(entityType, entityId, limit);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving audit logs", error = ex.Message });
        }
    }

    /// <summary>
    /// Get failed security events for monitoring
    /// </summary>
    /// <param name="startDate">Start date filter (optional)</param>
    /// <param name="limit">Maximum number of logs to return (default: 100, max: 1000)</param>
    /// <returns>List of failed security events</returns>
    /// <response code="200">Returns the failed security events</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="401">Unauthorized (not an admin)</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// **Requires Admin role**
    ///
    /// Sample request:
    ///
    ///     GET /api/auditlogs/security/failed?startDate=2024-01-01&amp;limit=50
    ///     Authorization: Bearer {admin-token}
    ///
    /// Returns all failed login attempts, unauthorized access attempts, and other security failures.
    ///
    /// Use cases:
    /// - Security monitoring
    /// - Detect brute force attacks
    /// - Identify compromised accounts
    /// </remarks>
    [HttpGet("security/failed")]
    [ProducesResponseType(typeof(List<AuditLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFailedSecurityEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            if (limit <= 0 || limit > 1000)
            {
                return BadRequest(new { message = "Limit must be between 1 and 1000" });
            }

            var logs = await _auditService.GetFailedSecurityEventsAsync(startDate, limit);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving failed security events", error = ex.Message });
        }
    }

    /// <summary>
    /// Search audit logs by action type
    /// </summary>
    /// <param name="action">Action type (e.g., "Login", "CreateAgent", "UpdateConfig")</param>
    /// <param name="startDate">Start date filter (optional)</param>
    /// <param name="endDate">End date filter (optional)</param>
    /// <param name="limit">Maximum number of logs to return (default: 100, max: 1000)</param>
    /// <returns>List of audit logs matching the action</returns>
    /// <response code="200">Returns the audit logs</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="401">Unauthorized (not an admin)</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// **Requires Admin role**
    ///
    /// Sample request:
    ///
    ///     GET /api/auditlogs/search/action/Login?startDate=2024-01-01&amp;limit=50
    ///     Authorization: Bearer {admin-token}
    ///
    /// Use cases:
    /// - Find all instances of a specific action
    /// - Compliance reporting
    /// - Security analysis
    /// </remarks>
    [HttpGet("search/action/{action?}")]
    [ProducesResponseType(typeof(List<AuditLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchByAction(
        string action,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return BadRequest(new { message = "Action is required" });
            }

            if (limit <= 0 || limit > 1000)
            {
                return BadRequest(new { message = "Limit must be between 1 and 1000" });
            }

            var logs = await _auditService.SearchByActionAsync(action, startDate, endDate, limit);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while searching audit logs", error = ex.Message });
        }
    }
}
