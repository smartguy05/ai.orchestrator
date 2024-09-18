using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models.Webhook;

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

    public Task<List<object>> GetPluginContracts()
    {
        return Task.Run(() => _pluginService.GetPluginContracts());
    }
    
    public async Task<object> ProcessRequest(OrchestratorRequest request)
    {
        return await _pluginService.RunPlugin(request);
    }
    
    public async Task<object> ProcessRequestChain(IEnumerable<OrchestratorRequest> requests)
    {
        return await Task.Run(() =>
        {
            var orderedRequests = requests.OrderBy(o => o.Order).ToList();
            object data = null;
            orderedRequests.ForEach(async request =>
            {
                if (data is not null)
                {
                    request.Data = data;
                }
                data = await ProcessRequest(request);
            });

            return data;
        });
    }
}