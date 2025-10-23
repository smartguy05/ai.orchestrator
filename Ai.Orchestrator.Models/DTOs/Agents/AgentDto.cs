namespace Ai.Orchestrator.Models.DTOs.Agents;

/// <summary>
/// Agent data transfer object
/// </summary>
public class AgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerUsername { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string Model { get; set; }
    public string ApiUrl { get; set; }
    public string DefaultSystemPrompt { get; set; }
    public bool ToolsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> EnabledTools { get; set; } = new();
    public List<PluginConfigurationDto> PluginConfigurations { get; set; } = new();

    // Note: ApiKey is intentionally excluded from DTOs for security
}
