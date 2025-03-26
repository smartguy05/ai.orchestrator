using Ai.Orchestrator.Models.Chat;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public class ApiRequest
{
    public string Model { get; set; }
    public List<ChatMessageHistory> Messages { get; set; }
    public List<ToolOption> Tools { get; set; }
}