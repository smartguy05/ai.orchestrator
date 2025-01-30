using System.ComponentModel;

namespace Ai.Orchestrator.Models.Dto;

public class ToolResponse
{
    public string Role { get; set; }
    public dynamic Content { get; set; }
    [Description("tool_call_id")]
    public string ToolCallId { get; set; }
}