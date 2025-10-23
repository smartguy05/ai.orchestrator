using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.PluginConfigs;

/// <summary>
/// Request model for creating a new plugin configuration
/// </summary>
public class CreatePluginConfigRequest
{
    /// <summary>
    /// Agent ID (null for default/global configuration)
    /// </summary>
    public Guid? AgentId { get; set; }

    [Required(ErrorMessage = "Plugin name is required")]
    [StringLength(200)]
    public string PluginName { get; set; }

    [Required(ErrorMessage = "Configuration JSON is required")]
    public string ConfigurationJson { get; set; }
}
