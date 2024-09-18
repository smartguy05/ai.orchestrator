
namespace Ai.Orchestrator.Models.Interfaces;

public interface IConfig
{
    public string ApiKey { get; set; }
    public string PluginDirectory { get; set; }
    public string ConfigDirectory { get; set; }
    public string ActivePlugins { get; set; }
}