using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Plugins.Webhook.Models;

public record ServiceRequest: IPluginServiceRequest
{
    public string Method { get; set; }
    public string ToolCallId { get; set; }
    public string RequestingService { get; set; }
    public string ConfirmationId { get; set; }
    public string WebhookName { get; set; }
    public string Value { get; set; }
}