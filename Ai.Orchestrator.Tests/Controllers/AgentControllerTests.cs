using Ai.Orchestrator.Controllers;
using Ai.Orchestrator.Models.DTOs.Agents;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ai.Orchestrator.Tests.Controllers;

/// <summary>
/// TDD Tests for AgentController
/// Tests agent management endpoints with ownership validation
/// </summary>
public class AgentControllerTests
{
    private readonly Mock<IAgentConfigurationService> _mockAgentService;
    private readonly AgentController _controller;
    private readonly Guid _currentUserId;

    public AgentControllerTests()
    {
        _mockAgentService = new Mock<IAgentConfigurationService>();
        _controller = new AgentController(_mockAgentService.Object);
        _currentUserId = Guid.NewGuid();

        // Setup controller context with authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUserId.ToString()),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #region GetAllAgents Tests

    [Fact]
    public async Task GetAllAgents_ShouldReturnOk_WithAgentList()
    {
        // Arrange
        var agents = new List<AgentDto>
        {
            new AgentDto { Id = Guid.NewGuid(), Name = "Agent1", Model = "gpt-4" },
            new AgentDto { Id = Guid.NewGuid(), Name = "Agent2", Model = "gpt-3.5-turbo" }
        };

        _mockAgentService
            .Setup(s => s.GetAllAgentsAsync(_currentUserId))
            .ReturnsAsync(agents);

        // Act
        var result = await _controller.GetAllAgents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedAgents = Assert.IsAssignableFrom<List<AgentDto>>(okResult.Value);
        Assert.Equal(2, returnedAgents.Count);
    }

    [Fact]
    public async Task GetAllAgents_ShouldReturnEmptyList_WhenNoAgents()
    {
        // Arrange
        _mockAgentService
            .Setup(s => s.GetAllAgentsAsync(_currentUserId))
            .ReturnsAsync(new List<AgentDto>());

        // Act
        var result = await _controller.GetAllAgents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedAgents = Assert.IsAssignableFrom<List<AgentDto>>(okResult.Value);
        Assert.Empty(returnedAgents);
    }

    [Fact]
    public async Task GetAllAgents_ShouldCallService_WithCurrentUserId()
    {
        // Arrange
        _mockAgentService
            .Setup(s => s.GetAllAgentsAsync(_currentUserId))
            .ReturnsAsync(new List<AgentDto>())
            .Verifiable();

        // Act
        await _controller.GetAllAgents();

        // Assert
        _mockAgentService.Verify();
    }

    [Fact]
    public async Task GetAllAgents_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        _mockAgentService
            .Setup(s => s.GetAllAgentsAsync(_currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllAgents();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetAgentById Tests

    [Fact]
    public async Task GetAgentById_ShouldReturnOk_WhenAgentExists()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agentDto = new AgentDto
        {
            Id = agentId,
            Name = "TestAgent",
            Model = "gpt-4",
            OwnerUsername = "testuser"
        };

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agentId, _currentUserId))
            .ReturnsAsync(agentDto);

        // Act
        var result = await _controller.GetAgentById(agentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedAgent = Assert.IsType<AgentDto>(okResult.Value);
        Assert.Equal(agentId, returnedAgent.Id);
        Assert.Equal("TestAgent", returnedAgent.Name);
    }

    [Fact]
    public async Task GetAgentById_ShouldReturnNotFound_WhenAgentDoesNotExist()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agentId, _currentUserId))
            .ReturnsAsync((AgentDto)null);

        // Act
        var result = await _controller.GetAgentById(agentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task GetAgentById_ShouldReturnNotFound_WhenUserNotOwner()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agentId, _currentUserId))
            .ReturnsAsync((AgentDto)null); // Service returns null for non-owners

        // Act
        var result = await _controller.GetAgentById(agentId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAgentById_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agentId, _currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAgentById(agentId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region CreateAgent Tests

    [Fact]
    public async Task CreateAgent_ShouldReturnCreated_WhenAgentCreated()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "NewAgent",
            Model = "gpt-4",
            ApiKey = "sk-test",
            DefaultSystemPrompt = "You are a helpful assistant"
        };

        var createdAgent = new AgentDto
        {
            Id = Guid.NewGuid(),
            Name = "NewAgent",
            Model = "gpt-4",
            OwnerUsername = "testuser"
        };

        _mockAgentService
            .Setup(s => s.CreateAgentAsync(_currentUserId, request))
            .ReturnsAsync(createdAgent);

        // Act
        var result = await _controller.CreateAgent(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetAgentById), createdResult.ActionName);
        var returnedAgent = Assert.IsType<AgentDto>(createdResult.Value);
        Assert.Equal("NewAgent", returnedAgent.Name);
    }

    [Fact]
    public async Task CreateAgent_ShouldReturnBadRequest_WhenDuplicateName()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "ExistingAgent",
            Model = "gpt-4"
        };

        _mockAgentService
            .Setup(s => s.CreateAgentAsync(_currentUserId, request))
            .ThrowsAsync(new InvalidOperationException("Agent with name 'ExistingAgent' already exists"));

        // Act
        var result = await _controller.CreateAgent(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already exists", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task CreateAgent_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "",
            Model = "gpt-4"
        };

        _mockAgentService
            .Setup(s => s.CreateAgentAsync(_currentUserId, request))
            .ThrowsAsync(new ArgumentException("Name cannot be empty"));

        // Act
        var result = await _controller.CreateAgent(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("cannot be empty", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task CreateAgent_FirstAgent_ShouldBeDefault()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "FirstAgent",
            Model = "gpt-4"
        };

        var createdAgent = new AgentDto
        {
            Id = Guid.NewGuid(),
            Name = "FirstAgent",
            Model = "gpt-4",
            IsDefault = true
        };

        _mockAgentService
            .Setup(s => s.CreateAgentAsync(_currentUserId, request))
            .ReturnsAsync(createdAgent);

        // Act
        var result = await _controller.CreateAgent(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedAgent = Assert.IsType<AgentDto>(createdResult.Value);
        Assert.True(returnedAgent.IsDefault);
    }

    [Fact]
    public async Task CreateAgent_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var request = new CreateAgentRequest { Name = "Agent", Model = "gpt-4" };

        _mockAgentService
            .Setup(s => s.CreateAgentAsync(_currentUserId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateAgent(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region UpdateAgent Tests

    [Fact]
    public async Task UpdateAgent_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new UpdateAgentRequest
        {
            Name = "UpdatedAgent",
            Model = "gpt-4-turbo"
        };

        var updatedAgent = new AgentDto
        {
            Id = agentId,
            Name = "UpdatedAgent",
            Model = "gpt-4-turbo"
        };

        _mockAgentService
            .Setup(s => s.UpdateAgentAsync(agentId, _currentUserId, request))
            .ReturnsAsync(updatedAgent);

        // Act
        var result = await _controller.UpdateAgent(agentId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedAgent = Assert.IsType<AgentDto>(okResult.Value);
        Assert.Equal("UpdatedAgent", returnedAgent.Name);
    }

    [Fact]
    public async Task UpdateAgent_ShouldReturnNotFound_WhenAgentDoesNotExist()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new UpdateAgentRequest { Name = "Updated" };

        _mockAgentService
            .Setup(s => s.UpdateAgentAsync(agentId, _currentUserId, request))
            .ThrowsAsync(new InvalidOperationException($"Agent with ID '{agentId}' not found"));

        // Act
        var result = await _controller.UpdateAgent(agentId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateAgent_ShouldReturnUnauthorized_WhenUserNotOwner()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new UpdateAgentRequest { Name = "Updated" };

        _mockAgentService
            .Setup(s => s.UpdateAgentAsync(agentId, _currentUserId, request))
            .ThrowsAsync(new UnauthorizedAccessException("You do not have permission to update this agent"));

        // Act
        var result = await _controller.UpdateAgent(agentId, request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("permission", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateAgent_ShouldReturnBadRequest_WhenDuplicateName()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new UpdateAgentRequest { Name = "ExistingName" };

        _mockAgentService
            .Setup(s => s.UpdateAgentAsync(agentId, _currentUserId, request))
            .ThrowsAsync(new InvalidOperationException("Agent with name 'ExistingName' already exists"));

        // Act
        var result = await _controller.UpdateAgent(agentId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already exists", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateAgent_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new UpdateAgentRequest { Name = "Updated" };

        _mockAgentService
            .Setup(s => s.UpdateAgentAsync(agentId, _currentUserId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateAgent(agentId, request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region DeleteAgent Tests

    [Fact]
    public async Task DeleteAgent_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.DeleteAgentAsync(agentId, _currentUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteAgent(agentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("deleted successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteAgent_ShouldReturnNotFound_WhenAgentDoesNotExist()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.DeleteAgentAsync(agentId, _currentUserId))
            .ThrowsAsync(new InvalidOperationException($"Agent with ID '{agentId}' not found"));

        // Act
        var result = await _controller.DeleteAgent(agentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteAgent_ShouldReturnUnauthorized_WhenUserNotOwner()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.DeleteAgentAsync(agentId, _currentUserId))
            .ThrowsAsync(new UnauthorizedAccessException("You do not have permission to delete this agent"));

        // Act
        var result = await _controller.DeleteAgent(agentId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("permission", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteAgent_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.DeleteAgentAsync(agentId, _currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteAgent(agentId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region AddToolToAgent Tests

    [Fact]
    public async Task AddToolToAgent_ShouldReturnOk_WhenToolAdded()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.AddToolToAgentAsync(agentId, toolName, _currentUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddToolToAgent(agentId, toolName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("added successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task AddToolToAgent_ShouldReturnNotFound_WhenAgentDoesNotExist()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.AddToolToAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new InvalidOperationException($"Agent with ID '{agentId}' not found"));

        // Act
        var result = await _controller.AddToolToAgent(agentId, toolName);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task AddToolToAgent_ShouldReturnUnauthorized_WhenUserNotOwner()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.AddToolToAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new UnauthorizedAccessException("You do not have permission"));

        // Act
        var result = await _controller.AddToolToAgent(agentId, toolName);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("permission", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task AddToolToAgent_ShouldReturnBadRequest_WhenToolAlreadyAdded()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.AddToolToAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Tool already added to agent"));

        // Act
        var result = await _controller.AddToolToAgent(agentId, toolName);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already added", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task AddToolToAgent_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.AddToolToAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AddToolToAgent(agentId, toolName);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region RemoveToolFromAgent Tests

    [Fact]
    public async Task RemoveToolFromAgent_ShouldReturnOk_WhenToolRemoved()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.RemoveToolFromAgentAsync(agentId, toolName, _currentUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveToolFromAgent(agentId, toolName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("removed successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task RemoveToolFromAgent_ShouldReturnNotFound_WhenAgentDoesNotExist()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.RemoveToolFromAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new InvalidOperationException($"Agent with ID '{agentId}' not found"));

        // Act
        var result = await _controller.RemoveToolFromAgent(agentId, toolName);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task RemoveToolFromAgent_ShouldReturnUnauthorized_WhenUserNotOwner()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.RemoveToolFromAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new UnauthorizedAccessException("You do not have permission"));

        // Act
        var result = await _controller.RemoveToolFromAgent(agentId, toolName);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("permission", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task RemoveToolFromAgent_ShouldReturnNotFound_WhenToolNotAssigned()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.RemoveToolFromAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new InvalidOperationException("Tool not found on agent"));

        // Act
        var result = await _controller.RemoveToolFromAgent(agentId, toolName);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task RemoveToolFromAgent_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolName = "EmailTool";

        _mockAgentService
            .Setup(s => s.RemoveToolFromAgentAsync(agentId, toolName, _currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.RemoveToolFromAgent(agentId, toolName);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion
}
