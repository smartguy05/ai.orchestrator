using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Chat;
using Ai.Orchestrator.Models.Interfaces;
using LogLevel = Ai.Orchestrator.Models.Enums.LogLevel;

namespace Ai.Orchestrator.Services;

public class Orchestrator: IOrchestrator
{
    private readonly IPluginService _pluginService;
    private readonly ILoggingService _logger;

    public Orchestrator(
        IPluginService pluginService,
        ILoggingService logger
        )
    {
        _logger = logger;
        _pluginService = pluginService;
        MessageCache.Init();
    }

    public Task<Dictionary<string, IEnumerable<string>>> GetPluginContracts()
    {
        return Task.Run(() => _pluginService.GetPluginContracts());
    }
    
    public async Task<object> ProcessRequest(OrchestratorRequest request)
    {
        request.ServiceFunctions ??= new Dictionary<string, IEnumerable<string>>();
        if (request.ServiceFunctions.Count == 0)
        {
            var serviceFunctions = _pluginService.GetPluginContracts();
            foreach (var function in serviceFunctions)
            {
                request.ServiceFunctions.Add(function.Key, function.Value);
            }

            request.ServiceFunctions = serviceFunctions;
        }
        var response = await _pluginService.RunPlugin(request);
        
        await _logger.Log(LogLevel.Trace, "Orchestrator ProcessRequest");
        if (response is OrchestratorRequest newRequest)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            await _logger.Log(LogLevel.Trace, JsonSerializer.Serialize(newRequest.Messages, options));
            return AddRequestData(await ProcessRequest(newRequest), request);
        } 
        
        if (response is IEnumerable<OrchestratorRequest> requestChain)
        {
            return AddRequestData(await ProcessRequestChain(requestChain), request);
        }
        
        return response;
    }

    public async Task<object> ProcessRequestChain(IEnumerable<OrchestratorRequest> requests)
    {
        var requestList = requests?.ToList();
        if (requests is null || !requestList.Any())
        {
            throw new Exception("No requests found");
        }

        var messages = requestList
            .Select(f => f.Messages)
            .FirstOrDefault()
            ?.ToList();

        if (messages is null || !messages.Any())
        {
            throw new Exception("No messages found");
        }

        var requestingService = string.Empty;

        var firstServiceRequest = requestList.First().ServiceRequest;
        string conversationId = null;
        if (firstServiceRequest is string serviceRequestString)
        {
            var serviceRequestJson = JsonSerializer.Deserialize<JsonElement>(serviceRequestString);
            if (serviceRequestJson.TryGetProperty("requestingService", out var service))
            {
                requestingService = service.GetString();    
            }

            if (serviceRequestJson.TryGetProperty("conversationId", out var convoId))
            {
                conversationId = convoId.GetString();
            }
        }
        
        var processedMessages = messages.ToList();
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string lastToolCallId = null;
        for (var i = 0; i < requestList.Count; i++)
        {
            var newRequest = requestList[i];
            newRequest.ToolCallId = null; // null so we are returned the actual object instead of another Orchestrator Request
            var toolCall = messages.Last().ToolCalls[i];
            
            // todo: multi-thread
            // process each item
            var result = await ProcessRequest(newRequest);
            
            // add new tool message after
            var toolResponseMessage = new ChatMessageHistory
            {
                Role = ChatMessageTypes.Tool,
                Content = result is string ? result : JsonSerializer.Serialize(result, options),
                ToolCallId = toolCall.Id
            };
            processedMessages.Add(toolResponseMessage);
            lastToolCallId = toolCall.Id;
        }
        
        // create single return OrchestratorRequest and return
        var last = requestList.Last();
        
        // correct cached messages
        await MessageCache.SaveCachedMessages(conversationId, processedMessages);
        return await ProcessRequest(new OrchestratorRequest
        {
            Service = requestingService,
            ServiceRequest = last.ServiceRequest,
            ToolCallId = lastToolCallId,
            ServiceFunctions = last.ServiceFunctions,
            Messages = processedMessages
        });
    }
    
    private object AddRequestData(object request, OrchestratorRequest orchestratorRequest)
    {
        _logger.Log(LogLevel.Trace, "AddRequestData Messages").ConfigureAwait(false);
        if (request is OrchestratorRequest chain)
        {
            if (!chain.Messages.Any())
            {
                chain.Messages = orchestratorRequest.Messages;
            }
            // chain.Data = orchestratorRequest.Data;
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            _logger.Log(LogLevel.Trace, JsonSerializer.Serialize(chain.Messages, options)).ConfigureAwait(false);
            return chain;
        }

        if (request is IEnumerable<OrchestratorRequest> chainList)
        {
            foreach (var oRequest in chainList)
            {
                if (!oRequest.Messages.Any())
                {
                    oRequest.Messages = orchestratorRequest.Messages;
                }
                // oRequest.Data = orchestratorRequest.Data;
            }
            return chainList;
        }

        return request;
    }
}