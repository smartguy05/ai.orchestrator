using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ai.Orchestrator.Tests.Integration;

/// <summary>
/// Integration tests for audit log viewer endpoints
/// Tests admin-only access and various query filters
/// </summary>
[Trait("Category", "Integration")]
public class AuditLogsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuditLogsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OrchestratorDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<OrchestratorDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"AuditLogsTestDb_{Guid.NewGuid()}");
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    #region Authorization Tests

    [Fact]
    public async Task GetUserAuditLogs_WithoutAuthentication_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync($"/api/auditlogs/user/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserAuditLogs_AsNonAdmin_ShouldReturn403()
    {
        // Arrange - Create non-admin user
        var (token, _) = await CreateUserWithRoleAsync("regularuser", "regular@example.com", new List<string> { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/auditlogs/user/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserAuditLogs_AsAdmin_ShouldReturn200()
    {
        // Arrange - Create admin user
        var (token, userId) = await CreateAdminUserAsync("adminuser", "admin@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/auditlogs/user/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Get User Audit Logs Tests

    [Fact]
    public async Task GetUserAuditLogs_ShouldReturnLogsForUser()
    {
        // Arrange
        var (adminToken, adminUserId) = await CreateAdminUserAsync("admin1", "admin1@example.com");
        var (userToken, userId) = await CreateUserWithRoleAsync("testuser1", "testuser1@example.com", new List<string> { "User" });

        // Create some audit logs for the user
        await CreateTestAuditLogsAsync(userId, "testuser1", 5);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/auditlogs/user/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLog>>();
        Assert.NotNull(logs);
        Assert.True(logs.Count >= 5); // At least the 5 we created (may include registration/login logs)
        Assert.All(logs, log => Assert.Equal(userId, log.UserId));
    }

    [Fact]
    public async Task GetUserAuditLogs_WithDateFilter_ShouldReturnFilteredLogs()
    {
        // Arrange
        var (adminToken, adminUserId) = await CreateAdminUserAsync("admin2", "admin2@example.com");
        var (userToken, userId) = await CreateUserWithRoleAsync("testuser2", "testuser2@example.com", new List<string> { "User" });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var startDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var response = await _client.GetAsync($"/api/auditlogs/user/{userId}?startDate={startDate:O}&limit=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLog>>();
        Assert.NotNull(logs);
        Assert.All(logs, log => Assert.True(log.Timestamp >= startDate));
    }

    #endregion

    #region Get Entity Audit Logs Tests

    [Fact]
    public async Task GetEntityAuditLogs_ShouldReturnLogsForEntity()
    {
        // Arrange
        var (adminToken, adminUserId) = await CreateAdminUserAsync("admin3", "admin3@example.com");

        var entityId = Guid.NewGuid();
        await CreateTestAuditLogsForEntityAsync(adminUserId, "admin3", "Agent", entityId, 3);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/auditlogs/entity/Agent/{entityId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLog>>();
        Assert.NotNull(logs);
        Assert.Equal(3, logs.Count);
        Assert.All(logs, log => Assert.Equal(entityId, log.EntityId));
        Assert.All(logs, log => Assert.Equal("Agent", log.EntityType));
    }

    #endregion

    #region Get Failed Security Events Tests

    [Fact]
    public async Task GetFailedSecurityEvents_ShouldReturnFailedEvents()
    {
        // Arrange
        var (adminToken, adminUserId) = await CreateAdminUserAsync("admin4", "admin4@example.com");

        // Create some failed security events
        await CreateFailedSecurityEventsAsync(3);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/auditlogs/security/failed");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLog>>();
        Assert.NotNull(logs);
        Assert.True(logs.Count >= 3); // At least the 3 we created
        Assert.All(logs, log =>
        {
            Assert.Equal("Security", log.EntityType);
            Assert.True(log.Outcome == "Failure" || log.Outcome == "Unauthorized");
        });
    }

    #endregion

    #region Search By Action Tests

    [Fact]
    public async Task SearchByAction_ShouldReturnMatchingLogs()
    {
        // Arrange
        var (adminToken, adminUserId) = await CreateAdminUserAsync("admin5", "admin5@example.com");

        await CreateTestAuditLogsWithActionAsync(adminUserId, "admin5", "CreateAgent", 4);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/auditlogs/search/action/CreateAgent");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<AuditLog>>();
        Assert.NotNull(logs);
        Assert.True(logs.Count >= 4);
        Assert.All(logs, log => Assert.Equal("CreateAgent", log.Action));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task GetUserAuditLogs_WithInvalidLimit_ShouldReturn400()
    {
        // Arrange
        var (adminToken, _) = await CreateAdminUserAsync("admin6", "admin6@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act - Limit too high
        var response = await _client.GetAsync($"/api/auditlogs/user/{Guid.NewGuid()}?limit=5000");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchByAction_WithEmptyAction_ShouldReturn400()
    {
        // Arrange
        var (adminToken, _) = await CreateAdminUserAsync("admin7", "admin7@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/auditlogs/search/action/ ");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<(string token, Guid userId)> CreateAdminUserAsync(string username, string email)
    {
        return await CreateUserWithRoleAsync(username, email, new List<string> { "Admin" });
    }

    private async Task<(string token, Guid userId)> CreateUserWithRoleAsync(string username, string email, List<string> roles)
    {
        // Register user
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assign roles
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user != null)
        {
            // Remove existing roles
            var existingRoles = await db.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
            db.UserRoles.RemoveRange(existingRoles);

            // Add new roles
            foreach (var roleName in roles)
            {
                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
            }
            await db.SaveChangesAsync();
        }

        // Login to get token
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = "Password123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (loginResult.Token, user.Id);
    }

    private async Task CreateTestAuditLogsAsync(Guid userId, string username, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        for (int i = 0; i < count; i++)
        {
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Username = username,
                Action = $"TestAction{i}",
                EntityType = "Test",
                Outcome = "Success",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task CreateTestAuditLogsForEntityAsync(Guid userId, string username, string entityType, Guid entityId, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        for (int i = 0; i < count; i++)
        {
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Username = username,
                Action = $"Update{entityType}",
                EntityType = entityType,
                EntityId = entityId,
                Outcome = "Success",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task CreateFailedSecurityEventsAsync(int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        for (int i = 0; i < count; i++)
        {
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = null,
                Username = "Anonymous",
                Action = "FailedLogin",
                EntityType = "Security",
                Outcome = i % 2 == 0 ? "Failure" : "Unauthorized",
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Details = "Invalid credentials"
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task CreateTestAuditLogsWithActionAsync(Guid userId, string username, string action, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

        for (int i = 0; i < count; i++)
        {
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Username = username,
                Action = action,
                EntityType = "Agent",
                Outcome = "Success",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync();
    }

    #endregion
}
