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
        availableToolCalls = availableToolCalls.ToList();
        
        if (serviceRequest is null)
        {
            throw new Exception("Unable to read openai service request");
        }
        
        var apiConfig = config.GetApiConfig(serviceRequest.Agent);
        if (apiConfig is null)
        {
            throw new Exception("Unable to read openai api config");
        }
        
        serviceRequest.SystemPrompt ??= apiConfig.DefaultSystemPrompt;
        serviceRequest.Model ??= apiConfig.Model;

        var service = new ChatService(Log);
        if (request.Messages is not null && request.Messages.Count > 0)
        {
            serviceRequest.Messages = request.Messages;
        }

        try
        {
            switch (serviceRequest.Method?.ToLower())
            {
                case "get_list_of_available_agents":
                    var agents =  config.Agents.Select(s => new
                    {
                        s.Name,
                        s.Description
                    });
                    return new
                    {
                        Success = true,
                        Agents = agents
                    };
                default:
                    return await service.CompleteChat(serviceRequest, apiConfig, availableToolCalls, request.ServiceFunctions, 1);
            }
        }
        catch (Exception e)
        {
            await Log(LogLevel.Error, e.Message, e);
            return new
            {
                Success = false,
                e.Message
            };
        }
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