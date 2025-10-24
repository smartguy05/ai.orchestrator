using Ai.Orchestrator.Models.DTOs.Agents;
using Ai.Orchestrator.Models.DTOs.PluginConfigs;

namespace Ai.Orchestrator.Models.Interfaces;

/// <summary>
/// Service interface for plugin configuration management
/// </summary>
public interface IPluginConfigurationService
{
    /// <summary>
    /// Creates a new plugin configuration
    /// </summary>
    /// <param name="userId">User identifier (for ownership validation)</param>
    /// <param name="request">Configuration creation request</param>
    /// <returns>Created configuration DTO</returns>
    Task<PluginConfigurationDto> CreateConfigurationAsync(Guid userId, CreatePluginConfigRequest request);

    /// <summary>
    /// Gets a plugin configuration for an agent with fallback to default
    /// Priority: Agent-specific config > Default config (AgentId=NULL) > null
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="pluginName">Plugin name</param>
    /// <param name="userId">User identifier (for ownership validation)</param>
    /// <returns>Configuration DTO or null if not found</returns>
    Task<PluginConfigurationDto> GetConfigurationAsync(Guid agentId, string pluginName, Guid userId);

    /// <summary>
    /// Gets all plugin configurations for an agent
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier (for ownership validation)</param>
    /// <returns>List of configuration DTOs</returns>
    Task<List<PluginConfigurationDto>> GetAgentConfigurationsAsync(Guid agentId, Guid userId);

    /// <summary>
    /// Gets all default plugin configurations (AgentId=NULL)
    /// </summary>
    /// <returns>List of default configuration DTOs</returns>
    Task<List<PluginConfigurationDto>> GetDefaultConfigurationsAsync();

    /// <summary>
    /// Updates a plugin configuration
    /// </summary>
    /// <param name="configurationId">Configuration identifier</param>
    /// <param name="userId">User identifier (for ownership validation)</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated configuration DTO</returns>
    Task<PluginConfigurationDto> UpdateConfigurationAsync(Guid configurationId, Guid userId, UpdatePluginConfigRequest request);

    /// <summary>
    /// Deletes a plugin configuration
    /// </summary>
    /// <param name="configurationId">Configuration identifier</param>
    /// <param name="userId">User identifier (for ownership validation)</param>
    Task DeleteConfigurationAsync(Guid configurationId, Guid userId);

    /// <summary>
    /// Checks if a configuration exists
    /// </summary>
    /// <param name="configurationId">Configuration identifier</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ConfigurationExistsAsync(Guid configurationId);

    /// <summary>
    /// Checks if an agent has a specific plugin configuration
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="pluginName">Plugin name</param>
    /// <returns>True if agent-specific config exists, false otherwise</returns>
    Task<bool> HasAgentConfigurationAsync(Guid agentId, string pluginName);

    /// <summary>
    /// Checks if a default configuration exists for a plugin
    /// </summary>
    /// <param name="pluginName">Plugin name</param>
    /// <returns>True if default config exists, false otherwise</returns>
    Task<bool> HasDefaultConfigurationAsync(string pluginName);
}
