
using Ai.Orchestrator.Models.Dto;
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Models.Interfaces;

public interface IPluginService
{
    Task<object> RunPlugin(OrchestratorRequest request);
    List<ToolCall> GetTools();
    // Task<ToolResponse> UseTool(OrchestratorRequest request);
    Dictionary<string, IEnumerable<string>> GetPluginContracts();
}