using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public record ServiceConfig: IPluginConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public IEnumerable<ToolCall> Tools { get; set; }
    public string OpenAiApiKey { get; set; }
    public string OpenAiUrl { get; set; }
    public string RedisConnectionString { get; set; }
    public string DefaultSystemPrompt { get; set; }
    public string Model { get; set; }
}