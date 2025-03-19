
namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public record ToolCallOption
{
    public string Id { get; set; }
    public string Type { get; set; }
    public FunctionResponse Function { get; set; }
}