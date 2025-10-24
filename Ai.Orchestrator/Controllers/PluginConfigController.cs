using Ai.Orchestrator.Models.DTOs.PluginConfigs;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ai.Orchestrator.Controllers;

/// <summary>
/// Controller for plugin configuration management
/// Handles plugin configurations with 3-tier fallback logic:
/// 1. Agent-specific configuration
/// 2. Default configuration (AgentId = NULL)
/// 3. JSON file fallback (handled by caller)
/// </summary>
[ApiController]
[Route("api/plugin-configs")]
[Authorize]
public class PluginConfigController : ControllerBase
{
    private readonly IPluginConfigurationService _pluginConfigService;

    public PluginConfigController(IPluginConfigurationService pluginConfigService)
    {
        _pluginConfigService = pluginConfigService ?? throw new ArgumentNullException(nameof(pluginConfigService));
    }

    /// <summary>
    /// Get current user's ID from JWT claims
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// Get all plugin configurations for an agent
    /// </summary>
    [HttpGet("agent/{agentId}")]
    public async Task<IActionResult> GetAgentConfigurations(Guid agentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var configurations = await _pluginConfigService.GetAgentConfigurationsAsync(agentId, userId);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving plugin configurations", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific plugin configuration for an agent with fallback to default
    /// Priority: Agent-specific > Default (AgentId=NULL) > null
    /// </summary>
    [HttpGet("agent/{agentId}/{pluginName}")]
    public async Task<IActionResult> GetConfiguration(Guid agentId, string pluginName)
    {
        try
        {
            var userId = GetCurrentUserId();
            var configuration = await _pluginConfigService.GetConfigurationAsync(agentId, pluginName, userId);

            if (configuration == null)
            {
                return NotFound(new { message = $"Configuration for plugin '{pluginName}' not found for agent '{agentId}'" });
            }

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the plugin configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all default plugin configurations (AgentId = NULL)
    /// </summary>
    [HttpGet("defaults")]
    public async Task<IActionResult> GetDefaultConfigurations()
    {
        try
        {
            var configurations = await _pluginConfigService.GetDefaultConfigurationsAsync();
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving default configurations", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new plugin configuration (agent-specific or default)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateConfiguration([FromBody] CreatePluginConfigRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var configuration = await _pluginConfigService.CreateConfigurationAsync(userId, request);

            // For created resources, return 201 with location header
            return CreatedAtAction(
                nameof(GetConfiguration),
                new { agentId = configuration.AgentId ?? Guid.Empty, pluginName = configuration.PluginName },
                configuration);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Check if it's a "not found" error or "already exists" error
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the plugin configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Update a plugin configuration
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConfiguration(Guid id, [FromBody] UpdatePluginConfigRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var configuration = await _pluginConfigService.UpdateConfigurationAsync(id, userId, request);
            return Ok(configuration);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the plugin configuration", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a plugin configuration
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConfiguration(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _pluginConfigService.DeleteConfigurationAsync(id, userId);
            return Ok(new { message = $"Plugin configuration with ID '{id}' deleted successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the plugin configuration", error = ex.Message });
        }
    }
}
