namespace Ai.Orchestrator.Models.Entities;

/// <summary>
/// Plugin configuration entity
/// Stores per-agent plugin configurations (or global defaults if AgentId is NULL)
/// </summary>
public class PluginConfiguration
{
    /// <summary>
    /// Configuration identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Agent ID (NULL for default/global configuration)
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// Plugin identifier/name
    /// </summary>
    public string PluginName { get; set; }

    /// <summary>
    /// Plugin configuration as JSON
    /// This stores the plugin-specific settings in a flexible JSON format
    /// </summary>
    public string ConfigurationJson { get; set; }

    /// <summary>
    /// Configuration active status
    /// </summary>
    public bool IsActive { get; set; } = true;

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
    /// Associated agent (NULL for default configurations)
    /// </summary>
    public Agent Agent { get; set; }
}
