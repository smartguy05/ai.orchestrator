using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ai.Orchestrator.Tests.Integration;

/// <summary>
/// Integration tests for refresh token functionality
/// Tests token rotation, revocation, and expiration
/// </summary>
[Trait("Category", "Integration")]
public class RefreshTokenIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RefreshTokenIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Signal that we're in integration test mode - prevents PostgreSQL registration
        Environment.SetEnvironmentVariable("IS_INTEGRATION_TEST", "true");

        // Set minimal required environment variables for testing
        Environment.SetEnvironmentVariable("LoggingPluginsString", "");
        Environment.SetEnvironmentVariable("PluginDirectory", "./plugins");
        Environment.SetEnvironmentVariable("ConfigDirectory", "./configs");
        Environment.SetEnvironmentVariable("RedisConnectionString", "localhost:6379");
        Environment.SetEnvironmentVariable("RedisConversationSubject", "test");
        Environment.SetEnvironmentVariable("ConfirmationPlugin", "");
        Environment.SetEnvironmentVariable("ActivePlugins", "");
        Environment.SetEnvironmentVariable("JwtSecret", "test-secret-key-that-is-long-enough-for-testing-purposes-minimum-32-bytes");
        Environment.SetEnvironmentVariable("JwtIssuer", "TestIssuer");
        Environment.SetEnvironmentVariable("JwtAudience", "TestAudience");
        Environment.SetEnvironmentVariable("JwtExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("RefreshTokenExpirationDays", "7");

        // Use a unique but consistent database name for this test instance
        var databaseName = $"RefreshTokenIntegrationTestDb_{Guid.NewGuid()}";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Register InMemory database BEFORE startup configuration runs
            builder.ConfigureServices((context, services) =>
            {
                // Add InMemory database for testing - this runs before MiddlewareRegistration
                // so the check in MiddlewareRegistration will see it and skip PostgreSQL
                services.AddDbContext<OrchestratorDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });

        _client = _factory.CreateClient();

        // Ensure database is created and seeded
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        db.Database.EnsureCreated();
    }

    #region Login with Refresh Token Tests

    [Fact]
    public async Task Login_ShouldReturnRefreshToken()
    {
        // Arrange
        await RegisterUserAsync("refreshuser1", "refresh1@example.com");
        var loginRequest = new LoginRequest
        {
            Username = "refreshuser1",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        Assert.NotNull(loginResult.Token);
        Assert.NotNull(loginResult.RefreshToken);
        Assert.NotEmpty(loginResult.RefreshToken);
    }

    #endregion

    #region Refresh Token Endpoint Tests

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var (accessToken, refreshToken) = await LoginAndGetTokensAsync("refreshuser2", "refresh2@example.com");

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEqual(accessToken, result.Token); // Should be a new access token
        Assert.NotEqual(refreshToken, result.RefreshToken); // Should be a new refresh token
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-token-12345"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ShouldReturn401()
    {
        // Arrange
        var (_, refreshToken) = await LoginAndGetTokensAsync("refreshuser3", "refresh3@example.com");

        // Manually expire the token in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        var tokenEntity = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (tokenEntity != null)
        {
            tokenEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // Expire it
            await db.SaveChangesAsync();
        }

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WhenUsedTwice_ShouldFailSecondTime()
    {
        // Arrange
        var (_, refreshToken) = await LoginAndGetTokensAsync("refreshuser4", "refresh4@example.com");

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Act - First refresh (should succeed)
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Act - Second refresh with same token (should fail - token rotation)
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    #endregion

    #region Token Revocation Tests

    [Fact]
    public async Task RevokeToken_WithValidToken_ShouldReturn200()
    {
        // Arrange
        var (accessToken, refreshToken) = await LoginAndGetTokensAsync("refreshuser5", "refresh5@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var revokeRequest = new RevokeTokenRequest
        {
            RefreshToken = refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/revoke", revokeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RevokeToken_ThenRefresh_ShouldFail()
    {
        // Arrange
        var (accessToken, refreshToken) = await LoginAndGetTokensAsync("refreshuser6", "refresh6@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Revoke the token
        await _client.PostAsJsonAsync("/api/auth/revoke", new RevokeTokenRequest { RefreshToken = refreshToken });

        // Clear authorization for next request
        _client.DefaultRequestHeaders.Authorization = null;

        // Act - Try to refresh with revoked token
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = refreshToken });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokeToken_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var revokeRequest = new RevokeTokenRequest
        {
            RefreshToken = "some-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/revoke", revokeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task RegisterUserAsync(string username, string email)
    {
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
    }

    private async Task<(string accessToken, string refreshToken)> LoginAndGetTokensAsync(string username, string email)
    {
        await RegisterUserAsync(username, email);

        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (loginResult.Token, loginResult.RefreshToken);
    }

    #endregion
}
