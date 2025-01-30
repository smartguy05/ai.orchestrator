using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Models;

public class OrchestratorRequest: IOrchestratorRequest
{
    public string Service { get; set; }
    public object ServiceRequest { get; set; }
    public Dictionary<string, dynamic> Data { get; set; }
    public List<ChatMessageHistory> Messages { get; set; } = new();
    public string ToolCallId { get; set; }
    public Dictionary<string, IEnumerable<string>> ServiceFunctions { get; set; }
}