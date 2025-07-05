using Ai.Orchestrator.Models.Extensions;
using Ai.Orchestrator.Models.Tools;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Models.Interfaces;

public delegate Task LogDelegate(LogLevel level, string message, Exception exception = null);

public abstract class CommandBase<T, TU>: ICommand where T : class, IPluginServiceRequest where TU : IPluginConfig
{
    public LogDelegate Logger { get; set; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    protected abstract INotificationService NotificationService { get; set; }

    public async Task<object> Execute(OrchestratorRequest request, string configString, IEnumerable<ToolCall> availableToolCalls, LogDelegate logFunction, INotificationService notificationService)
    {
        NotificationService =  notificationService;
        Logger = logFunction;
        var serviceRequest = request.ServiceRequest.GetServiceRequest<T>();
        var config = configString.ReadPluginConfig<TU>();
        
        var result = await DoWork(serviceRequest, config, availableToolCalls);
        
        if (!string.IsNullOrWhiteSpace(request.ToolCallId))
        {
            return await request.ReturnNewOrchestratorRequest(serviceRequest.RequestingService, result);
        }

        return result;
    }

    protected abstract Task<object> DoWork(T serviceRequest, TU config, IEnumerable<ToolCall> availableToolCalls);

    public Task Log(LogLevel logLevel, string message, Exception exception = null)
    { 
        return Logger(logLevel, message, exception);
    }
    
    public virtual Task<object> Initialize(string config, LogDelegate logFunction, INotificationService notificationService)
    {
        NotificationService ??= notificationService;
        return Task.FromResult<object>(null);
    }
}