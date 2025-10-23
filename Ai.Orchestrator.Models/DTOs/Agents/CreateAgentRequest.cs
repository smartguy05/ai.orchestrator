using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.Agents;

/// <summary>
/// Request model for creating a new agent
/// </summary>
public class CreateAgentRequest
{
    [Required(ErrorMessage = "Agent name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    [StringLength(200)]
    public string Model { get; set; }

    [StringLength(500)]
    public string ApiKey { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Invalid API URL format")]
    public string ApiUrl { get; set; }

    [StringLength(50000)]
    public string DefaultSystemPrompt { get; set; }

    public bool ToolsEnabled { get; set; } = true;

    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// List of tool names to enable for this agent
    /// </summary>
    public List<string> EnabledTools { get; set; } = new();
}
