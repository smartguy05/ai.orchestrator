using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Plugins.Webhook.Models;

public record ServiceConfig: IPluginConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public IEnumerable<ToolCall> Tools { get; set; }
    public IEnumerable<WebHookParameters> Webhooks { get; set; }
}