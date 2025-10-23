using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.Agents;

/// <summary>
/// Request model for updating an existing agent
/// </summary>
public class UpdateAgentRequest
{
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

    public bool? ToolsEnabled { get; set; }

    public bool? IsDefault { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// List of tool names to enable for this agent (if null, tools are not updated)
    /// </summary>
    public List<string> EnabledTools { get; set; }
}
