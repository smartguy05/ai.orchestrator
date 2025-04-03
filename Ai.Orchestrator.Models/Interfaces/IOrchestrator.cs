
namespace Ai.Orchestrator.Models.Interfaces;

public interface IOrchestrator
{
    private static IServiceScopeFactory _scopeFactory;
    
    public Task<object> ProcessRequest(OrchestratorRequest request);
    public Task<object> ProcessRequestChain(IEnumerable<OrchestratorRequest> requests);
    Task<Dictionary<string, IEnumerable<string>>> GetPluginContracts();
}