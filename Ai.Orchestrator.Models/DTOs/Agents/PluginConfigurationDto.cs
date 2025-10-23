namespace Ai.Orchestrator.Models.DTOs.Agents;

/// <summary>
/// Plugin configuration data transfer object
/// </summary>
public class PluginConfigurationDto
{
    public Guid Id { get; set; }
    public Guid? AgentId { get; set; }
    public string PluginName { get; set; }
    public string ConfigurationJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
