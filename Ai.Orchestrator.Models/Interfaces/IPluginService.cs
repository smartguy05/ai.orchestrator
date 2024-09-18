using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ai.Orchestrator.Models.Interfaces;

public interface IPluginService
{
    Task<object> RunPlugin(IWebhook request);
    Task<List<object>> GetPluginContracts();
}