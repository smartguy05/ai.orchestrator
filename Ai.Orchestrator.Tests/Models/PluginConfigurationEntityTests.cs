using Ai.Orchestrator.Models.Entities;
using System.Text.Json;

namespace Ai.Orchestrator.Tests.Models;

/// <summary>
/// TDD Tests for PluginConfiguration entity
/// </summary>
public class PluginConfigurationEntityTests
{
    [Fact]
    public void PluginConfiguration_ShouldCreateInstanceWithValidProperties()
    {
        // Arrange & Act
        var config = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"smtp.gmail.com\"}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, config.Id);
        Assert.NotNull(config.AgentId);
        Assert.Equal("Ai.Orchestrator.Plugins.Email", config.PluginName);
        Assert.Equal("{\"smtp\":\"smtp.gmail.com\"}", config.ConfigurationJson);
        Assert.True(config.IsActive);
        Assert.NotEqual(default(DateTime), config.CreatedAt);
        Assert.NotEqual(default(DateTime), config.UpdatedAt);
    }

    [Fact]
    public void PluginConfiguration_AgentId_CanBeNullForDefaultConfig()
    {
        // Arrange & Act
        var config = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = null,  // NULL indicates default/global configuration
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"default\":true}"
        };

        // Assert
        Assert.Null(config.AgentId);
        Assert.Equal("Ai.Orchestrator.Plugins.Email", config.PluginName);
    }

    [Fact]
    public void PluginConfiguration_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var config = new PluginConfiguration();

        // Assert
        Assert.True(config.IsActive);
    }

    [Fact]
    public void PluginConfiguration_ShouldStoreComplexJsonConfiguration()
    {
        // Arrange
        var complexConfig = new
        {
            emailAccounts = new[]
            {
                new { name = "Account1", email = "test1@example.com", smtp = "smtp.gmail.com" },
                new { name = "Account2", email = "test2@example.com", smtp = "smtp.outlook.com" }
            },
            defaultAccount = "Account1",
            timeout = 30
        };

        var json = JsonSerializer.Serialize(complexConfig);

        // Act
        var config = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = json
        };

        // Assert
        Assert.Equal(json, config.ConfigurationJson);

        // Verify it can be deserialized back
        var deserializedConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(config.ConfigurationJson);
        Assert.NotNull(deserializedConfig);
        Assert.True(deserializedConfig.ContainsKey("emailAccounts"));
        Assert.True(deserializedConfig.ContainsKey("defaultAccount"));
        Assert.True(deserializedConfig.ContainsKey("timeout"));
    }

    [Fact]
    public void PluginConfiguration_ShouldSupportNavigationToAgent()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid()
        };

        var config = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}",
            Agent = agent
        };

        // Assert
        Assert.Equal(agent.Id, config.AgentId);
        Assert.Equal(agent.Name, config.Agent.Name);
    }

    [Fact]
    public void PluginConfiguration_DefaultConfig_ShouldNotHaveAgentNavigation()
    {
        // Arrange & Act
        var defaultConfig = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = null,  // Default configuration
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}",
            Agent = null
        };

        // Assert
        Assert.Null(defaultConfig.AgentId);
        Assert.Null(defaultConfig.Agent);
    }

    [Theory]
    [InlineData("Ai.Orchestrator.Plugins.Email")]
    [InlineData("Ai.Orchestrator.Plugins.OpenAi")]
    [InlineData("Ai.Orchestrator.Plugins.Memories")]
    [InlineData("Ai.Orchestrator.Plugins.WebSearch")]
    public void PluginConfiguration_ShouldAcceptVariousPluginNames(string pluginName)
    {
        // Arrange & Act
        var config = new PluginConfiguration
        {
            PluginName = pluginName,
            ConfigurationJson = "{}"
        };

        // Assert
        Assert.Equal(pluginName, config.PluginName);
    }

    [Fact]
    public void PluginConfiguration_UpdatedAt_ShouldBeUpdatableIndependently()
    {
        // Arrange
        var config = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var originalUpdatedAt = config.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        config.UpdatedAt = DateTime.UtcNow;

        // Assert
        Assert.True(config.UpdatedAt > originalUpdatedAt);
        Assert.True(config.UpdatedAt > config.CreatedAt);
    }

    [Fact]
    public void PluginConfiguration_ShouldStoreLargeJsonConfiguration()
    {
        // Arrange - Create a large configuration object
        var largeConfig = new
        {
            agents = Enumerable.Range(1, 100).Select(i => new
            {
                name = $"Agent{i}",
                description = $"Description for agent {i}",
                model = "gpt-4",
                systemPrompt = new string('A', 1000)
            }).ToList()
        };

        var json = JsonSerializer.Serialize(largeConfig);

        // Act
        var config = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            PluginName = "Ai.Orchestrator.Plugins.OpenAi",
            ConfigurationJson = json
        };

        // Assert
        Assert.Equal(json, config.ConfigurationJson);
        Assert.True(config.ConfigurationJson.Length > 10000);
    }

    [Fact]
    public void PluginConfiguration_ShouldHandleEmptyJsonObject()
    {
        // Arrange & Act
        var config = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            PluginName = "Ai.Orchestrator.Plugins.Test",
            ConfigurationJson = "{}"
        };

        // Assert
        Assert.Equal("{}", config.ConfigurationJson);

        // Verify it's valid JSON
        var deserializedConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(config.ConfigurationJson);
        Assert.NotNull(deserializedConfig);
        Assert.Empty(deserializedConfig);
    }
}
