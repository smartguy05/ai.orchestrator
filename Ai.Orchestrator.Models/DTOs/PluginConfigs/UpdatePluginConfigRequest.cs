using System.ComponentModel.DataAnnotations;

namespace Ai.Orchestrator.Models.DTOs.PluginConfigs;

/// <summary>
/// Request model for updating an existing plugin configuration
/// </summary>
public class UpdatePluginConfigRequest
{
    [Required(ErrorMessage = "Configuration JSON is required")]
    public string ConfigurationJson { get; set; }

    public bool? IsActive { get; set; }
}
