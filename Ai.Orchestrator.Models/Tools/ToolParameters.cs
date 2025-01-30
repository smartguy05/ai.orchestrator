
namespace Ai.Orchestrator.Models.Tools;

public class ToolParameters
{
    public string Type { get; set; }
    public IEnumerable<string> Required { get; set; }
    public Dictionary<string, ToolProperty> Properties { get; set; }
}