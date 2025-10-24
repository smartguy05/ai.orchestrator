using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Auth;
using Ai.Orchestrator.Models.DTOs.Agents;
using Ai.Orchestrator.Models.DTOs.PluginConfigs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ai.Orchestrator.Tests.Integration;

/// <summary>
/// Integration tests for plugin configuration with 3-tier fallback logic
/// Tests: Agent-specific config > Default config > JSON file fallback
/// </summary>
[Trait("Category", "Integration")]
public class PluginConfigIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PluginConfigIntegrationTests(WebApplicationFactory<Program> factory)
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
        var databaseName = $"PluginConfigIntegrationTestDb_{Guid.NewGuid()}";

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

    #region Create Plugin Configuration Tests

    [Fact]
    public async Task CreatePluginConfig_AgentSpecific_ShouldReturn201()
    {
        // Arrange
        var (token, agentId) = await SetupUserAndAgentAsync("configuser", "config@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreatePluginConfigRequest
        {
            AgentId = agentId,
            PluginName = "Email",
            ConfigurationJson = "{\"smtp\":\"smtp.example.com\",\"port\":587}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/plugin-configs", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<PluginConfigurationDto>();
        Assert.NotNull(config);
        Assert.Equal(agentId, config.AgentId);
        Assert.Equal("Email", config.PluginName);
    }

    [Fact]
    public async Task CreatePluginConfig_Default_ShouldReturn201()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("defaultconfig", "default@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreatePluginConfigRequest
        {
            AgentId = null, // Default configuration
            PluginName = "OpenAI",
            ConfigurationJson = "{\"apiKey\":\"default-key\",\"model\":\"gpt-4\"}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/plugin-configs", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<PluginConfigurationDto>();
        Assert.NotNull(config);
        Assert.Null(config.AgentId); // Should be default config
        Assert.Equal("OpenAI", config.PluginName);
    }

    #endregion

    #region 3-Tier Fallback Tests

    [Fact]
    public async Task GetConfiguration_ShouldReturnAgentSpecific_WhenExists()
    {
        // Arrange
        var (token, agentId) = await SetupUserAndAgentAsync("fallback1", "fallback1@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create default config
        await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Email",
            ConfigurationJson = "{\"smtp\":\"default.smtp.com\"}"
        });

        // Create agent-specific config
        await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = agentId,
            PluginName = "Email",
            ConfigurationJson = "{\"smtp\":\"agent.smtp.com\"}"
        });

        // Act - Get configuration (should return agent-specific)
        var response = await _client.GetAsync($"/api/plugin-configs/agent/{agentId}/Email");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<PluginConfigurationDto>();
        Assert.NotNull(config);
        Assert.Equal(agentId, config.AgentId); // Agent-specific
        Assert.Contains("agent.smtp.com", config.ConfigurationJson);
    }

    [Fact]
    public async Task GetConfiguration_ShouldReturnDefault_WhenNoAgentSpecific()
    {
        // Arrange
        var (token, agentId) = await SetupUserAndAgentAsync("fallback2", "fallback2@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create ONLY default config
        await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "OpenAI",
            ConfigurationJson = "{\"apiKey\":\"default-key\"}"
        });

        // Act - Get configuration (should return default)
        var response = await _client.GetAsync($"/api/plugin-configs/agent/{agentId}/OpenAI");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<PluginConfigurationDto>();
        Assert.NotNull(config);
        Assert.Null(config.AgentId); // Default config
        Assert.Contains("default-key", config.ConfigurationJson);
    }

    [Fact]
    public async Task GetConfiguration_ShouldReturn404_WhenNeitherExists()
    {
        // Arrange
        var (token, agentId) = await SetupUserAndAgentAsync("fallback3", "fallback3@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Get configuration with no configs created
        var response = await _client.GetAsync($"/api/plugin-configs/agent/{agentId}/NonExistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        // Caller should fall back to JSON file in this case
    }

    #endregion

    #region Update Plugin Configuration Tests

    [Fact]
    public async Task UpdatePluginConfig_ShouldReturn200_WhenOwner()
    {
        // Arrange
        var (token, agentId) = await SetupUserAndAgentAsync("updateconfig", "updateconfig@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = agentId,
            PluginName = "Email",
            ConfigurationJson = "{\"smtp\":\"old.smtp.com\"}"
        });
        var config = await createResponse.Content.ReadFromJsonAsync<PluginConfigurationDto>();

        var updateRequest = new UpdatePluginConfigRequest
        {
            ConfigurationJson = "{\"smtp\":\"new.smtp.com\"}",
            IsActive = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/plugin-configs/{config.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PluginConfigurationDto>();
        Assert.Contains("new.smtp.com", updated.ConfigurationJson);
    }

    #endregion

    #region Delete Plugin Configuration Tests

    [Fact]
    public async Task DeletePluginConfig_ShouldReturn200_WhenOwner()
    {
        // Arrange
        var (token, agentId) = await SetupUserAndAgentAsync("deleteconfig", "deleteconfig@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = agentId,
            PluginName = "Email",
            ConfigurationJson = "{\"smtp\":\"delete.smtp.com\"}"
        });
        var config = await createResponse.Content.ReadFromJsonAsync<PluginConfigurationDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/plugin-configs/{config.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Get All Configurations Tests

    [Fact]
    public async Task GetAgentConfigurations_ShouldReturnAllForAgent()
    {
        // Arrange
        var (token, agentId) = await SetupUserAndAgentAsync("multiconfig", "multiconfig@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create multiple configs for agent
        await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = agentId,
            PluginName = "Email",
            ConfigurationJson = "{}"
        });
        await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = agentId,
            PluginName = "OpenAI",
            ConfigurationJson = "{}"
        });

        // Act
        var response = await _client.GetAsync($"/api/plugin-configs/agent/{agentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var configs = await response.Content.ReadFromJsonAsync<List<PluginConfigurationDto>>();
        Assert.NotNull(configs);
        Assert.Equal(2, configs.Count);
    }

    [Fact]
    public async Task GetDefaultConfigurations_ShouldReturnAllDefaults()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("defaultstest", "defaults@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create multiple default configs
        await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Email",
            ConfigurationJson = "{}"
        });
        await _client.PostAsJsonAsync("/api/plugin-configs", new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "OpenAI",
            ConfigurationJson = "{}"
        });

        // Act
        var response = await _client.GetAsync("/api/plugin-configs/defaults");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var configs = await response.Content.ReadFromJsonAsync<List<PluginConfigurationDto>>();
        Assert.NotNull(configs);
        Assert.Equal(2, configs.Count);
        Assert.All(configs, c => Assert.Null(c.AgentId));
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task CreatePluginConfig_ShouldReturn401_WithoutAuthentication()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            PluginName = "Email",
            ConfigurationJson = "{}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/plugin-configs", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private async Task<(string token, Guid agentId)> SetupUserAndAgentAsync(string username, string email)
    {
        var token = await RegisterAndLoginAsync(username, email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createAgentResponse = await _client.PostAsJsonAsync("/api/agents", new CreateAgentRequest
        {
            Name = $"{username}-agent",
            Model = "gpt-4"
        });
        var agent = await createAgentResponse.Content.ReadFromJsonAsync<AgentDto>();

        return (token, agent.Id);
    }

    #endregion
}
