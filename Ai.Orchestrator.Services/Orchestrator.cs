using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<List<object>> GetPluginContracts()
    {
        return await _pluginService.GetPluginContracts();
    }
    
    public async Task<object> ProcessWebHook(Webhook request)
    {
        return await _pluginService.RunPlugin(request);
    }
    
    public async Task ProcessWebHookChain(IEnumerable<Webhook> requests)
    {
        await Task.Run(() =>
        {
            var orderedRequests = requests.OrderBy(o => o.Order).ToList();
            dynamic data = null;
            orderedRequests.ForEach(async request =>
            {
                if (data is not null)
                {
                    request.Data = data;
                }
                data = await ProcessWebHook(request);
            });
        });
    }
}