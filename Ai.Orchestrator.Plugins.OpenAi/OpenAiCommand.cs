using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Enums;
using Ai.Orchestrator.Models.Extensions;
using Ai.Orchestrator.Models.Tools;
using Ai.Orchestrator.Plugins.OpenAi.Models;

namespace Ai.Orchestrator.Plugins.OpenAi;

public class OpenAiCommand : ICommand
{
    public string Name => "OpenAI";
    public string Description => "OpenAI integration";
    public LogDelegate Logger { get; set; }

    public async Task<object> Execute(OrchestratorRequest request, string configString, IEnumerable<ToolCall> availableToolCalls, LogDelegate logFunction)
    {
        Logger = logFunction;
        var serviceRequest = request.ServiceRequest?.GetServiceRequest<ServiceRequest>();
        var config = configString.ReadPluginConfig<ServiceConfig>();
        config.Tools = availableToolCalls;
        
        if (serviceRequest is null && (request.Messages is null || !request.Messages.Any()))
        {
            throw new Exception("Unable to read openai service request");
        }

        if (serviceRequest is not null)
        {
            serviceRequest.SystemPrompt ??= config.DefaultSystemPrompt;
            serviceRequest.Model ??= config.Model;
        }

        var service = new ChatService(Log);
        if (request.Messages is not null && request.Messages.Any())
        {
            if (serviceRequest is null)
            {
                serviceRequest = new ServiceRequest();
            }
            serviceRequest.Messages = request.Messages;
        }
        return await service.CompleteChat(serviceRequest, config, request.ServiceFunctions, 1);
    }
    
    public Task Log(LogLevel logLevel, string message, Exception exception = null)
    {
        return Logger(logLevel, message, exception);
    }
    
    public Task<object> Initialize(string config, LogDelegate logFunction)
    {
        return Task.FromResult<object>(null);
    }
}