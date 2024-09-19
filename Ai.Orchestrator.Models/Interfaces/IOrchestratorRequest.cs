namespace Ai.Orchestrator.Models.Interfaces;

public interface IOrchestratorRequest
{
    public string Service { get; set; }
    public object ServiceRequest { get; set; }
    public object Data { get; set; }
}