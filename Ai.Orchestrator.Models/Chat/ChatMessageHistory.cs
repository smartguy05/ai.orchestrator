
using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Models.Chat;

public class ChatMessageHistory
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("role")]
    public string Role { get; set; }
    [JsonPropertyName("content")]
    public dynamic Content { get; set; }
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; }
    [JsonPropertyName("tool_calls")]
    public dynamic ToolCalls { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
}