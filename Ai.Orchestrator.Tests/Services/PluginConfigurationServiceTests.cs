using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.PluginConfigs;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Services.PluginConfigs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Ai.Orchestrator.Tests.Services;

/// <summary>
/// TDD Tests for PluginConfigurationService
/// Tests plugin configuration management with default fallback logic
/// </summary>
public class PluginConfigurationServiceTests : IDisposable
{
    private readonly OrchestratorDbContext _context;
    private readonly PluginConfigurationService _pluginConfigService;
    private readonly Guid _testUserId;
    private readonly Guid _testAgentId;
    private readonly Guid _otherUserId;
    private readonly Guid _otherAgentId;

    public PluginConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestratorDbContext(options);

        // Create test data
        _testUserId = Guid.NewGuid();
        _testAgentId = Guid.NewGuid();
        _otherUserId = Guid.NewGuid();
        _otherAgentId = Guid.NewGuid();
        SeedTestData();

        _pluginConfigService = new PluginConfigurationService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var testUser = new User
        {
            Id = _testUserId,
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        testUser.SetPassword("Password123");

        var otherUser = new User
        {
            Id = _otherUserId,
            Username = "otheruser",
            Email = "other@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        otherUser.SetPassword("Password123");

        var testAgent = new Agent
        {
            Id = _testAgentId,
            Name = "TestAgent",
            OwnerId = _testUserId,
            Model = "gpt-4",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var otherAgent = new Agent
        {
            Id = _otherAgentId,
            Name = "OtherAgent",
            OwnerId = _otherUserId,
            Model = "gpt-4",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(testUser, otherUser);
        _context.Agents.AddRange(testAgent, otherAgent);
        _context.SaveChanges();
    }

    #region Create Configuration Tests

    [Fact]
    public async Task CreateConfigurationAsync_ShouldCreateAgentSpecificConfig()
    {
        // Arrange
        var config = new { smtp = "smtp.gmail.com", port = 587 };
        var request = new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = JsonSerializer.Serialize(config)
        };

        // Act
        var result = await _pluginConfigService.CreateConfigurationAsync(_testUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testAgentId, result.AgentId);
        Assert.Equal("Ai.Orchestrator.Plugins.Email", result.PluginName);
        Assert.Contains("smtp.gmail.com", result.ConfigurationJson);
    }

    [Fact]
    public async Task CreateConfigurationAsync_ShouldCreateDefaultConfig_WhenAgentIdIsNull()
    {
        // Arrange
        var config = new { defaultSetting = "value" };
        var request = new CreatePluginConfigRequest
        {
            AgentId = null,  // Default configuration
            PluginName = "Ai.Orchestrator.Plugins.Test",
            ConfigurationJson = JsonSerializer.Serialize(config)
        };

        // Act
        var result = await _pluginConfigService.CreateConfigurationAsync(_testUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AgentId);
        Assert.Equal("Ai.Orchestrator.Plugins.Test", result.PluginName);
    }

    [Fact]
    public async Task CreateConfigurationAsync_ShouldThrowException_ForDuplicateAgentPluginCombo()
    {
        // Arrange
        var request1 = new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"test\":\"value\"}"
        };

        var request2 = new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"test\":\"value2\"}"
        };

        // Act
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, request1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _pluginConfigService.CreateConfigurationAsync(_testUserId, request2));
    }

    [Fact]
    public async Task CreateConfigurationAsync_ShouldThrowException_WhenNonOwnerCreatesForAgent()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,  // Owned by testUser
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"test\":\"value\"}"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _pluginConfigService.CreateConfigurationAsync(_otherUserId, request));
    }

    [Fact]
    public async Task CreateConfigurationAsync_ShouldThrowException_ForNonExistentAgent()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = Guid.NewGuid(),  // Non-existent agent
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"test\":\"value\"}"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _pluginConfigService.CreateConfigurationAsync(_testUserId, request));
    }

    #endregion

    #region Get Configuration Tests

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnAgentConfig_WhenExists()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"smtp.gmail.com\"}"
        };
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, request);

        // Act
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email", _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testAgentId, result.AgentId);
        Assert.Contains("smtp.gmail.com", result.ConfigurationJson);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnDefaultConfig_WhenAgentConfigNotExists()
    {
        // Arrange - Create default config
        var defaultRequest = new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"default.smtp.com\"}"
        };
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, defaultRequest);

        // Act - Request for agent that has no specific config
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email", _testUserId);

        // Assert - Should get default config
        Assert.NotNull(result);
        Assert.Null(result.AgentId);
        Assert.Contains("default.smtp.com", result.ConfigurationJson);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnAgentConfig_EvenWhenDefaultExists()
    {
        // Arrange - Create both default and agent-specific config
        var defaultRequest = new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"default.smtp.com\"}"
        };
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, defaultRequest);

        var agentRequest = new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"agent.smtp.com\"}"
        };
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, agentRequest);

        // Act
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email", _testUserId);

        // Assert - Should get agent-specific config, not default
        Assert.NotNull(result);
        Assert.Equal(_testAgentId, result.AgentId);
        Assert.Contains("agent.smtp.com", result.ConfigurationJson);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnNull_WhenNoConfigExists()
    {
        // Act
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.NonExistent", _testUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnNull_WhenNonOwnerRequests()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"smtp.gmail.com\"}"
        };
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, request);

        // Act - Other user tries to access
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email", _otherUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAgentConfigurationsAsync_ShouldReturnAllConfigsForAgent()
    {
        // Arrange
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.WebSearch",
            ConfigurationJson = "{}"
        });

        // Act
        var result = await _pluginConfigService.GetAgentConfigurationsAsync(_testAgentId, _testUserId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDefaultConfigurationsAsync_ShouldReturnAllDefaultConfigs()
    {
        // Arrange
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.WebSearch",
            ConfigurationJson = "{}"
        });

        // Act
        var result = await _pluginConfigService.GetDefaultConfigurationsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Null(c.AgentId));
    }

    #endregion

    #region Update Configuration Tests

    [Fact]
    public async Task UpdateConfigurationAsync_ShouldUpdateConfig()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"smtp\":\"old.smtp.com\"}"
        });

        var updateRequest = new UpdatePluginConfigRequest
        {
            ConfigurationJson = "{\"smtp\":\"new.smtp.com\"}"
        };

        // Act
        var result = await _pluginConfigService.UpdateConfigurationAsync(created.Id, _testUserId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("new.smtp.com", result.ConfigurationJson);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ShouldThrowException_WhenNonOwnerUpdates()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        var updateRequest = new UpdatePluginConfigRequest
        {
            ConfigurationJson = "{\"hacked\":\"true\"}"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _pluginConfigService.UpdateConfigurationAsync(created.Id, _otherUserId, updateRequest));
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ShouldUpdateIsActive()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        var updateRequest = new UpdatePluginConfigRequest
        {
            ConfigurationJson = "{}",
            IsActive = false
        };

        // Act
        var result = await _pluginConfigService.UpdateConfigurationAsync(created.Id, _testUserId, updateRequest);

        // Assert
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_CanUpdateDefaultConfig()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = null,  // Default config
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"old\":\"value\"}"
        });

        var updateRequest = new UpdatePluginConfigRequest
        {
            ConfigurationJson = "{\"new\":\"value\"}"
        };

        // Act
        var result = await _pluginConfigService.UpdateConfigurationAsync(created.Id, _testUserId, updateRequest);

        // Assert
        Assert.Contains("new", result.ConfigurationJson);
    }

    #endregion

    #region Delete Configuration Tests

    [Fact]
    public async Task DeleteConfigurationAsync_ShouldRemoveConfig()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        // Act
        await _pluginConfigService.DeleteConfigurationAsync(created.Id, _testUserId);

        // Assert
        var deleted = await _context.PluginConfigurations.FindAsync(created.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ShouldThrowException_WhenNonOwnerDeletes()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _pluginConfigService.DeleteConfigurationAsync(created.Id, _otherUserId));
    }

    [Fact]
    public async Task DeleteConfigurationAsync_CanDeleteDefaultConfig()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        // Act
        await _pluginConfigService.DeleteConfigurationAsync(created.Id, _testUserId);

        // Assert
        var deleted = await _context.PluginConfigurations.FindAsync(created.Id);
        Assert.Null(deleted);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ConfigurationExistsAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var created = await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        // Act
        var exists = await _pluginConfigService.ConfigurationExistsAsync(created.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ConfigurationExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var exists = await _pluginConfigService.ConfigurationExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task HasAgentConfigurationAsync_ShouldReturnTrue_WhenAgentHasConfig()
    {
        // Arrange
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        // Act
        var hasConfig = await _pluginConfigService.HasAgentConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email");

        // Assert
        Assert.True(hasConfig);
    }

    [Fact]
    public async Task HasAgentConfigurationAsync_ShouldReturnFalse_WhenAgentHasNoConfig()
    {
        // Act
        var hasConfig = await _pluginConfigService.HasAgentConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email");

        // Assert
        Assert.False(hasConfig);
    }

    [Fact]
    public async Task HasDefaultConfigurationAsync_ShouldReturnTrue_WhenDefaultExists()
    {
        // Arrange
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{}"
        });

        // Act
        var hasDefault = await _pluginConfigService.HasDefaultConfigurationAsync("Ai.Orchestrator.Plugins.Email");

        // Assert
        Assert.True(hasDefault);
    }

    [Fact]
    public async Task HasDefaultConfigurationAsync_ShouldReturnFalse_WhenDefaultNotExists()
    {
        // Act
        var hasDefault = await _pluginConfigService.HasDefaultConfigurationAsync("Ai.Orchestrator.Plugins.Email");

        // Assert
        Assert.False(hasDefault);
    }

    #endregion

    #region Fallback Logic Tests

    [Fact]
    public async Task FallbackLogic_ShouldPreferAgentConfig_OverDefault()
    {
        // Arrange - Create both configs
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"priority\":\"default\"}"
        });

        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = _testAgentId,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"priority\":\"agent\"}"
        });

        // Act
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email", _testUserId);

        // Assert
        Assert.Contains("agent", result.ConfigurationJson);
        Assert.DoesNotContain("default", result.ConfigurationJson);
    }

    [Fact]
    public async Task FallbackLogic_ShouldUseDefault_WhenNoAgentConfig()
    {
        // Arrange - Only default config
        await _pluginConfigService.CreateConfigurationAsync(_testUserId, new CreatePluginConfigRequest
        {
            AgentId = null,
            PluginName = "Ai.Orchestrator.Plugins.Email",
            ConfigurationJson = "{\"priority\":\"default\"}"
        });

        // Act
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email", _testUserId);

        // Assert
        Assert.Contains("default", result.ConfigurationJson);
        Assert.Null(result.AgentId);
    }

    [Fact]
    public async Task FallbackLogic_ShouldReturnNull_WhenNoConfigs()
    {
        // Act
        var result = await _pluginConfigService.GetConfigurationAsync(
            _testAgentId, "Ai.Orchestrator.Plugins.Email", _testUserId);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
