using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Plugins.Memories.Models;

public class ServiceConfig: IPluginConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public IEnumerable<ToolCall> Tools { get; set; }
    public string ChromaUrl { get; set; }
    public string DefaultCollectionName { get; set; }
    public string OpenAiApiKey { get; set; }
    public string OpenAiUrl { get; set; }
    public string EmbeddingModel { get; set; }
}