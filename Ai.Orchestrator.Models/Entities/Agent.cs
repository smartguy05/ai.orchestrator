namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Agent (mini-agent) configuration entity
/// Represents an AI agent with specific tools and settings
/// </summary>
public class Agent
{
    /// <summary>
    /// Unique agent identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Agent name (unique per owner)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Agent description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Owner user ID
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Is this the default agent for the owner
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Agent active status
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// AI model identifier (e.g., "gpt-4", "claude-3-opus")
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// API key for the AI service (encrypted at rest)
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiUrl { get; set; }

    /// <summary>
    /// Default system prompt for the agent
    /// </summary>
    public string DefaultSystemPrompt { get; set; }

    /// <summary>
    /// Enable tool calling for this agent
    /// </summary>
    public bool ToolsEnabled { get; set; } = true;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Agent owner
    /// </summary>
    public User Owner { get; set; }

    /// <summary>
    /// Plugin configurations specific to this agent
    /// </summary>
    public List<PluginConfiguration> PluginConfigurations { get; set; } = new();

    /// <summary>
    /// Tools/functions enabled for this agent
    /// </summary>
    public List<AgentTool> AgentTools { get; set; } = new();
}
