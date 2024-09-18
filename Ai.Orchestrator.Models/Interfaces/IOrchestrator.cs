
namespace Ai.Orchestrator.Models.Interfaces;

public interface IOrchestrator
{
    public Task<object> ProcessRequest(Webhook.OrchestratorRequest request);
    public Task<object> ProcessRequestChain(IEnumerable<Webhook.OrchestratorRequest> requests);
    Task<List<object>> GetPluginContracts();
}