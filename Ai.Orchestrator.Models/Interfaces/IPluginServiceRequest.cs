namespace Ai.Orchestrator.Models.Interfaces;

public interface IPluginServiceRequest
{
    public string Method { get; set; }
    public string ToolCallId { get; set; }
    public string RequestingService { get; set; }
}