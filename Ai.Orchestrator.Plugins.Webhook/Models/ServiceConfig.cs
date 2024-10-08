﻿using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Plugins.Webhook.Models;

public record ServiceConfig: IPluginConfig
{
    public object Contract { get; set; }
    public string Description { get; set; }
    public IEnumerable<WebHookParameters> Webhooks { get; set; }
}