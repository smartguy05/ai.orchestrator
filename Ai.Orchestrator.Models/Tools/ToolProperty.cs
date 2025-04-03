
using System.Text.Json.Serialization;

namespace Ai.Orchestrator.Models.Tools;

public class ToolProperty
{
    public string Type { get; set; }
    public string Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object Items { get; set; } = null;
}