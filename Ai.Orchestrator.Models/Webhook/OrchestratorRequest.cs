using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Models.Webhook;

public record OrchestratorRequest : IOrchestratorRequest
{
    public int Order { get; set; }
    public string Service { get; set; }
    public object ServiceRequest { get; set; }
    public object Data { get; set; }
}