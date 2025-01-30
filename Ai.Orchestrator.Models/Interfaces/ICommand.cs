
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Models.Interfaces;

public interface ICommand
{
    public string Name { get; }
    public string Description { get; }
    Task<object> Execute(OrchestratorRequest request, string config, IEnumerable<ToolCall> availableToolCalls);
}
