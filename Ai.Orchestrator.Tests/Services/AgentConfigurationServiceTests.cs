using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Agents;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Services.Agents;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Tests.Services;

/// <summary>
/// TDD Tests for AgentConfigurationService
/// Tests agent CRUD operations with owner-based access control
/// </summary>
public class AgentConfigurationServiceTests : IDisposable
{
    private readonly OrchestratorDbContext _context;
    private readonly AgentConfigurationService _agentService;
    private readonly Guid _testUserId;
    private readonly Guid _otherUserId;

    public AgentConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrchestratorDbContext(options);

        // Create test users
        _testUserId = Guid.NewGuid();
        _otherUserId = Guid.NewGuid();
        SeedTestUsers();

        _agentService = new AgentConfigurationService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestUsers()
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

        _context.Users.AddRange(testUser, otherUser);
        _context.SaveChanges();
    }

    #region Create Agent Tests

    [Fact]
    public async Task CreateAgentAsync_ShouldCreateAgent_WithValidData()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "TestAgent",
            Description = "Test agent description",
            Model = "gpt-4",
            ApiKey = "test-api-key",
            ApiUrl = "https://api.openai.com/v1",
            DefaultSystemPrompt = "You are a helpful assistant",
            ToolsEnabled = true
        };

        // Act
        var result = await _agentService.CreateAgentAsync(_testUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("TestAgent", result.Name);
        Assert.Equal(_testUserId, result.OwnerId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAgentAsync_ShouldSetFirstAgentAsDefault()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "FirstAgent",
            Description = "First agent",
            Model = "gpt-4"
        };

        // Act
        var result = await _agentService.CreateAgentAsync(_testUserId, request);

        // Assert
        Assert.True(result.IsDefault);
    }

    [Fact]
    public async Task CreateAgentAsync_ShouldNotSetSecondAgentAsDefault()
    {
        // Arrange
        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "FirstAgent",
            Model = "gpt-4"
        });

        var secondRequest = new CreateAgentRequest
        {
            Name = "SecondAgent",
            Model = "gpt-4"
        };

        // Act
        var result = await _agentService.CreateAgentAsync(_testUserId, secondRequest);

        // Assert
        Assert.False(result.IsDefault);
    }

    [Fact]
    public async Task CreateAgentAsync_WithEnabledTools_ShouldCreateAgentTools()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4",
            EnabledTools = new List<string> { "SendEmail", "WebSearch", "ReadFile" }
        };

        // Act
        var result = await _agentService.CreateAgentAsync(_testUserId, request);

        // Assert
        Assert.Equal(3, result.EnabledTools.Count);
        Assert.Contains("SendEmail", result.EnabledTools);
        Assert.Contains("WebSearch", result.EnabledTools);
        Assert.Contains("ReadFile", result.EnabledTools);
    }

    [Fact]
    public async Task CreateAgentAsync_ShouldThrowException_ForDuplicateNamePerUser()
    {
        // Arrange
        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "DuplicateAgent",
            Model = "gpt-4"
        });

        var duplicateRequest = new CreateAgentRequest
        {
            Name = "DuplicateAgent",
            Model = "gpt-4"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.CreateAgentAsync(_testUserId, duplicateRequest));
    }

    [Fact]
    public async Task CreateAgentAsync_ShouldAllowSameNameForDifferentUsers()
    {
        // Arrange
        var request1 = new CreateAgentRequest { Name = "SameName", Model = "gpt-4" };
        var request2 = new CreateAgentRequest { Name = "SameName", Model = "gpt-4" };

        // Act
        var agent1 = await _agentService.CreateAgentAsync(_testUserId, request1);
        var agent2 = await _agentService.CreateAgentAsync(_otherUserId, request2);

        // Assert
        Assert.NotEqual(agent1.Id, agent2.Id);
        Assert.Equal("SameName", agent1.Name);
        Assert.Equal("SameName", agent2.Name);
    }

    [Fact]
    public async Task CreateAgentAsync_ShouldThrowException_ForNonExistentUser()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var request = new CreateAgentRequest { Name = "TestAgent", Model = "gpt-4" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.CreateAgentAsync(nonExistentUserId, request));
    }

    #endregion

    #region Get Agent Tests

    [Fact]
    public async Task GetAgentByIdAsync_ShouldReturnAgent_WhenOwnerRequests()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        var result = await _agentService.GetAgentByIdAsync(agent.Id, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agent.Id, result.Id);
    }

    [Fact]
    public async Task GetAgentByIdAsync_ShouldReturnNull_WhenNonOwnerRequests()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        var result = await _agentService.GetAgentByIdAsync(agent.Id, _otherUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAgentByIdAsync_ShouldReturnNull_WhenAgentNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _agentService.GetAgentByIdAsync(nonExistentId, _testUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserAgentsAsync_ShouldReturnAllUserAgents()
    {
        // Arrange
        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent1",
            Model = "gpt-4"
        });

        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent2",
            Model = "claude-3"
        });

        // Act
        var result = await _agentService.GetUserAgentsAsync(_testUserId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserAgentsAsync_ShouldReturnOnlyActiveAgents()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "ActiveAgent",
            Model = "gpt-4"
        });

        await _agentService.DeactivateAgentAsync(agent.Id, _testUserId);

        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "AnotherAgent",
            Model = "gpt-4"
        });

        // Act
        var result = await _agentService.GetUserAgentsAsync(_testUserId);

        // Assert
        Assert.Single(result);
        Assert.Equal("AnotherAgent", result.First().Name);
    }

    [Fact]
    public async Task GetUserAgentsAsync_ShouldNotReturnOtherUsersAgents()
    {
        // Arrange
        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "User1Agent",
            Model = "gpt-4"
        });

        await _agentService.CreateAgentAsync(_otherUserId, new CreateAgentRequest
        {
            Name = "User2Agent",
            Model = "gpt-4"
        });

        // Act
        var result = await _agentService.GetUserAgentsAsync(_testUserId);

        // Assert
        Assert.Single(result);
        Assert.Equal("User1Agent", result.First().Name);
    }

    [Fact]
    public async Task GetDefaultAgentAsync_ShouldReturnDefaultAgent()
    {
        // Arrange
        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "FirstAgent",
            Model = "gpt-4"
        });

        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "SecondAgent",
            Model = "gpt-4"
        });

        // Act
        var result = await _agentService.GetDefaultAgentAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FirstAgent", result.Name);
        Assert.True(result.IsDefault);
    }

    [Fact]
    public async Task GetDefaultAgentAsync_ShouldReturnNull_WhenNoDefaultAgent()
    {
        // Act
        var result = await _agentService.GetDefaultAgentAsync(_testUserId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Update Agent Tests

    [Fact]
    public async Task UpdateAgentAsync_ShouldUpdateAgentProperties()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "OriginalName",
            Description = "Original description",
            Model = "gpt-4"
        });

        var updateRequest = new UpdateAgentRequest
        {
            Name = "UpdatedName",
            Description = "Updated description",
            Model = "gpt-4-turbo"
        };

        // Act
        var result = await _agentService.UpdateAgentAsync(agent.Id, _testUserId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UpdatedName", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal("gpt-4-turbo", result.Model);
    }

    [Fact]
    public async Task UpdateAgentAsync_ShouldThrowException_WhenNonOwnerUpdates()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        var updateRequest = new UpdateAgentRequest { Name = "HackedName" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _agentService.UpdateAgentAsync(agent.Id, _otherUserId, updateRequest));
    }

    [Fact]
    public async Task UpdateAgentAsync_ShouldThrowException_ForDuplicateName()
    {
        // Arrange
        await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent1",
            Model = "gpt-4"
        });

        var agent2 = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent2",
            Model = "gpt-4"
        });

        var updateRequest = new UpdateAgentRequest { Name = "Agent1" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _agentService.UpdateAgentAsync(agent2.Id, _testUserId, updateRequest));
    }

    [Fact]
    public async Task UpdateAgentAsync_ShouldUpdateTools()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4",
            EnabledTools = new List<string> { "SendEmail" }
        });

        var updateRequest = new UpdateAgentRequest
        {
            EnabledTools = new List<string> { "WebSearch", "ReadFile" }
        };

        // Act
        var result = await _agentService.UpdateAgentAsync(agent.Id, _testUserId, updateRequest);

        // Assert
        Assert.Equal(2, result.EnabledTools.Count);
        Assert.Contains("WebSearch", result.EnabledTools);
        Assert.Contains("ReadFile", result.EnabledTools);
        Assert.DoesNotContain("SendEmail", result.EnabledTools);
    }

    [Fact]
    public async Task SetDefaultAgentAsync_ShouldSetAgentAsDefault()
    {
        // Arrange
        var agent1 = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent1",
            Model = "gpt-4"
        });

        var agent2 = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent2",
            Model = "gpt-4"
        });

        // Act
        await _agentService.SetDefaultAgentAsync(agent2.Id, _testUserId);

        // Assert
        var defaultAgent = await _agentService.GetDefaultAgentAsync(_testUserId);
        Assert.Equal(agent2.Id, defaultAgent.Id);
    }

    [Fact]
    public async Task SetDefaultAgentAsync_ShouldUnsetPreviousDefault()
    {
        // Arrange
        var agent1 = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent1",
            Model = "gpt-4"
        });

        var agent2 = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "Agent2",
            Model = "gpt-4"
        });

        // Act
        await _agentService.SetDefaultAgentAsync(agent2.Id, _testUserId);

        // Assert
        var agent1Updated = await _agentService.GetAgentByIdAsync(agent1.Id, _testUserId);
        Assert.False(agent1Updated.IsDefault);
    }

    [Fact]
    public async Task SetDefaultAgentAsync_ShouldThrowException_WhenNonOwnerSetsDefault()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _agentService.SetDefaultAgentAsync(agent.Id, _otherUserId));
    }

    [Fact]
    public async Task DeactivateAgentAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        await _agentService.DeactivateAgentAsync(agent.Id, _testUserId);

        // Assert
        var deactivatedAgent = await _context.Agents.FindAsync(agent.Id);
        Assert.False(deactivatedAgent.IsActive);
    }

    [Fact]
    public async Task DeactivateAgentAsync_ShouldThrowException_WhenNonOwnerDeactivates()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _agentService.DeactivateAgentAsync(agent.Id, _otherUserId));
    }

    #endregion

    #region Delete Agent Tests

    [Fact]
    public async Task DeleteAgentAsync_ShouldRemoveAgent()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        await _agentService.DeleteAgentAsync(agent.Id, _testUserId);

        // Assert
        var deletedAgent = await _context.Agents.FindAsync(agent.Id);
        Assert.Null(deletedAgent);
    }

    [Fact]
    public async Task DeleteAgentAsync_ShouldThrowException_WhenNonOwnerDeletes()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _agentService.DeleteAgentAsync(agent.Id, _otherUserId));
    }

    [Fact]
    public async Task DeleteAgentAsync_ShouldRemoveAssociatedTools()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4",
            EnabledTools = new List<string> { "SendEmail", "WebSearch" }
        });

        // Act
        await _agentService.DeleteAgentAsync(agent.Id, _testUserId);

        // Assert
        var tools = await _context.AgentTools.Where(t => t.AgentId == agent.Id).ToListAsync();
        Assert.Empty(tools);
    }

    #endregion

    #region Agent Tools Tests

    [Fact]
    public async Task GetAgentToolsAsync_ShouldReturnEnabledTools()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4",
            EnabledTools = new List<string> { "SendEmail", "WebSearch" }
        });

        // Act
        var tools = await _agentService.GetAgentToolsAsync(agent.Id, _testUserId);

        // Assert
        Assert.Equal(2, tools.Count);
        Assert.Contains("SendEmail", tools);
        Assert.Contains("WebSearch", tools);
    }

    [Fact]
    public async Task EnableToolAsync_ShouldAddToolToAgent()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        await _agentService.EnableToolAsync(agent.Id, _testUserId, "SendEmail");

        // Assert
        var tools = await _agentService.GetAgentToolsAsync(agent.Id, _testUserId);
        Assert.Contains("SendEmail", tools);
    }

    [Fact]
    public async Task EnableToolAsync_ShouldNotDuplicateTool()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4",
            EnabledTools = new List<string> { "SendEmail" }
        });

        // Act
        await _agentService.EnableToolAsync(agent.Id, _testUserId, "SendEmail");

        // Assert
        var tools = await _agentService.GetAgentToolsAsync(agent.Id, _testUserId);
        Assert.Single(tools);
    }

    [Fact]
    public async Task DisableToolAsync_ShouldRemoveToolFromAgent()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4",
            EnabledTools = new List<string> { "SendEmail", "WebSearch" }
        });

        // Act
        await _agentService.DisableToolAsync(agent.Id, _testUserId, "SendEmail");

        // Assert
        var tools = await _agentService.GetAgentToolsAsync(agent.Id, _testUserId);
        Assert.Single(tools);
        Assert.DoesNotContain("SendEmail", tools);
    }

    [Fact]
    public async Task EnableToolAsync_ShouldThrowException_WhenNonOwner()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _agentService.EnableToolAsync(agent.Id, _otherUserId, "SendEmail"));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task AgentExistsAsync_ShouldReturnTrue_WhenAgentExists()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        var exists = await _agentService.AgentExistsAsync(agent.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AgentExistsAsync_ShouldReturnFalse_WhenAgentNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _agentService.AgentExistsAsync(nonExistentId);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task IsOwnerAsync_ShouldReturnTrue_WhenUserIsOwner()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        var isOwner = await _agentService.IsOwnerAsync(agent.Id, _testUserId);

        // Assert
        Assert.True(isOwner);
    }

    [Fact]
    public async Task IsOwnerAsync_ShouldReturnFalse_WhenUserIsNotOwner()
    {
        // Arrange
        var agent = await _agentService.CreateAgentAsync(_testUserId, new CreateAgentRequest
        {
            Name = "TestAgent",
            Model = "gpt-4"
        });

        // Act
        var isOwner = await _agentService.IsOwnerAsync(agent.Id, _otherUserId);

        // Assert
        Assert.False(isOwner);
    }

    #endregion
}
