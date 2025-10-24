using Ai.Orchestrator.Models.DTOs.Agents;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ai.Orchestrator.Controllers;

/// <summary>
/// Controller for agent configuration management
/// Handles CRUD operations for AI agents with ownership validation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentConfigurationService _agentService;

    public AgentController(IAgentConfigurationService agentService)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
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
    /// Get all agents for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAgents()
    {
        try
        {
            var userId = GetCurrentUserId();
            var agents = await _agentService.GetAllAgentsAsync(userId);
            return Ok(agents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving agents", error = ex.Message });
        }
    }

    /// <summary>
    /// Get agent by ID (ownership validated)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAgentById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var agent = await _agentService.GetAgentByIdAsync(id, userId);

            if (agent == null)
            {
                return NotFound(new { message = $"Agent with ID '{id}' not found or you do not have permission to access it" });
            }

            return Ok(agent);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the agent", error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new agent
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAgent([FromBody] CreateAgentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var agent = await _agentService.CreateAgentAsync(userId, request);
            return CreatedAtAction(nameof(GetAgentById), new { id = agent.Id }, agent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the agent", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an agent (ownership validated)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAgent(Guid id, [FromBody] UpdateAgentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var agent = await _agentService.UpdateAgentAsync(id, userId, request);
            return Ok(agent);
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
            return StatusCode(500, new { message = "An error occurred while updating the agent", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an agent (ownership validated)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAgent(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _agentService.DeleteAgentAsync(id, userId);
            return Ok(new { message = $"Agent with ID '{id}' deleted successfully" });
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
            return StatusCode(500, new { message = "An error occurred while deleting the agent", error = ex.Message });
        }
    }

    /// <summary>
    /// Add a tool to an agent (ownership validated)
    /// </summary>
    [HttpPost("{id}/tools/{toolName}")]
    public async Task<IActionResult> AddToolToAgent(Guid id, string toolName)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _agentService.AddToolToAgentAsync(id, toolName, userId);
            return Ok(new { message = $"Tool '{toolName}' added successfully to agent" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Check if it's a "not found" error or "already added" error
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while adding tool to agent", error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a tool from an agent (ownership validated)
    /// </summary>
    [HttpDelete("{id}/tools/{toolName}")]
    public async Task<IActionResult> RemoveToolFromAgent(Guid id, string toolName)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _agentService.RemoveToolFromAgentAsync(id, toolName, userId);
            return Ok(new { message = $"Tool '{toolName}' removed successfully from agent" });
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
            return StatusCode(500, new { message = "An error occurred while removing tool from agent", error = ex.Message });
        }
    }
}
