using Ai.Orchestrator.Controllers;
using Ai.Orchestrator.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace Ai.Orchestrator.Tests.Controllers;

/// <summary>
/// TDD Tests for HealthCheckController
/// Tests health check endpoints for monitoring
/// </summary>
public class HealthCheckControllerTests
{
    private readonly Mock<HealthCheckService> _mockHealthCheckService;
    private readonly HealthCheckController _controller;

    public HealthCheckControllerTests()
    {
        _mockHealthCheckService = new Mock<HealthCheckService>();
        _controller = new HealthCheckController(_mockHealthCheckService.Object);
    }

    #region GetHealth Tests

    [Fact]
    public async Task GetHealth_ShouldReturnOk_WhenAllHealthy()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Database is healthy",
                    TimeSpan.FromMilliseconds(50),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(50));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnServiceUnavailable_WhenUnhealthy()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Database connection failed",
                    TimeSpan.FromMilliseconds(5000),
                    new Exception("Connection timeout"),
                    null)
            },
            TimeSpan.FromMilliseconds(5000));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ShouldReturnDegraded_WhenDegraded()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Degraded,
                    "Database is slow",
                    TimeSpan.FromMilliseconds(2000),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(2000));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeDetails_InResponse()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "PostgreSQL connection successful",
                    TimeSpan.FromMilliseconds(50),
                    null,
                    new Dictionary<string, object>
                    {
                        ["database"] = "orchestrator",
                        ["server"] = "localhost:5432"
                    })
            },
            TimeSpan.FromMilliseconds(50));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetHealthSimple Tests

    [Fact]
    public async Task GetHealthSimple_ShouldReturnOk_WhenHealthy()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    null,
                    TimeSpan.FromMilliseconds(50),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(50));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealthSimple();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic response = okResult.Value;
        Assert.Equal("Healthy", response.Status);
    }

    [Fact]
    public async Task GetHealthSimple_ShouldReturnUnhealthy_WhenDatabaseDown()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    null,
                    TimeSpan.FromMilliseconds(5000),
                    new Exception("Connection failed"),
                    null)
            },
            TimeSpan.FromMilliseconds(5000));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealthSimple();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusCodeResult.StatusCode);
    }

    #endregion

    #region DatabaseHealth Tests

    [Fact]
    public async Task GetDatabaseHealth_ShouldReturnOk_WhenDatabaseHealthy()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Healthy,
                    "Database connection successful",
                    TimeSpan.FromMilliseconds(50),
                    null,
                    null)
            },
            TimeSpan.FromMilliseconds(50));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetDatabaseHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetDatabaseHealth_ShouldReturnServiceUnavailable_WhenDatabaseDown()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Connection timeout",
                    TimeSpan.FromMilliseconds(5000),
                    new Exception("Npgsql.NpgsqlException: Connection timeout"),
                    null)
            },
            TimeSpan.FromMilliseconds(5000));

        _mockHealthCheckService
            .Setup(s => s.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetDatabaseHealth();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, statusCodeResult.StatusCode);
    }

    #endregion
}
