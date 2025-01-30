namespace Ai.Orchestrator.Models.Tools;

public class ToolCall
{
    public string Type { get; set; } = "function";
    public ToolFunction Function { get; set; }
}