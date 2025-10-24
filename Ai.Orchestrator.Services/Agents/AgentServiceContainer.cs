using Ai.Orchestrator.Models.Interfaces;

namespace Ai.Orchestrator.Services.Agents;

/// <summary>
/// Container for all services associated with a specific agent
/// </summary>
public class AgentServiceContainer
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; }
    public IPluginService PluginService { get; set; }
    public ILoggingService LoggingService { get; set; }
    public INotificationService NotificationService { get; set; }
    public ITaskScheduler TaskScheduler { get; set; }
    public IOrchestrator Orchestrator { get; set; }
}
