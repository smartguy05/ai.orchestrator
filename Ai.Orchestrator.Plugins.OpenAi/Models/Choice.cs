using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public record Choice
{
    public dynamic Message { get; set; }
    public object Logprobs { get; set; }
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
    public int Index { get; set; }
}