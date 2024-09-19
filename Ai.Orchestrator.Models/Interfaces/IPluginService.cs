
namespace Ai.Orchestrator.Models.Interfaces;

public interface IPluginService
{
    Task<object> RunPlugin(IOrchestratorRequest request);
    List<object> GetPluginContracts();
}