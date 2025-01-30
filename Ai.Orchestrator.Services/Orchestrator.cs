using System.Text.Json;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Services;

public class Orchestrator: IOrchestrator
{
    private readonly IPluginService _pluginService;

    public Orchestrator(
        IPluginService pluginService
        )
    {
        _pluginService = pluginService;
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
            var serviceFunctions = await GetPluginContracts();
            foreach (var function in serviceFunctions)
            {
                request.ServiceFunctions.Add(function.Key, function.Value);
            }

            request.ServiceFunctions = serviceFunctions;
        }
        var response = await _pluginService.RunPlugin(request);
        
        Console.WriteLine("Orchestrator ProcessRequest");
        if (response is OrchestratorRequest newRequest)
        {
            Console.WriteLine(JsonSerializer.Serialize(newRequest.Messages));
            return AddRequestData(await ProcessRequest(newRequest), request);
        } 
        
        if (response is IEnumerable<OrchestratorRequest> requestChain)
        {
            return AddRequestData(await ProcessRequestChain(requestChain), request);
        }
        
        return response;
    }

    // todo: fix this method
    public async Task<object> ProcessRequestChain(IEnumerable<OrchestratorRequest> requests)
    {
        return await Task.Run(() =>
        {
            // var orderedRequests = requests.OrderBy(o => o.Order).ToList();
            object data = null;
            // orderedRequests.ForEach(async request =>
            // {
            //     // if (data is not null)
            //     // {
            //     //     request.Data = data;
            //     // }
            //     data = await ProcessRequest(request);
            // });

            return data;
        });
    }

    private object AddRequestData(object request, OrchestratorRequest orchestratorRequest)
    {
        Console.WriteLine("AddRequestData Messages");
        if (request is OrchestratorRequest chain)
        {
            if (!chain.Messages.Any())
            {
                chain.Messages = orchestratorRequest.Messages;
            }
            // chain.Data = orchestratorRequest.Data;
            Console.WriteLine(JsonSerializer.Serialize(chain.Messages));
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