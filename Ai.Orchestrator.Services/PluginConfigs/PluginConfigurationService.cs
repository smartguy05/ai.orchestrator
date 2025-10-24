using Ai.Orchestrator.Models.Data;
using Ai.Orchestrator.Models.DTOs.Agents;
using Ai.Orchestrator.Models.DTOs.PluginConfigs;
using Ai.Orchestrator.Models.Entities;
using Ai.Orchestrator.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ai.Orchestrator.Services.PluginConfigs;

/// <summary>
/// Service for plugin configuration management with default fallback logic
/// </summary>
public class PluginConfigurationService : IPluginConfigurationService
{
    private readonly OrchestratorDbContext _context;

    public PluginConfigurationService(OrchestratorDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PluginConfigurationDto> CreateConfigurationAsync(Guid userId, CreatePluginConfigRequest request)
    {
        // If agent-specific config, validate agent and ownership
        if (request.AgentId.HasValue)
        {
            var agent = await _context.Agents.FindAsync(request.AgentId.Value);

            if (agent == null)
            {
                throw new InvalidOperationException($"Agent with ID '{request.AgentId}' not found");
            }

            if (agent.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to create configurations for this agent");
            }
        }

        // Check for duplicate configuration
        var existingConfig = await _context.PluginConfigurations
            .FirstOrDefaultAsync(pc => pc.AgentId == request.AgentId && pc.PluginName == request.PluginName);

        if (existingConfig != null)
        {
            var configType = request.AgentId.HasValue ? "Agent-specific" : "Default";
            throw new InvalidOperationException($"{configType} configuration for plugin '{request.PluginName}' already exists");
        }

        // Create new configuration
        var configuration = new PluginConfiguration
        {
            Id = Guid.NewGuid(),
            AgentId = request.AgentId,
            PluginName = request.PluginName,
            ConfigurationJson = request.ConfigurationJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PluginConfigurations.Add(configuration);
        await _context.SaveChangesAsync();

        return MapToDto(configuration);
    }

    public async Task<PluginConfigurationDto> GetConfigurationAsync(Guid agentId, string pluginName, Guid userId)
    {
        // Validate agent ownership
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null || agent.OwnerId != userId)
        {
            return null;
        }

        // Priority 1: Try to get agent-specific configuration
        var agentConfig = await _context.PluginConfigurations
            .FirstOrDefaultAsync(pc => pc.AgentId == agentId && pc.PluginName == pluginName && pc.IsActive);

        if (agentConfig != null)
        {
            return MapToDto(agentConfig);
        }

        // Priority 2: Fall back to default configuration
        var defaultConfig = await _context.PluginConfigurations
            .FirstOrDefaultAsync(pc => pc.AgentId == null && pc.PluginName == pluginName && pc.IsActive);

        if (defaultConfig != null)
        {
            return MapToDto(defaultConfig);
        }

        // Priority 3: No configuration found
        return null;
    }

    public async Task<List<PluginConfigurationDto>> GetAgentConfigurationsAsync(Guid agentId, Guid userId)
    {
        // Validate agent ownership
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null || agent.OwnerId != userId)
        {
            return new List<PluginConfigurationDto>();
        }

        var configurations = await _context.PluginConfigurations
            .Where(pc => pc.AgentId == agentId && pc.IsActive)
            .ToListAsync();

        return configurations.Select(MapToDto).ToList();
    }

    public async Task<List<PluginConfigurationDto>> GetDefaultConfigurationsAsync()
    {
        var configurations = await _context.PluginConfigurations
            .Where(pc => pc.AgentId == null && pc.IsActive)
            .ToListAsync();

        return configurations.Select(MapToDto).ToList();
    }

    public async Task<PluginConfigurationDto> UpdateConfigurationAsync(Guid configurationId, Guid userId, UpdatePluginConfigRequest request)
    {
        var configuration = await _context.PluginConfigurations
            .Include(pc => pc.Agent)
            .FirstOrDefaultAsync(pc => pc.Id == configurationId);

        if (configuration == null)
        {
            throw new InvalidOperationException($"Configuration with ID '{configurationId}' not found");
        }

        // Validate ownership (if agent-specific config)
        if (configuration.AgentId.HasValue)
        {
            if (configuration.Agent == null || configuration.Agent.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this configuration");
            }
        }
        // For default configs, any authenticated user can update (or add permission check if needed)

        // Update properties
        configuration.ConfigurationJson = request.ConfigurationJson;

        if (request.IsActive.HasValue)
        {
            configuration.IsActive = request.IsActive.Value;
        }

        configuration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(configuration);
    }

    public async Task DeleteConfigurationAsync(Guid configurationId, Guid userId)
    {
        var configuration = await _context.PluginConfigurations
            .Include(pc => pc.Agent)
            .FirstOrDefaultAsync(pc => pc.Id == configurationId);

        if (configuration == null)
        {
            throw new InvalidOperationException($"Configuration with ID '{configurationId}' not found");
        }

        // Validate ownership (if agent-specific config)
        if (configuration.AgentId.HasValue)
        {
            if (configuration.Agent == null || configuration.Agent.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this configuration");
            }
        }
        // For default configs, any authenticated user can delete (or add permission check if needed)

        _context.PluginConfigurations.Remove(configuration);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ConfigurationExistsAsync(Guid configurationId)
    {
        return await _context.PluginConfigurations.AnyAsync(pc => pc.Id == configurationId);
    }

    public async Task<bool> HasAgentConfigurationAsync(Guid agentId, string pluginName)
    {
        return await _context.PluginConfigurations
            .AnyAsync(pc => pc.AgentId == agentId && pc.PluginName == pluginName);
    }

    public async Task<bool> HasDefaultConfigurationAsync(string pluginName)
    {
        return await _context.PluginConfigurations
            .AnyAsync(pc => pc.AgentId == null && pc.PluginName == pluginName);
    }

    // Helper method
    private PluginConfigurationDto MapToDto(PluginConfiguration configuration)
    {
        return new PluginConfigurationDto
        {
            Id = configuration.Id,
            AgentId = configuration.AgentId,
            PluginName = configuration.PluginName,
            ConfigurationJson = configuration.ConfigurationJson,
            IsActive = configuration.IsActive,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };
    }
}
