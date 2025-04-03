
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Models.Interfaces;

public interface ICommand
{
    public string Name { get; }
    public string Description { get; }
    Task<object> Execute(OrchestratorRequest request, string config, IEnumerable<ToolCall> availableToolCalls);

    public Task<object> Initialize(string config)
    {
        return Task.FromResult<object>(null);
    }
    
    public Task Dispose()
    {
        return Task.CompletedTask;
    }
}
