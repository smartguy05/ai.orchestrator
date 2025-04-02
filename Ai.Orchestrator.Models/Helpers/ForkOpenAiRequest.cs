using Ai.Orchestrator.Models.Chat;

namespace Ai.Orchestrator.Models.Helpers;

public class ForkOpenAiRequest
{
    public string ConversationId { get; set; }
    public string RequestingService { get; set; }
    public object ToolResponse { get; set; }
    public string Model { get; set; } = null;
    public string SystemPrompt { get; set; } = null;
    public List<ChatMessageHistory> Messages { get; set; } = null;
}