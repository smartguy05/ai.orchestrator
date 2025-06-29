using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Models.Interfaces;

public interface IPluginServiceRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; }
    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; set; }
    [JsonPropertyName("requestingService")]
    public string RequestingService { get; set; }
    public string ConfirmationId { get; set; }
}