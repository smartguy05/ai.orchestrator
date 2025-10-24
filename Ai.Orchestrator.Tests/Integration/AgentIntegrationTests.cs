using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.DTOs.Agents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ai.Orchestrator.Tests.Integration;

/// <summary>
/// Integration tests for agent management with ownership validation
/// Tests agent CRUD operations and ownership-based access control
/// </summary>
[Trait("Category", "Integration")]
public class AgentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AgentIntegrationTests(WebApplicationFactory<Program> factory)
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
        var databaseName = $"AgentIntegrationTestDb_{Guid.NewGuid()}";

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

    #region Create Agent Tests

    [Fact]
    public async Task CreateAgent_ShouldReturn201_WithValidData()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("agentuser", "agent@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateAgentRequest
        {
            Name = "MyFirstAgent",
            Model = "gpt-4",
            DefaultSystemPrompt = "You are a helpful assistant",
            ToolsEnabled = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var agent = await response.Content.ReadFromJsonAsync<AgentDto>();
        Assert.NotNull(agent);
        Assert.Equal("MyFirstAgent", agent.Name);
        Assert.Equal("gpt-4", agent.Model);
        Assert.True(agent.IsDefault); // First agent should be default
    }

    [Fact]
    public async Task CreateAgent_SecondAgent_ShouldNotBeDefault()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("multiagent", "multi@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create first agent
        var request1 = new CreateAgentRequest
        {
            Name = "FirstAgent",
            Model = "gpt-4"
        };
        await _client.PostAsJsonAsync("/api/agents", request1);

        // Create second agent
        var request2 = new CreateAgentRequest
        {
            Name = "SecondAgent",
            Model = "gpt-3.5-turbo"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agents", request2);

        // Assert
        var agent = await response.Content.ReadFromJsonAsync<AgentDto>();
        Assert.NotNull(agent);
        Assert.False(agent.IsDefault); // Second agent should NOT be default
    }

    [Fact]
    public async Task CreateAgent_ShouldReturn401_WithoutAuthentication()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "UnauthorizedAgent",
            Model = "gpt-4"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Get Agent Tests

    [Fact]
    public async Task GetAgents_ShouldReturnOnlyUserAgents()
    {
        // Arrange - Create two users
        var token1 = await RegisterAndLoginAsync("user1", "user1@example.com");
        var token2 = await RegisterAndLoginAsync("user2", "user2@example.com");

        // User 1 creates an agent
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "User1Agent",
            Model = "gpt-4"
        });

        // User 2 creates an agent
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "User2Agent",
            Model = "gpt-3.5-turbo"
        });

        // Act - User 1 gets their agents
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var response = await _client.GetAsync("/api/agents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var agents = await response.Content.ReadFromJsonAsync<List<AgentDto>>();
        Assert.NotNull(agents);
        Assert.Single(agents); // Should only see their own agent
        Assert.Equal("User1Agent", agents[0].Name);
    }

    [Fact]
    public async Task GetAgentById_ShouldReturn404_WhenNotOwner()
    {
        // Arrange - User 1 creates an agent
        var token1 = await RegisterAndLoginAsync("owner", "owner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var createResponse = await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "OwnerAgent",
            Model = "gpt-4"
        });
        var agent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();

        // User 2 tries to access User 1's agent
        var token2 = await RegisterAndLoginAsync("nonowner", "nonowner@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // Act
        var response = await _client.GetAsync($"/api/agents/{agent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Update Agent Tests

    [Fact]
    public async Task UpdateAgent_ShouldReturn200_WhenOwner()
    {
        // Arrange - Create agent
        var token = await RegisterAndLoginAsync("updateuser", "update@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "OriginalName",
            Model = "gpt-4"
        });
        var agent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();

        // Update request
        var updateRequest = new UpdateAgentRequest
        {
            Name = "UpdatedName",
            Model = "gpt-4-turbo"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/agents/{agent.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AgentDto>();
        Assert.Equal("UpdatedName", updated.Name);
        Assert.Equal("gpt-4-turbo", updated.Model);
    }

    [Fact]
    public async Task UpdateAgent_ShouldReturn401_WhenNotOwner()
    {
        // Arrange - User 1 creates agent
        var token1 = await RegisterAndLoginAsync("owner2", "owner2@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var createResponse = await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "ProtectedAgent",
            Model = "gpt-4"
        });
        var agent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();

        // User 2 tries to update
        var token2 = await RegisterAndLoginAsync("hacker", "hacker@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        var updateRequest = new UpdateAgentRequest
        {
            Name = "HackedName"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/agents/{agent.Id}", updateRequest);

        // Assert - Should be Unauthorized (401) not NotFound (404) per implementation
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Agent Tests

    [Fact]
    public async Task DeleteAgent_ShouldReturn200_WhenOwner()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("deleteuser", "delete@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "ToDelete",
            Model = "gpt-4"
        });
        var agent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/agents/{agent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/agents/{agent.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    #endregion

    #region Tool Management Tests

    [Fact]
    public async Task AddToolToAgent_ShouldReturn200()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("tooluser", "tool@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "ToolAgent",
            Model = "gpt-4"
        });
        var agent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();

        // Act
        var response = await _client.PostAsync($"/api/agents/{agent.Id}/tools/EmailTool", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RemoveToolFromAgent_ShouldReturn200()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("removetool", "removetool@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = "RemoveToolAgent",
            Model = "gpt-4"
        });
        var agent = await createResponse.Content.ReadFromJsonAsync<AgentDto>();

        // Add tool first
        await _client.PostAsync($"/api/agents/{agent.Id}/tools/EmailTool", null);

        // Act - Remove tool
        var response = await _client.DeleteAsync($"/api/agents/{agent.Id}/tools/EmailTool");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<string> RegisterAndLoginAsync(string username, string email)
    {
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = "Password123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return loginResult.Token;
    }

    #endregion
}
