
using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Models.Chat;

public class ChatMessageHistory
{
    public string Id { get; set; }
    public string Role { get; set; }
    public dynamic Content { get; set; }
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; }
    [JsonPropertyName("tool_calls")]
    public dynamic ToolCalls { get; set; }
    public string Name { get; set; }
}