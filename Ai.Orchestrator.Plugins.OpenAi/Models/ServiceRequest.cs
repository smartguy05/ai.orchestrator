using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public class ServiceRequest: IPluginServiceRequest
{
    public string Method { get; set; }
    public string ToolCallId { get; set; }
    public string RequestingService { get; set; }
    public string ConfirmationId { get; set; }
    public string SystemPrompt { get; set; }
    public string UserPrompt { get; set; }
    public string Model { get; set; }
    public string Agent { get; set; }
    public IEnumerable<ChatMessageHistory> Messages { get; set; }
    public double Temperature { get; set; } = 0.7;
    public string ConversationId { get; set; }
    public string Photo { get; set; }
}