using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public class FileListResponse
{
    [JsonPropertyName("data")]
    public List<FileResponse> Data { get; set; }
}