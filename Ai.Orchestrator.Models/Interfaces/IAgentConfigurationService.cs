using Ai.Orchestrator.Models.DTOs.Agents;

namespace Ai.Orchestrator.Models.Interfaces;

/// <summary>
/// Service interface for agent configuration management
/// </summary>
public interface IAgentConfigurationService
{
    /// <summary>
    /// Creates a new agent for a user
    /// </summary>
    /// <param name="userId">Owner user identifier</param>
    /// <param name="request">Agent creation request</param>
    /// <returns>Created agent DTO</returns>
    Task<AgentDto> CreateAgentAsync(Guid userId, CreateAgentRequest request);

    /// <summary>
    /// Gets an agent by ID (with ownership validation)
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">Requesting user identifier</param>
    /// <returns>Agent DTO or null if not found or not authorized</returns>
    Task<AgentDto> GetAgentByIdAsync(Guid agentId, Guid userId);

    /// <summary>
    /// Gets all agents for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of agent DTOs</returns>
    Task<List<AgentDto>> GetUserAgentsAsync(Guid userId);

    /// <summary>
    /// Gets the default agent for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Default agent DTO or null if no default</returns>
    Task<AgentDto> GetDefaultAgentAsync(Guid userId);

    /// <summary>
    /// Updates an agent (with ownership validation)
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">Requesting user identifier</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated agent DTO</returns>
    Task<AgentDto> UpdateAgentAsync(Guid agentId, Guid userId, UpdateAgentRequest request);

    /// <summary>
    /// Sets an agent as the default for a user
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier</param>
    Task SetDefaultAgentAsync(Guid agentId, Guid userId);

    /// <summary>
    /// Deactivates an agent
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier</param>
    Task DeactivateAgentAsync(Guid agentId, Guid userId);

    /// <summary>
    /// Deletes an agent (with ownership validation)
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier</param>
    Task DeleteAgentAsync(Guid agentId, Guid userId);

    /// <summary>
    /// Gets all enabled tools for an agent
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier</param>
    /// <returns>List of tool names</returns>
    Task<List<string>> GetAgentToolsAsync(Guid agentId, Guid userId);

    /// <summary>
    /// Enables a tool for an agent
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier</param>
    /// <param name="toolName">Tool name</param>
    Task EnableToolAsync(Guid agentId, Guid userId, string toolName);

    /// <summary>
    /// Disables a tool for an agent
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier</param>
    /// <param name="toolName">Tool name</param>
    Task DisableToolAsync(Guid agentId, Guid userId, string toolName);

    /// <summary>
    /// Checks if an agent exists
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> AgentExistsAsync(Guid agentId);

    /// <summary>
    /// Checks if a user is the owner of an agent
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <param name="userId">User identifier</param>
    /// <returns>True if owner, false otherwise</returns>
    Task<bool> IsOwnerAsync(Guid agentId, Guid userId);
}
