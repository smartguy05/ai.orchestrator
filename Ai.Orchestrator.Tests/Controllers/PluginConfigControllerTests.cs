using Ai.Orchestrator.Controllers;
using Ai.Orchestrator.Models.DTOs.PluginConfigs;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ai.Orchestrator.Tests.Controllers;

/// <summary>
/// TDD Tests for PluginConfigController
/// Tests plugin configuration management with 3-tier fallback logic
/// </summary>
public class PluginConfigControllerTests
{
    private readonly Mock<IPluginConfigurationService> _mockPluginConfigService;
    private readonly PluginConfigController _controller;
    private readonly Guid _currentUserId;

    public PluginConfigControllerTests()
    {
        _mockPluginConfigService = new Mock<IPluginConfigurationService>();
        _controller = new PluginConfigController(_mockPluginConfigService.Object);
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

    #region GetAgentConfigurations Tests

    [Fact]
    public async Task GetAgentConfigurations_ShouldReturnOk_WithConfigList()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var configs = new List<PluginConfigurationDto>
        {
            new PluginConfigurationDto { Id = Guid.NewGuid(), AgentId = agentId, PluginName = "Email", ConfigurationJson = "{}" },
            new PluginConfigurationDto { Id = Guid.NewGuid(), AgentId = agentId, PluginName = "OpenAI", ConfigurationJson = "{}" }
        };

        _mockPluginConfigService
            .Setup(s => s.GetAgentConfigurationsAsync(agentId, _currentUserId))
            .ReturnsAsync(configs);

        // Act
        var result = await _controller.GetAgentConfigurations(agentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfigs = Assert.IsAssignableFrom<List<PluginConfigurationDto>>(okResult.Value);
        Assert.Equal(2, returnedConfigs.Count);
    }

    [Fact]
    public async Task GetAgentConfigurations_ShouldReturnEmptyList_WhenNoConfigs()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockPluginConfigService
            .Setup(s => s.GetAgentConfigurationsAsync(agentId, _currentUserId))
            .ReturnsAsync(new List<PluginConfigurationDto>());

        // Act
        var result = await _controller.GetAgentConfigurations(agentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfigs = Assert.IsAssignableFrom<List<PluginConfigurationDto>>(okResult.Value);
        Assert.Empty(returnedConfigs);
    }

    [Fact]
    public async Task GetAgentConfigurations_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        _mockPluginConfigService
            .Setup(s => s.GetAgentConfigurationsAsync(agentId, _currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAgentConfigurations(agentId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetConfiguration Tests

    [Fact]
    public async Task GetConfiguration_ShouldReturnOk_WhenAgentConfigExists()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var pluginName = "Email";
        var config = new PluginConfigurationDto
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            PluginName = pluginName,
            ConfigurationJson = "{\"smtp\":\"agent.smtp.com\"}"
        };

        _mockPluginConfigService
            .Setup(s => s.GetConfigurationAsync(agentId, pluginName, _currentUserId))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.GetConfiguration(agentId, pluginName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfig = Assert.IsType<PluginConfigurationDto>(okResult.Value);
        Assert.Equal(agentId, returnedConfig.AgentId);
        Assert.Equal(pluginName, returnedConfig.PluginName);
    }

    [Fact]
    public async Task GetConfiguration_ShouldReturnOk_WhenDefaultConfigExists()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var pluginName = "Email";
        var config = new PluginConfigurationDto
        {
            Id = Guid.NewGuid(),
            AgentId = null, // Default config
            PluginName = pluginName,
            ConfigurationJson = "{\"smtp\":\"default.smtp.com\"}"
        };

        _mockPluginConfigService
            .Setup(s => s.GetConfigurationAsync(agentId, pluginName, _currentUserId))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.GetConfiguration(agentId, pluginName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfig = Assert.IsType<PluginConfigurationDto>(okResult.Value);
        Assert.Null(returnedConfig.AgentId); // Default config
    }

    [Fact]
    public async Task GetConfiguration_ShouldReturnNotFound_WhenNoConfigExists()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var pluginName = "Email";

        _mockPluginConfigService
            .Setup(s => s.GetConfigurationAsync(agentId, pluginName, _currentUserId))
            .ReturnsAsync((PluginConfigurationDto)null);

        // Act
        var result = await _controller.GetConfiguration(agentId, pluginName);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task GetConfiguration_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var pluginName = "Email";

        _mockPluginConfigService
            .Setup(s => s.GetConfigurationAsync(agentId, pluginName, _currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetConfiguration(agentId, pluginName);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetDefaultConfigurations Tests

    [Fact]
    public async Task GetDefaultConfigurations_ShouldReturnOk_WithConfigList()
    {
        // Arrange
        var configs = new List<PluginConfigurationDto>
        {
            new PluginConfigurationDto { Id = Guid.NewGuid(), AgentId = null, PluginName = "Email", ConfigurationJson = "{}" },
            new PluginConfigurationDto { Id = Guid.NewGuid(), AgentId = null, PluginName = "OpenAI", ConfigurationJson = "{}" }
        };

        _mockPluginConfigService
            .Setup(s => s.GetDefaultConfigurationsAsync())
            .ReturnsAsync(configs);

        // Act
        var result = await _controller.GetDefaultConfigurations();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfigs = Assert.IsAssignableFrom<List<PluginConfigurationDto>>(okResult.Value);
        Assert.Equal(2, returnedConfigs.Count);
        Assert.All(returnedConfigs, c => Assert.Null(c.AgentId));
    }

    [Fact]
    public async Task GetDefaultConfigurations_ShouldReturnEmptyList_WhenNoDefaults()
    {
        // Arrange
        _mockPluginConfigService
            .Setup(s => s.GetDefaultConfigurationsAsync())
            .ReturnsAsync(new List<PluginConfigurationDto>());

        // Act
        var result = await _controller.GetDefaultConfigurations();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfigs = Assert.IsAssignableFrom<List<PluginConfigurationDto>>(okResult.Value);
        Assert.Empty(returnedConfigs);
    }

    [Fact]
    public async Task GetDefaultConfigurations_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        _mockPluginConfigService
            .Setup(s => s.GetDefaultConfigurationsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetDefaultConfigurations();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region CreateConfiguration Tests

    [Fact]
    public async Task CreateConfiguration_ShouldReturnCreated_WhenAgentConfigCreated()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var request = new CreatePluginConfigRequest
        {
            AgentId = agentId,
            PluginName = "Email",
            ConfigurationJson = "{\"smtp\":\"agent.smtp.com\"}"
        };

        var createdConfig = new PluginConfigurationDto
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            PluginName = "Email",
            ConfigurationJson = request.ConfigurationJson
        };

        _mockPluginConfigService
            .Setup(s => s.CreateConfigurationAsync(_currentUserId, request))
            .ReturnsAsync(createdConfig);

        // Act
        var result = await _controller.CreateConfiguration(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedConfig = Assert.IsType<PluginConfigurationDto>(createdResult.Value);
        Assert.Equal(agentId, returnedConfig.AgentId);
        Assert.Equal("Email", returnedConfig.PluginName);
    }

    [Fact]
    public async Task CreateConfiguration_ShouldReturnCreated_WhenDefaultConfigCreated()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = null, // Default config
            PluginName = "Email",
            ConfigurationJson = "{\"smtp\":\"default.smtp.com\"}"
        };

        var createdConfig = new PluginConfigurationDto
        {
            Id = Guid.NewGuid(),
            AgentId = null,
            PluginName = "Email",
            ConfigurationJson = request.ConfigurationJson
        };

        _mockPluginConfigService
            .Setup(s => s.CreateConfigurationAsync(_currentUserId, request))
            .ReturnsAsync(createdConfig);

        // Act
        var result = await _controller.CreateConfiguration(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedConfig = Assert.IsType<PluginConfigurationDto>(createdResult.Value);
        Assert.Null(returnedConfig.AgentId);
    }

    [Fact]
    public async Task CreateConfiguration_ShouldReturnBadRequest_WhenDuplicateConfig()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = Guid.NewGuid(),
            PluginName = "Email",
            ConfigurationJson = "{}"
        };

        _mockPluginConfigService
            .Setup(s => s.CreateConfigurationAsync(_currentUserId, request))
            .ThrowsAsync(new InvalidOperationException("Configuration already exists"));

        // Act
        var result = await _controller.CreateConfiguration(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already exists", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task CreateConfiguration_ShouldReturnNotFound_WhenAgentDoesNotExist()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = Guid.NewGuid(),
            PluginName = "Email",
            ConfigurationJson = "{}"
        };

        _mockPluginConfigService
            .Setup(s => s.CreateConfigurationAsync(_currentUserId, request))
            .ThrowsAsync(new InvalidOperationException("Agent not found"));

        // Act
        var result = await _controller.CreateConfiguration(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task CreateConfiguration_ShouldReturnUnauthorized_WhenUserNotAgentOwner()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            AgentId = Guid.NewGuid(),
            PluginName = "Email",
            ConfigurationJson = "{}"
        };

        _mockPluginConfigService
            .Setup(s => s.CreateConfigurationAsync(_currentUserId, request))
            .ThrowsAsync(new UnauthorizedAccessException("You do not have permission"));

        // Act
        var result = await _controller.CreateConfiguration(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("permission", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task CreateConfiguration_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var request = new CreatePluginConfigRequest
        {
            PluginName = "Email",
            ConfigurationJson = "{}"
        };

        _mockPluginConfigService
            .Setup(s => s.CreateConfigurationAsync(_currentUserId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateConfiguration(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region UpdateConfiguration Tests

    [Fact]
    public async Task UpdateConfiguration_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var request = new UpdatePluginConfigRequest
        {
            ConfigurationJson = "{\"smtp\":\"updated.smtp.com\"}",
            IsActive = true
        };

        var updatedConfig = new PluginConfigurationDto
        {
            Id = configId,
            PluginName = "Email",
            ConfigurationJson = request.ConfigurationJson,
            IsActive = true
        };

        _mockPluginConfigService
            .Setup(s => s.UpdateConfigurationAsync(configId, _currentUserId, request))
            .ReturnsAsync(updatedConfig);

        // Act
        var result = await _controller.UpdateConfiguration(configId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedConfig = Assert.IsType<PluginConfigurationDto>(okResult.Value);
        Assert.Contains("updated", returnedConfig.ConfigurationJson);
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldReturnNotFound_WhenConfigDoesNotExist()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var request = new UpdatePluginConfigRequest { ConfigurationJson = "{}" };

        _mockPluginConfigService
            .Setup(s => s.UpdateConfigurationAsync(configId, _currentUserId, request))
            .ThrowsAsync(new InvalidOperationException($"Configuration with ID '{configId}' not found"));

        // Act
        var result = await _controller.UpdateConfiguration(configId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldReturnUnauthorized_WhenUserNotOwner()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var request = new UpdatePluginConfigRequest { ConfigurationJson = "{}" };

        _mockPluginConfigService
            .Setup(s => s.UpdateConfigurationAsync(configId, _currentUserId, request))
            .ThrowsAsync(new UnauthorizedAccessException("You do not have permission"));

        // Act
        var result = await _controller.UpdateConfiguration(configId, request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("permission", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var request = new UpdatePluginConfigRequest { ConfigurationJson = "{}" };

        _mockPluginConfigService
            .Setup(s => s.UpdateConfigurationAsync(configId, _currentUserId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateConfiguration(configId, request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region DeleteConfiguration Tests

    [Fact]
    public async Task DeleteConfiguration_ShouldReturnOk_WhenDeleteSucceeds()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockPluginConfigService
            .Setup(s => s.DeleteConfigurationAsync(configId, _currentUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteConfiguration(configId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("deleted successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteConfiguration_ShouldReturnNotFound_WhenConfigDoesNotExist()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockPluginConfigService
            .Setup(s => s.DeleteConfigurationAsync(configId, _currentUserId))
            .ThrowsAsync(new InvalidOperationException($"Configuration with ID '{configId}' not found"));

        // Act
        var result = await _controller.DeleteConfiguration(configId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteConfiguration_ShouldReturnUnauthorized_WhenUserNotOwner()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockPluginConfigService
            .Setup(s => s.DeleteConfigurationAsync(configId, _currentUserId))
            .ThrowsAsync(new UnauthorizedAccessException("You do not have permission"));

        // Act
        var result = await _controller.DeleteConfiguration(configId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("permission", unauthorizedResult.Value.ToString());
    }

    [Fact]
    public async Task DeleteConfiguration_ShouldReturnInternalServerError_OnException()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockPluginConfigService
            .Setup(s => s.DeleteConfigurationAsync(configId, _currentUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteConfiguration(configId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion
}
