using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Models.Webhook;

public record OrchestratorRequest : IWebhook
{
    public int Order { get; set; }
    public string Service { get; set; }
    public dynamic ServiceRequest { get; set; }
    public dynamic Data { get; set; }
}