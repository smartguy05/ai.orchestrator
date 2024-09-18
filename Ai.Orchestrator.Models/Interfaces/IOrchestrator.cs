using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ai.Orchestrator.Models.Interfaces;

public interface IOrchestrator
{
    public Task<object> ProcessWebHook(Webhook.Webhook request);
    public Task ProcessWebHookChain(IEnumerable<Webhook.Webhook> requests);
    Task<List<object>> GetPluginContracts();
}