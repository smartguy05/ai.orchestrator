using System.Collections.Generic;
using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Plugins.Webhook.Models;

public record ServiceConfig: IPluginConfig
{
    public object Contract { get; set; }
    public string Description { get; set; }
    public IEnumerable<WebHookSetting> Webhooks { get; set; }
}

public record WebHookSetting
{
    public string Name { get; set; }
    public string Url { get; set; }
}