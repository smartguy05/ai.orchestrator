using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public record ToolCallsResponse
{
    public string Role { get; init; }
    public dynamic Content { get; init; }
    [JsonPropertyName("tool_calls")]
    public List<ToolCallOption> ToolCalls { get; init; }
    public string Refusal { get; init; }
}