using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Common.Extensions;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Tools;
using Ai.Orchestrator.Plugins.OpenAi.Models;

namespace Ai.Orchestrator.Plugins.OpenAi;

public class OpenAiCommand : ICommand
{
    public string Name => "OpenAI";
    public string Description  => "OpenAI integration";

    public async Task<object> Execute(OrchestratorRequest request, string configString, IEnumerable<ToolCall> availableToolCalls)
    {
        var serviceRequest = request.ServiceRequest?.GetServiceRequest<ServiceRequest>();
        var config = configString.ReadConfig<ServiceConfig>();
        config.Tools = availableToolCalls;
        
        if (serviceRequest is null && (request.Messages is null || !request.Messages.Any()))
        {
            throw new Exception("Unable to read openai service request");
        }

        var service = new ChatService(config);
        if (request.Messages is not null && request.Messages.Any())
        {
            if (serviceRequest is null)
            {
                serviceRequest = new ServiceRequest();
            }
            serviceRequest.Messages = request.Messages;
        }
        return await service.CompleteChat(serviceRequest, config, request.ServiceFunctions);
    }
}