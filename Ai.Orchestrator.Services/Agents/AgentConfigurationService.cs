using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Agents;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Services.Agents;

/// <summary>
/// Service for agent configuration management
/// </summary>
public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly OrchestratorDbContext _context;

    public AgentConfigurationService(OrchestratorDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AgentDto> CreateAgentAsync(Guid userId, CreateAgentRequest request)
    {
        // Validate user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        // Check for duplicate agent name per user
        var existingAgent = await _context.Agents
            .FirstOrDefaultAsync(a => a.OwnerId == userId && a.Name == request.Name);

        if (existingAgent != null)
        {
            throw new InvalidOperationException($"Agent with name '{request.Name}' already exists for this user");
        }

        // Determine if this should be the default agent (first agent for user)
        var hasExistingAgents = await _context.Agents.AnyAsync(a => a.OwnerId == userId);
        var isDefault = !hasExistingAgents || request.IsDefault;

        // Create new agent
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            Model = request.Model,
            ApiKey = request.ApiKey,
            ApiUrl = request.ApiUrl,
            DefaultSystemPrompt = request.DefaultSystemPrompt,
            ToolsEnabled = request.ToolsEnabled,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Agents.Add(agent);

        // Add enabled tools
        if (request.EnabledTools != null && request.EnabledTools.Any())
        {
            foreach (var toolName in request.EnabledTools)
            {
                _context.AgentTools.Add(new AgentTool
                {
                    Id = Guid.NewGuid(),
                    AgentId = agent.Id,
                    ToolName = toolName,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return await GetAgentByIdAsync(agent.Id, userId);
    }

    public async Task<AgentDto> GetAgentByIdAsync(Guid agentId, Guid userId)
    {
        var agent = await _context.Agents
            .Include(a => a.Owner)
            .Include(a => a.AgentTools)
            .Include(a => a.PluginConfigurations)
            .FirstOrDefaultAsync(a => a.Id == agentId);

        // Check ownership
        if (agent == null || agent.OwnerId != userId)
        {
            return null;
        }

        return MapToDto(agent);
    }

    public async Task<List<AgentDto>> GetUserAgentsAsync(Guid userId)
    {
        var agents = await _context.Agents
            .Include(a => a.Owner)
            .Include(a => a.AgentTools)
            .Include(a => a.PluginConfigurations)
            .Where(a => a.OwnerId == userId && a.IsActive)
            .ToListAsync();

        return agents.Select(MapToDto).ToList();
    }

    public async Task<AgentDto> GetDefaultAgentAsync(Guid userId)
    {
        var agent = await _context.Agents
            .Include(a => a.Owner)
            .Include(a => a.AgentTools)
            .Include(a => a.PluginConfigurations)
            .FirstOrDefaultAsync(a => a.OwnerId == userId && a.IsDefault && a.IsActive);

        return agent != null ? MapToDto(agent) : null;
    }

    public async Task<AgentDto> UpdateAgentAsync(Guid agentId, Guid userId, UpdateAgentRequest request)
    {
        var agent = await _context.Agents.FindAsync(agentId);

        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with ID '{agentId}' not found");
        }

        // Validate ownership
        if (agent.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to update this agent");
        }

        // Check for duplicate name if name is being changed
        if (!string.IsNullOrEmpty(request.Name) && request.Name != agent.Name)
        {
            var existingAgent = await _context.Agents
                .FirstOrDefaultAsync(a => a.OwnerId == userId && a.Name == request.Name && a.Id != agentId);

            if (existingAgent != null)
            {
                throw new InvalidOperationException($"Agent with name '{request.Name}' already exists for this user");
            }

            agent.Name = request.Name;
        }

        // Update properties if provided
        if (request.Description != null)
        {
            agent.Description = request.Description;
        }

        if (request.Model != null)
        {
            agent.Model = request.Model;
        }

        if (request.ApiKey != null)
        {
            agent.ApiKey = request.ApiKey;
        }

        if (request.ApiUrl != null)
        {
            agent.ApiUrl = request.ApiUrl;
        }

        if (request.DefaultSystemPrompt != null)
        {
            agent.DefaultSystemPrompt = request.DefaultSystemPrompt;
        }

        if (request.ToolsEnabled.HasValue)
        {
            agent.ToolsEnabled = request.ToolsEnabled.Value;
        }

        if (request.IsDefault.HasValue && request.IsDefault.Value)
        {
            await SetDefaultAgentInternalAsync(agentId, userId);
        }

        if (request.IsActive.HasValue)
        {
            agent.IsActive = request.IsActive.Value;
        }

        // Update tools if provided
        if (request.EnabledTools != null)
        {
            // Remove existing tools
            var existingTools = await _context.AgentTools
                .Where(t => t.AgentId == agentId)
                .ToListAsync();

            _context.AgentTools.RemoveRange(existingTools);

            // Add new tools
            foreach (var toolName in request.EnabledTools)
            {
                _context.AgentTools.Add(new AgentTool
                {
                    Id = Guid.NewGuid(),
                    AgentId = agentId,
                    ToolName = toolName,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        agent.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetAgentByIdAsync(agentId, userId);
    }

    public async Task SetDefaultAgentAsync(Guid agentId, Guid userId)
    {
        var agent = await _context.Agents.FindAsync(agentId);

        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with ID '{agentId}' not found");
        }

        // Validate ownership
        if (agent.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to set this agent as default");
        }

        await SetDefaultAgentInternalAsync(agentId, userId);
        await _context.SaveChangesAsync();
    }

    private async Task SetDefaultAgentInternalAsync(Guid agentId, Guid userId)
    {
        // Unset current default
        var currentDefault = await _context.Agents
            .FirstOrDefaultAsync(a => a.OwnerId == userId && a.IsDefault);

        if (currentDefault != null)
        {
            currentDefault.IsDefault = false;
        }

        // Set new default
        var newDefault = await _context.Agents.FindAsync(agentId);
        if (newDefault != null)
        {
            newDefault.IsDefault = true;
        }
    }

    public async Task DeactivateAgentAsync(Guid agentId, Guid userId)
    {
        var agent = await _context.Agents.FindAsync(agentId);

        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with ID '{agentId}' not found");
        }

        // Validate ownership
        if (agent.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to deactivate this agent");
        }

        agent.IsActive = false;
        agent.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAgentAsync(Guid agentId, Guid userId)
    {
        var agent = await _context.Agents.FindAsync(agentId);

        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with ID '{agentId}' not found");
        }

        // Validate ownership
        if (agent.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this agent");
        }

        _context.Agents.Remove(agent);
        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetAgentToolsAsync(Guid agentId, Guid userId)
    {
        // Validate ownership
        if (!await IsOwnerAsync(agentId, userId))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this agent's tools");
        }

        var tools = await _context.AgentTools
            .Where(t => t.AgentId == agentId && t.IsEnabled)
            .Select(t => t.ToolName)
            .ToListAsync();

        return tools;
    }

    public async Task EnableToolAsync(Guid agentId, Guid userId, string toolName)
    {
        // Validate ownership
        if (!await IsOwnerAsync(agentId, userId))
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this agent's tools");
        }

        // Check if tool already exists
        var existingTool = await _context.AgentTools
            .FirstOrDefaultAsync(t => t.AgentId == agentId && t.ToolName == toolName);

        if (existingTool == null)
        {
            _context.AgentTools.Add(new AgentTool
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                ToolName = toolName,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }

    public async Task DisableToolAsync(Guid agentId, Guid userId, string toolName)
    {
        // Validate ownership
        if (!await IsOwnerAsync(agentId, userId))
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this agent's tools");
        }

        var tool = await _context.AgentTools
            .FirstOrDefaultAsync(t => t.AgentId == agentId && t.ToolName == toolName);

        if (tool != null)
        {
            _context.AgentTools.Remove(tool);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> AgentExistsAsync(Guid agentId)
    {
        return await _context.Agents.AnyAsync(a => a.Id == agentId);
    }

    public async Task<bool> IsOwnerAsync(Guid agentId, Guid userId)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        return agent != null && agent.OwnerId == userId;
    }

    public async Task<List<AgentDto>> GetAllAgentsAsync(Guid userId)
    {
        return await GetUserAgentsAsync(userId);
    }

    public async Task AddToolToAgentAsync(Guid agentId, string toolName, Guid userId)
    {
        await EnableToolAsync(agentId, userId, toolName);
    }

    public async Task RemoveToolFromAgentAsync(Guid agentId, string toolName, Guid userId)
    {
        await DisableToolAsync(agentId, userId, toolName);
    }

    // Helper methods

    private AgentDto MapToDto(Agent agent)
    {
        return new AgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            OwnerId = agent.OwnerId,
            OwnerUsername = agent.Owner?.Username,
            IsDefault = agent.IsDefault,
            IsActive = agent.IsActive,
            Model = agent.Model,
            ApiUrl = agent.ApiUrl,
            DefaultSystemPrompt = agent.DefaultSystemPrompt,
            ToolsEnabled = agent.ToolsEnabled,
            CreatedAt = agent.CreatedAt,
            UpdatedAt = agent.UpdatedAt,
            EnabledTools = agent.AgentTools?.Where(t => t.IsEnabled).Select(t => t.ToolName).ToList() ?? new List<string>(),
            PluginConfigurations = agent.PluginConfigurations?.Select(pc => new PluginConfigurationDto
            {
                Id = pc.Id,
                AgentId = pc.AgentId,
                PluginName = pc.PluginName,
                ConfigurationJson = pc.ConfigurationJson,
                IsActive = pc.IsActive,
                CreatedAt = pc.CreatedAt,
                UpdatedAt = pc.UpdatedAt
            }).ToList() ?? new List<PluginConfigurationDto>()
        };
    }
}
