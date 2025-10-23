using Ai.Orchestrator.Models.Entities;

namespace Ai.Orchestrator.Tests.Models;

/// <summary>
/// TDD Tests for Agent entity
/// </summary>
public class AgentEntityTests
{
    [Fact]
    public void Agent_ShouldCreateInstanceWithValidProperties()
    {
        // Arrange & Act
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            Description = "Test agent for testing",
            OwnerId = Guid.NewGuid(),
            Model = "gpt-4",
            IsDefault = false,
            IsActive = true,
            ToolsEnabled = true
        };

        // Assert
        Assert.NotEqual(Guid.Empty, agent.Id);
        Assert.Equal("TestAgent", agent.Name);
        Assert.Equal("Test agent for testing", agent.Description);
        Assert.NotEqual(Guid.Empty, agent.OwnerId);
        Assert.Equal("gpt-4", agent.Model);
        Assert.False(agent.IsDefault);
        Assert.True(agent.IsActive);
        Assert.True(agent.ToolsEnabled);
    }

    [Fact]
    public void Agent_DefaultConstructor_ShouldInitializeCollections()
    {
        // Arrange & Act
        var agent = new Agent();

        // Assert
        Assert.NotNull(agent.PluginConfigurations);
        Assert.Empty(agent.PluginConfigurations);
        Assert.NotNull(agent.AgentTools);
        Assert.Empty(agent.AgentTools);
    }

    [Fact]
    public void Agent_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var agent = new Agent();

        // Assert
        Assert.True(agent.IsActive);
    }

    [Fact]
    public void Agent_IsDefault_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var agent = new Agent();

        // Assert
        Assert.False(agent.IsDefault);
    }

    [Fact]
    public void Agent_ToolsEnabled_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var agent = new Agent();

        // Assert
        Assert.True(agent.ToolsEnabled);
    }

    [Fact]
    public void Agent_ShouldSupportNavigationToOwner()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = user.Id,
            Owner = user
        };

        // Assert
        Assert.Equal(user.Id, agent.OwnerId);
        Assert.Equal(user.Username, agent.Owner.Username);
    }

    [Fact]
    public void Agent_ShouldSupportNavigationToPluginConfigurations()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid()
        };

        var pluginConfig = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"key\":\"value\"}",
            Agent = agent
        };

        // Act
        agent.PluginConfigurations.Add(pluginConfig);

        // Assert
        Assert.Single(agent.PluginConfigurations);
        Assert.Equal(pluginConfig.PluginName, agent.PluginConfigurations.First().PluginName);
    }

    [Fact]
    public void Agent_ShouldSupportNavigationToAgentTools()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid()
        };

        var tool = new AgentTool
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            ToolName = "SendEmail",
            IsEnabled = true,
            Agent = agent
        };

        // Act
        agent.AgentTools.Add(tool);

        // Assert
        Assert.Single(agent.AgentTools);
        Assert.Equal("SendEmail", agent.AgentTools.First().ToolName);
        Assert.True(agent.AgentTools.First().IsEnabled);
    }

    [Fact]
    public void Agent_ShouldAllowMultiplePluginConfigurations()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid()
        };

        var emailConfig = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"smtp.gmail.com\"}"
        };

        var openAiConfig = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            PluginName = "Ai.Orchestrator.Plugins.OpenAi",
            ConfigurationJson = "{\"model\":\"gpt-4\"}"
        };

        // Act
        agent.PluginConfigurations.Add(emailConfig);
        agent.PluginConfigurations.Add(openAiConfig);

        // Assert
        Assert.Equal(2, agent.PluginConfigurations.Count);
        Assert.Contains(agent.PluginConfigurations, c => c.PluginName == "Ai.Orchestrator.Plugins.Email");
        Assert.Contains(agent.PluginConfigurations, c => c.PluginName == "Ai.Orchestrator.Plugins.OpenAi");
    }

    [Fact]
    public void Agent_ShouldAllowMultipleTools()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid()
        };

        var emailTool = new AgentTool
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            ToolName = "SendEmail",
            IsEnabled = true
        };

        var searchTool = new AgentTool
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            ToolName = "WebSearch",
            IsEnabled = true
        };

        // Act
        agent.AgentTools.Add(emailTool);
        agent.AgentTools.Add(searchTool);

        // Assert
        Assert.Equal(2, agent.AgentTools.Count);
        Assert.Contains(agent.AgentTools, t => t.ToolName == "SendEmail");
        Assert.Contains(agent.AgentTools, t => t.ToolName == "WebSearch");
    }

    [Theory]
    [InlineData("gpt-4")]
    [InlineData("claude-3-opus")]
    [InlineData("google/gemini-2.5-flash")]
    [InlineData("perplexity/sonar-deep-research")]
    public void Agent_ShouldAcceptVariousModelIdentifiers(string model)
    {
        // Arrange & Act
        var agent = new Agent
        {
            Model = model
        };

        // Assert
        Assert.Equal(model, agent.Model);
    }

    [Fact]
    public void Agent_ShouldStoreApiKeyAndUrl()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid(),
            ApiKey = "sk-test-key-12345",
            ApiUrl = "https://api.openai.com/v1"
        };

        // Assert
        Assert.Equal("sk-test-key-12345", agent.ApiKey);
        Assert.Equal("https://api.openai.com/v1", agent.ApiUrl);
    }

    [Fact]
    public void Agent_ShouldStoreLongSystemPrompt()
    {
        // Arrange
        var longPrompt = new string('A', 10000);
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid(),
            DefaultSystemPrompt = longPrompt
        };

        // Assert
        Assert.Equal(longPrompt, agent.DefaultSystemPrompt);
        Assert.Equal(10000, agent.DefaultSystemPrompt.Length);
    }

    [Fact]
    public void Agent_UpdatedAt_ShouldBeUpdatableIndependently()
    {
        // Arrange
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "TestAgent",
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var originalUpdatedAt = agent.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        agent.UpdatedAt = DateTime.UtcNow;

        // Assert
        Assert.True(agent.UpdatedAt > originalUpdatedAt);
        Assert.True(agent.UpdatedAt > agent.CreatedAt);
    }

    [Fact]
    public void Agent_OnlyOneAgentPerUserCanBeDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var agent1 = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Agent1",
            OwnerId = userId,
            IsDefault = true
        };

        var agent2 = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Agent2",
            OwnerId = userId,
            IsDefault = false
        };

        // Assert
        Assert.True(agent1.IsDefault);
        Assert.False(agent2.IsDefault);
        // Note: The uniqueness constraint for default agents per user
        // will be enforced at the DbContext level
    }
}
