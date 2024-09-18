namespace Ai.Orchestrator.Plugins.Webhook.Models;

public record WebHookParameters
{
    public string Name { get; set; }
    public string Url { get; set; }
}