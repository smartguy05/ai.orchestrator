
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Models.Interfaces;

public interface ICommand
{
    public LogDelegate Logger { get; set; }
    public string Name { get; }
    public string Description { get; }

    public Task<object> Execute(OrchestratorRequest request, string configString, IEnumerable<ToolCall> availableToolCalls, LogDelegate logFunction);

    public Task<object> Initialize(string config, LogDelegate logFunction);

    public Task Log(Ai.Orchestrator.Models.Enums.LogLevel logLevel, string message, Exception exception = null);
    
    public Task Dispose()
    {
        return Task.CompletedTask;
    }
}
