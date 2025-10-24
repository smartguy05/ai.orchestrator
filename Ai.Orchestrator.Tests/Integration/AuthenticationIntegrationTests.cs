using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.DTOs.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ai.Orchestrator.Tests.Integration;

/// <summary>
/// Integration tests for authentication flow
/// Tests the complete user registration, login, and JWT token usage workflow
/// </summary>
[Trait("Category", "Integration")]
public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
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
        var databaseName = $"AuthIntegrationTestDb_{Guid.NewGuid()}";

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

    #region Registration Tests

    [Fact]
    public async Task RegisterUser_ShouldReturn200_WithValidData()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("test@example.com", user.Email);
        Assert.Contains("User", user.Roles);
    }

    [Fact]
    public async Task RegisterUser_ShouldReturn400_WhenDuplicateUsername()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "duplicate",
            Email = "user1@example.com",
            Password = "Password123!"
        };

        // Act - Register first user
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Try to register with same username
        var request2 = new RegisterRequest
        {
            Username = "duplicate",
            Email = "user2@example.com",
            Password = "Password123!"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterUser_ShouldReturn400_WhenPasswordTooShort()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "weakpass",
            Email = "weak@example.com",
            Password = "short"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturn200_WithValidCredentials()
    {
        // Arrange - Register user first
        var registerRequest = new RegisterRequest
        {
            Username = "logintest",
            Email = "login@example.com",
            Password = "LoginPassword123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Username = "logintest",
            Password = "LoginPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.Token);
        Assert.Equal("logintest", loginResponse.Username);
        Assert.Contains("User", loginResponse.Roles);
        Assert.True(loginResponse.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WithInvalidCredentials()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "nonexistent",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WithWrongPassword()
    {
        // Arrange - Register user
        var registerRequest = new RegisterRequest
        {
            Username = "wrongpasstest",
            Email = "wrongpass@example.com",
            Password = "CorrectPassword123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - Try to login with wrong password
        var loginRequest = new LoginRequest
        {
            Username = "wrongpasstest",
            Password = "WrongPassword123!"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Token Usage Tests

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturn401_WithoutToken()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturn200_WithValidToken()
    {
        // Arrange - Register and login
        var token = await RegisterAndLoginAsync("tokentest", "token@example.com", "TokenPassword123!");

        // Act - Call protected endpoint with token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/agents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturn401_WithInvalidToken()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        // Act
        var response = await _client.GetAsync("/api/agents");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Complete Authentication Flow

    [Fact]
    public async Task CompleteAuthFlow_RegisterLoginAndAccessProtectedEndpoint()
    {
        // Step 1: Register
        var registerRequest = new RegisterRequest
        {
            Username = "fullflowtest",
            Email = "fullflow@example.com",
            Password = "FullFlowPassword123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Step 2: Login
        var loginRequest = new LoginRequest
        {
            Username = "fullflowtest",
            Password = "FullFlowPassword123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        Assert.NotEmpty(loginResult.Token);

        // Step 3: Access protected endpoint with token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        var protectedResponse = await _client.GetAsync("/api/agents");
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);

        // Step 4: Verify we can create an agent
        var createAgentRequest = new
        {
            Name = "TestAgent",
            Model = "gpt-4",
            DefaultSystemPrompt = "You are a helpful assistant"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/agents", createAgentRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<string> RegisterAndLoginAsync(string username, string email, string password)
    {
        // Register
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Login
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return loginResult.Token;
    }

    #endregion
}
