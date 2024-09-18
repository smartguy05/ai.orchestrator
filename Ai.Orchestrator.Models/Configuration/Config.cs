using Ai.Orchestrator.Models.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Ai.Orchestrator.Models.Configuration;

public class Config: IConfig
{
    public string ApiKey { get; set; }
    public string PluginDirectory { get; set; }
    public string ConfigDirectory { get; set; }
    public string ActivePlugins { get; set; }

    public Config()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables();
        configBuilder.Build().Bind(this);
    }
}