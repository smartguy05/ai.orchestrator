using Ai.Orchestrator.Models.Chat;

namespace Ai.Orchestrator.Models.Interfaces;

public interface IOrchestratorRequest
{
    public string Service { get; set; }
    public object ServiceRequest { get; set; }
    public List<ChatMessageHistory> Messages { get; set; }
    public string ToolCallId { get; set; }
    public Dictionary<string, IEnumerable<string>> ServiceFunctions { get; set; }
}