namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public static class ChatFinishReasons
{
    public const string Stop = "stop";
    public const string Length = "length";
    public const string ContentFilter = "content_filter";
    public const string ToolCalls = "tool_calls";
    public const string FunctionCall = "function";
}