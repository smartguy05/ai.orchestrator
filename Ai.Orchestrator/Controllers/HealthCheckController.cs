using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ai.Orchestrator.Controllers;

/// <summary>
/// Controller for application health monitoring
/// Provides detailed health status for monitoring systems
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
    }

    /// <summary>
    /// Get comprehensive health status with details
    /// </summary>
    /// <returns>Health status with component details</returns>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var healthReport = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration.TotalMilliseconds,
            Checks = healthReport.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration.TotalMilliseconds,
                Exception = e.Value.Exception?.Message,
                Data = e.Value.Data
            })
        };

        // Healthy and Degraded return 200 OK, Unhealthy returns 503
        return healthReport.Status == HealthStatus.Unhealthy
            ? StatusCode(503, response) // Service Unavailable
            : Ok(response);
    }

    /// <summary>
    /// Get simple health status (for load balancers)
    /// </summary>
    /// <returns>Simple OK/Unhealthy response</returns>
    [HttpGet("simple")]
    public async Task<IActionResult> GetHealthSimple()
    {
        var healthReport = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            Status = healthReport.Status.ToString()
        };

        return healthReport.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }

    /// <summary>
    /// Get database-specific health status
    /// </summary>
    /// <returns>Database health status</returns>
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        var healthReport = await _healthCheckService.CheckHealthAsync(
            registration => registration.Name == "database");

        if (!healthReport.Entries.TryGetValue("database", out var databaseEntry))
        {
            return NotFound(new { message = "Database health check not configured" });
        }

        var response = new
        {
            Status = databaseEntry.Status.ToString(),
            Description = databaseEntry.Description,
            Duration = databaseEntry.Duration.TotalMilliseconds,
            Exception = databaseEntry.Exception?.Message,
            Data = databaseEntry.Data
        };

        return databaseEntry.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }
}