namespace Ai.Orchestrator.Plugins.Webhook.Models;

public record ServiceRequest
{
    public string WebhookName { get; set; }
    public string Value { get; set; }
}