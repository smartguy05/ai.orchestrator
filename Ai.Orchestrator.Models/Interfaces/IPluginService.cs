
namespace Ai.Orchestrator.Models.Interfaces;

public interface IPluginService
{
    Task<object> RunPlugin(IWebhook request);
    List<object> GetPluginContracts();
}