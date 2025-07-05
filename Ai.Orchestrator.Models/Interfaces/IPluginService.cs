using Ai.Orchestrator.Models.Tools;

namespace Ai.Orchestrator.Models.Interfaces;

public interface IPluginService
{
    Task<object> RunPlugin(OrchestratorRequest request);
    List<ToolCall> GetTools();
    Dictionary<string, IEnumerable<string>> GetPluginContracts();
    Task InitializePlugins(LogDelegate logger, INotificationService notificationService);
    Task DisposePlugins();
    T GetPlugin<T>(string pluginName) where T : class;
}