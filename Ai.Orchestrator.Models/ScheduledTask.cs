namespace Ai.Orchestrator.Models;

public class ScheduledTask
{
    public string Description { get ;set; }
    public string Name { get ;set; }
    public string Expiration { get ;set; }
    public int? Timeout { get ;set; }
    public bool IsRecurring { get ;set; }
    public OrchestratorRequest OrchestratorRequest { get ;set; }
}