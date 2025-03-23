using Ai.Orchestrator.Models.Extensions;
using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Models.Interfaces;

public abstract class CommandBase<T, TU>: ICommand where T : class, IPluginServiceRequest where TU : IPluginConfig
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public async Task<object> Execute(OrchestratorRequest request, string configString, IEnumerable<ToolCall> availableToolCalls)
    {
        var serviceRequest = request.ServiceRequest.GetServiceRequest<T>();
        var config = configString.ReadConfig<TU>();
        
        var result = await DoWork(serviceRequest, config, availableToolCalls);
        
        if (!string.IsNullOrWhiteSpace(request.ToolCallId))
        {
            return request.ReturnNewOrchestratorRequest(serviceRequest.RequestingService, result);
        }

        return result;
    }

    public abstract Task<object> DoWork(T serviceRequest, TU config, IEnumerable<ToolCall> availableToolCalls);
}