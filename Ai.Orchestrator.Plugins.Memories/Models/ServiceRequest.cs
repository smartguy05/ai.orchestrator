using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Plugins.Memories.Models;

public class ServiceRequest: IPluginServiceRequest
{
    public string Method { get; set; }
    public string ToolCallId { get; set; }
    public string RequestingService { get; set; }
    public string MemoryId { get; set; }
    public string Text { get; set; }
    public string Query { get; set; }
    public int? MaxResults { get; set; }
}