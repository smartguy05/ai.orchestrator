using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Plugins.OpenAi.Models;

public record ToolOption(string Type, ToolFunction Function);